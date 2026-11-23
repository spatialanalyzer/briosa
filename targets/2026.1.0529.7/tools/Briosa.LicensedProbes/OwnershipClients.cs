using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Briosa.Worker.Control;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Win32.SafeHandles;
using Api = global::Briosa;

namespace Briosa.LicensedProbes;

/// <summary>
/// The packaged server through generated clients, on loopback HTTP only. Every call has its
/// own deadline and is sent once; a failure becomes a value-free result, never an exception
/// with server text.
/// </summary>
internal sealed class OwnershipServer : IOwnershipServer
{
    public const string SdkLifecycleErrorTrailer = "briosa-spatial-analyzer-sdk-lifecycle-error-bin";

    private static readonly TimeSpan DiscoveryTimeout = TimeSpan.FromSeconds(15);

    private readonly GrpcChannel _channel;
    private readonly TimeSpan _callTimeout;
    private readonly Api.SpatialAnalyzerSdkLifecycle.SpatialAnalyzerSdkLifecycleClient _sdk;

    public OwnershipServer(Uri address, TimeSpan callTimeout)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsLoopback || address.Scheme != Uri.UriSchemeHttp)
            throw new ArgumentException("The ownership probe address must be loopback HTTP.", nameof(address));
        _channel = GrpcChannel.ForAddress(address);
        _callTimeout = callTimeout;
        _sdk = new Api.SpatialAnalyzerSdkLifecycle.SpatialAnalyzerSdkLifecycleClient(_channel);
    }

    public async Task<ServerDescription> DescribeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var info = await new Api.DiscoveryService.DiscoveryServiceClient(_channel)
                .GetServerInfoAsync(new Api.GetServerInfoRequest(), deadline: DateTime.UtcNow.Add(DiscoveryTimeout), cancellationToken: cancellationToken)
                .ResponseAsync.ConfigureAwait(false);
            return new ServerDescription(
                Reachable: true,
                TargetMatches: string.Equals(info.Version?.SpatialAnalyzerTarget, ProbeTarget.SpatialAnalyzerTarget, StringComparison.Ordinal),
                ServerVersion: info.Version is { HasBriosaVersion: true } version ? version.BriosaVersion : null,
                SourceRevision: info.Version is { HasSourceRevision: true } source ? source.SourceRevision : null,
                LifecycleBoundsMilliseconds: Bounds(info.LifecycleBounds));
        }
        catch (RpcException)
        {
            return ServerDescription.Unreachable;
        }
    }

    public Task<LifecycleResult> GetSdkStateAsync(CancellationToken cancellationToken) =>
        CallAsync(options => _sdk.GetSpatialAnalyzerSdkStateAsync(new Api.GetSpatialAnalyzerSdkStateRequest(), options).ResponseAsync,
            static response => response.State, cancellationToken);

    public Task<LifecycleResult> StartSdkAsync(CancellationToken cancellationToken) =>
        CallAsync(options => _sdk.StartSpatialAnalyzerSdkAsync(new Api.StartSpatialAnalyzerSdkRequest(), options).ResponseAsync,
            static response => response.State, cancellationToken);

    public Task<LifecycleResult> ConnectAsync(int generation, CancellationToken cancellationToken) =>
        CallAsync(options => _sdk.ConnectToSpatialAnalyzerAsync(
                new Api.ConnectToSpatialAnalyzerRequest { ExpectedSdkGeneration = generation }, options).ResponseAsync,
            static response => response.State, cancellationToken);

    public Task<LifecycleResult> StopSdkAsync(int generation, CancellationToken cancellationToken) =>
        CallAsync(options => _sdk.StopSpatialAnalyzerSdkAsync(
                new Api.StopSpatialAnalyzerSdkRequest { ExpectedSdkGeneration = generation }, options).ResponseAsync,
            static response => response.State, cancellationToken);

    public async Task<ProbeOutcome> ExecuteAsync(ProbeStep step, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(step);
        var options = new CallOptions(deadline: DateTime.UtcNow.Add(step.ClientTimeout ?? _callTimeout), cancellationToken: cancellationToken);
        try
        {
            return await step.Operation.InvokePublicAsync(_channel.CreateCallInvoker(), step.Request, options).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return ProbeOutcome.Unknown("grpc:client-cancelled", "client-cancelled");
        }
    }

    public ValueTask DisposeAsync()
    {
        _channel.Dispose();
        return ValueTask.CompletedTask;
    }

    internal static LifecycleResult FromState(Api.SpatialAnalyzerSdkLifecycleState? state, string transport) => state is null
        ? LifecycleResult.Failed(transport, null)
        : new LifecycleResult(
            transport,
            state.SdkState.ToString(),
            state.HasSdkGeneration ? state.SdkGeneration : null,
            state.ReadyForMp,
            state.ConnectionState.ToString(),
            state.HasDiagnosticCode ? SafeCode.OrNull(state.DiagnosticCode) : null,
            state.LastIncident?.TerminationKind.ToString());

    internal static LifecycleResult FromRpcException(RpcException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var transport = "grpc:" + exception.StatusCode.ToString();
        var trailer = exception.Trailers.FirstOrDefault(static entry => entry.Key == SdkLifecycleErrorTrailer);
        if (trailer is null)
        {
            return LifecycleResult.Failed(transport, null);
        }

        try
        {
            var error = Api.SpatialAnalyzerSdkLifecycleError.Parser.ParseFrom(trailer.ValueBytes);
            return FromState(error.State, transport) with { DiagnosticCode = SafeCode.OrNull(error.DiagnosticCode) };
        }
        catch (Google.Protobuf.InvalidProtocolBufferException)
        {
            return LifecycleResult.Failed(transport, null);
        }
    }

    internal static IReadOnlyDictionary<string, long> Bounds(Api.LifecycleBounds? bounds)
    {
        var map = new SortedDictionary<string, long>(StringComparer.Ordinal);
        if (bounds is null)
        {
            return map;
        }

        void Add(string name, bool present, uint value)
        {
            if (present && value > 0)
            {
                map[name] = value;
            }
        }

        Add("start_sdk_milliseconds", bounds.HasStartSdkMilliseconds, bounds.StartSdkMilliseconds);
        Add("connect_spatial_analyzer_milliseconds", bounds.HasConnectSpatialAnalyzerMilliseconds, bounds.ConnectSpatialAnalyzerMilliseconds);
        Add("stop_sdk_milliseconds", bounds.HasStopSdkMilliseconds, bounds.StopSdkMilliseconds);
        Add("recover_sdk_milliseconds", bounds.HasRecoverSdkMilliseconds, bounds.RecoverSdkMilliseconds);
        Add("startup_without_launch_milliseconds", bounds.HasStartupWithoutLaunchMilliseconds, bounds.StartupWithoutLaunchMilliseconds);
        return map;
    }

    private async Task<LifecycleResult> CallAsync<TResponse>(
        Func<CallOptions, Task<TResponse>> call,
        Func<TResponse, Api.SpatialAnalyzerSdkLifecycleState?> state,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await call(new CallOptions(deadline: DateTime.UtcNow.Add(_callTimeout), cancellationToken: cancellationToken))
                .ConfigureAwait(false);
            return FromState(state(response), "grpc:OK");
        }
        catch (RpcException exception)
        {
            return FromRpcException(exception);
        }
        catch (OperationCanceledException)
        {
            return LifecycleResult.Failed("grpc:client-cancelled", "client-cancelled");
        }
    }
}

