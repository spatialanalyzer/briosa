using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Briosa.Worker.Control;
using Microsoft.Win32.SafeHandles;

namespace Briosa.LicensedProbes;

/// <summary>
/// Worker path for probes the public API intentionally cannot express. The
/// harness acts as a minimal host for one unchanged Briosa.Worker process over
/// the existing private control protocol, exactly as the server supervisor does:
/// the worker owns the only SDK client on its single STA, reports runtime SDK
/// identity, attaches with one ConnectEx attempt, and proves execution readiness
/// before any probe. The harness sends each command once, never retries, and
/// terminates the worker after an unknown outcome instead of replaying.
/// </summary>
internal sealed class WorkerTransport : IProbeTransport
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan AttachTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(15);

    private readonly string _workerPath;
    private readonly TimeSpan _stepTimeout;
    private readonly IProcessCensus _census;
    private NamedPipeServerStream? _pipe;
    private WorkerControlChannel? _channel;
    private Process? _process;
    private bool _faulted;

    public WorkerTransport(string workerPath, TimeSpan stepTimeout, IProcessCensus census)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerPath);
        ArgumentNullException.ThrowIfNull(census);
        _workerPath = workerPath;
        _stepTimeout = stepTimeout;
        _census = census;
    }

    public ProbePhase Phase => ProbePhase.Worker;

    public async Task<SessionIdentity> StartAsync(ProbePlan plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);

        // One clean SA and no other Briosa or SDK client: this worker becomes the only one.
        if (_census.Count(ProcessCensus.SpatialAnalyzer) != 1)
            throw new ProbeRefusedException("exactly-one-spatial-analyzer-required");
        if (_census.Count(ProcessCensus.BriosaServer) != 0 || _census.Count(ProcessCensus.BriosaWorker) != 0 ||
            _census.Count(ProcessCensus.SpatialAnalyzerSdk) != 0)
            throw new ProbeRefusedException("residual-briosa-or-sdk-process");
        if (!File.Exists(_workerPath) ||
            !string.Equals(Path.GetFileName(_workerPath), "Briosa.Worker.exe", StringComparison.OrdinalIgnoreCase))
            throw new ProbeRefusedException("worker-executable-invalid");

        var pipeName = $"briosa-probe-{Environment.ProcessId}-{Guid.NewGuid():N}";
        _pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough | PipeOptions.CurrentUserOnly);
        var startInfo = new ProcessStartInfo
        {
            FileName = _workerPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(_workerPath))!
        };
        startInfo.ArgumentList.Add("--control-pipe");
        startInfo.ArgumentList.Add(pipeName);
        startInfo.ArgumentList.Add("--parent-process-id");
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
        _process = Process.Start(startInfo) ?? throw new ProbeRefusedException("worker-start-failed");

        try
        {
            using (var connect = Linked(ConnectTimeout, cancellationToken))
            {
                await _pipe.WaitForConnectionAsync(connect.Token).ConfigureAwait(false);
            }

            // The pipe ACL admits any current-user process; only the launched child may own it.
            if (!NativeMethods.GetNamedPipeClientProcessId(_pipe.SafePipeHandle, out var clientProcessId) ||
                clientProcessId != (uint)_process.Id)
                throw new ProbeRefusedException("worker-pipe-client-mismatch");

            _channel = new WorkerControlChannel(_pipe, leaveOpen: true);
            var ready = await ReceiveAsync(StartupTimeout, cancellationToken).ConfigureAwait(false);
            if (ready.Kind != WorkerControlMessageKind.Ready || ready.ProcessId != _process.Id)
                throw new ProbeRefusedException("worker-ready-invalid");

            // Runtime-verified activated SDK identity must equal this exact target.
            var activated = ready.Connection?.RuntimeIdentity?.ActivatedSdk;
            if (activated is not { Source: WorkerRuntimeIdentityEvidenceSource.RuntimeVerified } ||
                !string.Equals(activated.Version, ProbeTarget.SpatialAnalyzerTarget, StringComparison.Ordinal))
                throw new ProbeRefusedException("activated-sdk-identity-mismatch");

            var attached = await RequestAsync(WorkerControlMessage.Connect(Guid.NewGuid()),
                WorkerControlMessageKind.ConnectionResult, AttachTimeout, cancellationToken).ConfigureAwait(false);
            if (attached.Connection?.State != WorkerConnectionState.Connected)
                throw new ProbeRefusedException("connect-ex-unavailable");

            var verified = await RequestAsync(WorkerControlMessage.VerifyExecution(Guid.NewGuid()),
                WorkerControlMessageKind.ExecutionVerificationResult, AttachTimeout, cancellationToken).ConfigureAwait(false);
            if (verified.Connection?.ExecutionReadinessState != WorkerExecutionReadinessState.ExecutionReady)
                throw new ProbeRefusedException("execution-readiness-not-verified");

            return new SessionIdentity
            {
                ActivatedSdkVersion = activated.Version,
                ActivatedSdkEvidence = "runtime-verified"
            };
        }
        catch (Exception exception) when (exception is OperationCanceledException or IOException or InvalidDataException or TimeoutException)
        {
            _faulted = true;
            throw new ProbeRefusedException("worker-startup-failed");
        }
    }

    public async Task<ProbeOutcome> ExecuteAsync(ProbeStep step, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (_faulted || _channel is null)
        {
            return ProbeOutcome.Unknown("worker:faulted", "worker-transport-faulted");
        }

        try
        {
            var response = await RequestAsync(WorkerControlMessage.Execute(Guid.NewGuid(), step.Command),
                WorkerControlMessageKind.ExecutionResult, _stepTimeout, cancellationToken).ConfigureAwait(false);
            var outcome = response.ExecutionResponse is { } execution
                ? WorkerOutcomes.FromResponse(execution, outputs => step.Operation.ObserveWorker(step.Request, outputs))
                : ProbeOutcome.Unknown("worker:invalid-response", "worker-response-invalid");

            // ExecuteStep false or a missing MP result leaves completion unknown even
            // though the worker answered. Like any unknown outcome, the worker is
            // terminated rather than reused.
            if (outcome.CompletionUnknown)
            {
                await FaultAsync().ConfigureAwait(false);
            }

            return outcome;
        }
        catch (WorkerMessageRejectedException)
        {
            // Encoding failed before any frame byte was written: definitely not started.
            return new ProbeOutcome(ProbeOutcomeKind.NotStarted, "worker:not-sent", null, null, null, [],
                "worker-message-rejected", null, "NotStarted", ProbeOutcome.EmptyObservations);
        }
        catch (Exception exception) when (exception is OperationCanceledException or TimeoutException)
        {
            await FaultAsync().ConfigureAwait(false);
            return ProbeOutcome.Unknown("worker:timeout", "worker-step-timeout");
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException)
        {
            await FaultAsync().ConfigureAwait(false);
            return ProbeOutcome.Unknown("worker:channel-lost", "worker-channel-lost");
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Best-effort graceful stop; termination below is the bounded fallback.")]
    public async ValueTask DisposeAsync()
    {
        if (_process is not null && !_process.HasExited && !_faulted && _channel is not null)
        {
            try
            {
                await RequestAsync(WorkerControlMessage.Stop(Guid.NewGuid()), WorkerControlMessageKind.Stopped,
                    StopTimeout, CancellationToken.None).ConfigureAwait(false);
                using var exit = new CancellationTokenSource(StopTimeout);
                await _process.WaitForExitAsync(exit.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }

        await TerminateAsync().ConfigureAwait(false);
        _channel?.Dispose();
        if (_pipe is not null)
        {
            await _pipe.DisposeAsync().ConfigureAwait(false);
        }

        _process?.Dispose();
    }

    private async Task<WorkerControlMessage> RequestAsync(
        WorkerControlMessage request,
        WorkerControlMessageKind expected,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var bounded = Linked(timeout, cancellationToken);
        await _channel!.SendAsync(request, bounded.Token).ConfigureAwait(false);
        var response = await _channel.ReceiveAsync(bounded.Token).ConfigureAwait(false);
        if (response.Kind != expected || response.CorrelationId != request.CorrelationId)
            throw new InvalidDataException("The worker response does not answer the request.");
        return response;
    }

    private async Task<WorkerControlMessage> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var bounded = Linked(timeout, cancellationToken);
        return await _channel!.ReceiveAsync(bounded.Token).ConfigureAwait(false);
    }

    private async Task FaultAsync()
    {
        _faulted = true;
        await TerminateAsync().ConfigureAwait(false);
    }

    // Like the server watchdog, a worker with an unknown in-flight call is
    // terminated rather than reused. SDK and SA state then need operator review.
    private async Task TerminateAsync()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException) when (_process.HasExited)
        {
        }
        catch (Win32Exception) when (_process.HasExited)
        {
        }

        using var exit = new CancellationTokenSource(StopTimeout);
        try
        {
            await _process.WaitForExitAsync(exit.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static CancellationTokenSource Linked(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(timeout);
        return source;
    }

    private static class NativeMethods
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetNamedPipeClientProcessId(SafePipeHandle pipe, out uint clientProcessId);
    }
}
