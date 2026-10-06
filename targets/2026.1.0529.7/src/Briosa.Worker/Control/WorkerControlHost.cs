using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Pipes;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Control;

internal static class WorkerControlHost
{
    private const int MaximumConnectionAttempts = 1;
    internal const int UnexpectedFaultExitCode = 5;

    public static Task<int> RunAsync(string pipeName, int? parentProcessId,
        string targetHost, bool disableSdkActivation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetHost);
        if (parentProcessId is > 0) StartParentMonitor(parentProcessId.Value);
        return RunAsync(pipeName, targetHost, disableSdkActivation
            ? static () => throw new InvalidOperationException("SDK activation is disabled for this worker smoke test.")
            : SpatialAnalyzerSdkAdapter.Create);
    }

    [SuppressMessage(
        "Design", "CA1031:Do not catch general exception types",
        Justification = "Last-resort guard: an unexpected fault still stops the worker, but without writing raw exception text to stderr or the event log.")]
    internal static async Task<int> RunAsync(string pipeName, string targetHost,
        Func<ISpatialAnalyzerSdk> sdkFactory, TextWriter? diagnostics = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetHost);
        ArgumentNullException.ThrowIfNull(sdkFactory);
        var connectionOwner = new SdkConnectionManager(targetHost,
            new SdkConnectionPolicy(MaximumConnectionAttempts, TimeSpan.Zero), sdkFactory);
        try
        {
            // Only SerializedSdkExecutor owns an STA. The pipe loop owns no COM
            // state and awaits each full SDK sequence before reading another message.
            var connection = await connectionOwner.StartAsync().ConfigureAwait(false);
            using var pipe = new NamedPipeClientStream(".", pipeName,
                PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(15_000).ConfigureAwait(false);
            using var channel = new WorkerControlChannel(pipe, leaveOpen: true);
            await channel.SendAsync(WorkerControlMessage.Ready(Environment.ProcessId,
                ToControlSnapshot(connection))).ConfigureAwait(false);
            while (true)
            {
                var message = await channel.ReceiveAsync().ConfigureAwait(false);
                switch (message.Kind)
                {
                    case WorkerControlMessageKind.Ping:
                        var heartbeat = await connectionOwner.ProbeLivenessAsync().ConfigureAwait(false);
                        await channel.SendAsync(WorkerControlMessage.Pong(message.CorrelationId,
                            ToControlSnapshot(heartbeat))).ConfigureAwait(false);
                        break;
                    case WorkerControlMessageKind.Execute:
                        var execution = await ExecuteAsync(connectionOwner, message).ConfigureAwait(false);
                        await SendExecutionAsync(channel, execution).ConfigureAwait(false);
                        break;
                    case WorkerControlMessageKind.Connect:
                        var attached = await connectionOwner.ConnectAsync().ConfigureAwait(false);
                        await channel.SendAsync(WorkerControlMessage.ConnectionResult(message.CorrelationId,
                            ToControlSnapshot(attached))).ConfigureAwait(false);
                        break;
                    case WorkerControlMessageKind.VerifyExecution:
                        var verified = await connectionOwner.VerifyExecutionAsync().ConfigureAwait(false);
                        await channel.SendAsync(WorkerControlMessage.ExecutionVerificationResult(message.CorrelationId,
                            ToControlSnapshot(verified))).ConfigureAwait(false);
                        break;
                    case WorkerControlMessageKind.Stop:
                        await connectionOwner.DisposeAsync().ConfigureAwait(false);
                        await channel.SendAsync(WorkerControlMessage.Stopped(message.CorrelationId)).ConfigureAwait(false);
                        return 0;
                    default:
                        return 4;
                }
            }
        }
        catch (TimeoutException) { return 2; }
        catch (IOException) { return 3; }
        catch (InvalidDataException) { return 4; }
        catch (Exception exception)
        {
            // A fault the SDK adapter did not classify as per-call (or one from the
            // serialized executor itself) leaves the STA untrustworthy. Fail-stop:
            // exiting without a response makes the supervisor retire this generation
            // and report StartedOutcomeUnknown. Only the type and HRESULT are written.
            await (diagnostics ?? Console.Error).WriteLineAsync(string.Create(
                CultureInfo.InvariantCulture,
                $"Briosa.Worker stopped: worker-unexpected-fault {exception.GetType().FullName} 0x{exception.HResult:X8}"))
                .ConfigureAwait(false);
            return UnexpectedFaultExitCode;
        }
        finally
        {
            await connectionOwner.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async Task<WorkerControlMessage> ExecuteAsync(
        SdkConnectionManager connectionOwner, WorkerControlMessage message)
    {
        var request = await connectionOwner.ExecuteAsync(message.Command!)
            .ConfigureAwait(false);
        var response = new WorkerExecutionResponse(
            request.Status == SdkRequestStatus.Completed
                ? WorkerExecutionResponseStatus.Completed : WorkerExecutionResponseStatus.Unavailable,
            request.Execution, ToControlSnapshot(request.Connection), request.DiagnosticCode);
        return WorkerControlMessage.ExecutionResult(message.CorrelationId, response);
    }

    private static async Task SendExecutionAsync(WorkerControlChannel channel, WorkerControlMessage message)
    {
        try
        {
            await channel.SendAsync(message).ConfigureAwait(false);
        }
        catch (WorkerMessageRejectedException) when (message.ExecutionResponse is
            { Execution: WorkerSdkCallFaulted or { MpSucceeded: true } } response)
        {
            // Encoding failed before any frame bytes were written. The MP already
            // completed, or an SDK call faulted: send a bounded response that keeps
            // that evidence (an SDK call fault stays one, dropping only the values
            // that cannot be encoded) and keep the pipe. Actual I/O failures never
            // enter this fallback.
            await channel.SendAsync(WorkerExecutionDelivery.Undeliverable(message.CorrelationId, response))
                .ConfigureAwait(false);
        }
    }

    internal static WorkerConnectionSnapshot ToControlSnapshot(
        SdkConnectionSnapshot connection) =>
        new(
            connection.State switch
            {
                SdkConnectionState.Disconnected => WorkerConnectionState.Disconnected,
                SdkConnectionState.Connecting => WorkerConnectionState.Connecting,
                SdkConnectionState.Connected => WorkerConnectionState.Connected,
                SdkConnectionState.Faulted => WorkerConnectionState.Faulted,
                SdkConnectionState.Stopping => WorkerConnectionState.Stopping,
                _ => throw new UnreachableException()
            },
            connection.ExecutionReadinessState switch
            {
                SdkExecutionReadinessState.Unverified =>
                    WorkerExecutionReadinessState.Unverified,
                SdkExecutionReadinessState.Verifying =>
                    WorkerExecutionReadinessState.Verifying,
                SdkExecutionReadinessState.ExecutionReady =>
                    WorkerExecutionReadinessState.ExecutionReady,
                SdkExecutionReadinessState.CompetingClientSuspected =>
                    WorkerExecutionReadinessState.CompetingClientSuspected,
                SdkExecutionReadinessState.OperatorRecoveryRequired =>
                    WorkerExecutionReadinessState.OperatorRecoveryRequired,
                _ => throw new UnreachableException()
            },
            connection.StatusCode,
            connection.Attempt,
            connection.MaximumAttempts,
            connection.DiagnosticCode,
            connection.TransitionedAt,
            new WorkerRuntimeIdentitySnapshot(
                new WorkerRuntimeIdentityEvidence(
                    connection.ActivatedSdkVersion,
                    connection.ActivatedSdkVersion is null ? WorkerRuntimeIdentityEvidenceSource.Unavailable : WorkerRuntimeIdentityEvidenceSource.RuntimeVerified),
                new WorkerRuntimeIdentityEvidence(
                    Version: null,
                    WorkerRuntimeIdentityEvidenceSource.Unavailable)),
            connection.Failure);

    private static void StartParentMonitor(int parentProcessId)
    {
        var monitor = new Thread(() =>
        {
            try
            {
                using var parent = Process.GetProcessById(parentProcessId);
                parent.WaitForExit();
            }
            catch (ArgumentException)
            {
            }

            Environment.Exit(20);
        })
        {
            IsBackground = true,
            Name = "Briosa worker parent monitor"
        };
        monitor.Start();
    }
}