/// <summary>
/// Terminates one Briosa worker or server process, never anything else, after checking that
/// the id still names the process the tracker saw (name and start time). It ends that process
/// only, not its children.
/// </summary>
internal sealed class ProcessTerminator : IProcessTerminator
{
    private static readonly TimeSpan ExitWait = TimeSpan.FromSeconds(10);

    public static ProcessTerminator Instance { get; } = new();

    public string Terminate(int processId, DateTimeOffset? startedAt, string expectedName)
    {
        if (expectedName is not (ProcessCensus.BriosaWorker or ProcessCensus.BriosaServer))
        {
            throw new ArgumentException("Only a Briosa worker or server may be terminated.", nameof(expectedName));
        }

        Process process;
        try
        {
            process = Process.GetProcessById(processId);
        }
        catch (ArgumentException)
        {
            return "not-found";
        }

        using (process)
        {
            try
            {
                if (!string.Equals(process.ProcessName, expectedName, StringComparison.OrdinalIgnoreCase) ||
                    (startedAt is { } expected && new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero) != expected))
                {
                    return "identity-changed";
                }

                process.Kill(entireProcessTree: false);
                return process.WaitForExit(ExitWait) ? "terminated" : "failed";
            }
            catch (InvalidOperationException)
            {
                return "not-found";
            }
            catch (Win32Exception)
            {
                return "failed";
            }
        }
    }
}

/// <summary>
/// The competing client: a second, unchanged Briosa worker from the same package, hosted by
/// the harness over the private control protocol while the server's worker is connected. It
/// activates the SDK and attaches with one ConnectEx attempt; it never runs an MP. Every
/// exchange is bounded, and a worker that does not stop in time is terminated.
/// </summary>
internal sealed class SecondWorker : ISecondWorker
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan AttachTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(15);

    private readonly string _workerPath;
    private NamedPipeServerStream? _pipe;
    private WorkerControlChannel? _channel;
    private Process? _process;
    private bool _faulted;

    public SecondWorker(string workerPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerPath);
        _workerPath = workerPath;
    }

    public async Task<SecondWorkerResult> StartAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_workerPath) ||
            !string.Equals(Path.GetFileName(_workerPath), "Briosa.Worker.exe", StringComparison.OrdinalIgnoreCase))
            return new SecondWorkerResult("worker-executable-invalid", null, null, null);

        var pipeName = $"briosa-ownership-{Environment.ProcessId}-{Guid.NewGuid():N}";
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
        try
        {
            _process = Process.Start(startInfo);
        }
        catch (Win32Exception)
        {
            return new SecondWorkerResult("worker-start-failed", null, null, null);
        }

        if (_process is null)
            return new SecondWorkerResult("worker-start-failed", null, null, null);

        try
        {
            using (var connect = Linked(ConnectTimeout, cancellationToken))
            {
                await _pipe.WaitForConnectionAsync(connect.Token).ConfigureAwait(false);
            }

            if (!NativeMethods.GetNamedPipeClientProcessId(_pipe.SafePipeHandle, out var clientProcessId) || clientProcessId != (uint)_process.Id)
            {
                await FaultAsync().ConfigureAwait(false);
                return new SecondWorkerResult("worker-pipe-client-mismatch", null, null, null);
            }

            _channel = new WorkerControlChannel(_pipe, leaveOpen: true);
            WorkerControlMessage ready;
            using (var bounded = Linked(StartupTimeout, cancellationToken))
            {
                ready = await _channel.ReceiveAsync(bounded.Token).ConfigureAwait(false);
            }

            if (ready.Kind != WorkerControlMessageKind.Ready || ready.ProcessId != _process.Id)
            {
                await FaultAsync().ConfigureAwait(false);
                return new SecondWorkerResult("worker-ready-invalid", null, null, null);
            }

            var activated = ready.Connection?.RuntimeIdentity?.ActivatedSdk;
            return new SecondWorkerResult("ready", ready.Connection?.State.ToString(), SafeCode.OrNull(ready.Connection?.DiagnosticCode),
                activated is { Source: WorkerRuntimeIdentityEvidenceSource.RuntimeVerified } &&
                string.Equals(activated.Version, ProbeTarget.SpatialAnalyzerTarget, StringComparison.Ordinal));
        }
        catch (Exception exception) when (exception is OperationCanceledException or IOException or InvalidDataException or TimeoutException)
        {
            await FaultAsync().ConfigureAwait(false);
            return new SecondWorkerResult(exception is OperationCanceledException ? "startup-timeout" : "worker-startup-failed", null, null, null);
        }
    }

    public async Task<SecondWorkerResult> ConnectAsync(CancellationToken cancellationToken)
    {
        if (_faulted || _channel is null)
            return new SecondWorkerResult("not-run", null, null, null);
        try
        {
            var request = WorkerControlMessage.Connect(Guid.NewGuid());
            using var bounded = Linked(AttachTimeout, cancellationToken);
            await _channel.SendAsync(request, bounded.Token).ConfigureAwait(false);
            var response = await _channel.ReceiveAsync(bounded.Token).ConfigureAwait(false);
            if (response.Kind != WorkerControlMessageKind.ConnectionResult || response.CorrelationId != request.CorrelationId)
            {
                await FaultAsync().ConfigureAwait(false);
                return new SecondWorkerResult("connect-response-invalid", null, null, null);
            }

            return new SecondWorkerResult(
                response.Connection?.State == WorkerConnectionState.Connected ? "connected" : "not-connected",
                response.Connection?.State.ToString(), SafeCode.OrNull(response.Connection?.DiagnosticCode), null);
        }
        catch (Exception exception) when (exception is OperationCanceledException or IOException or InvalidDataException or TimeoutException)
        {
            // A ConnectEx that does not answer in time: the worker is terminated, never reused.
            await FaultAsync().ConfigureAwait(false);
            return new SecondWorkerResult(exception is OperationCanceledException ? "connect-timeout" : "channel-lost", null, null, null);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Best-effort graceful stop; termination is the bounded fallback and the result says which happened.")]
    public async Task<SecondWorkerResult> StopAsync(CancellationToken cancellationToken)
    {
        if (_process is null)
            return new SecondWorkerResult("not-started", null, null, null);
        if (!_faulted && _channel is not null && !_process.HasExited)
        {
            try
            {
                var request = WorkerControlMessage.Stop(Guid.NewGuid());
                using var bounded = Linked(StopTimeout, cancellationToken);
                await _channel.SendAsync(request, bounded.Token).ConfigureAwait(false);
                var response = await _channel.ReceiveAsync(bounded.Token).ConfigureAwait(false);
                await _process.WaitForExitAsync(bounded.Token).ConfigureAwait(false);
                if (response.Kind == WorkerControlMessageKind.Stopped)
                    return new SecondWorkerResult("stopped", null, null, null);
            }
            catch (Exception)
            {
            }
        }

        var exited = _process.HasExited;
        await TerminateAsync().ConfigureAwait(false);
        return new SecondWorkerResult(exited ? "exited" : "terminated", null, null, null);
    }

    public async ValueTask DisposeAsync()
    {
        await TerminateAsync().ConfigureAwait(false);
        _channel?.Dispose();
        if (_pipe is not null)
        {
            await _pipe.DisposeAsync().ConfigureAwait(false);
        }

        _process?.Dispose();
    }

    private async Task FaultAsync()
    {
        _faulted = true;
        await TerminateAsync().ConfigureAwait(false);
    }

    private async Task TerminateAsync()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            // Only the second worker itself: SDK activation never makes the SDK process its child.
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: false);
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
