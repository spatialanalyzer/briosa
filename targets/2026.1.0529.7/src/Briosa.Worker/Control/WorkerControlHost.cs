using System.Diagnostics;
using System.IO.Pipes;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Control;

internal static class WorkerControlHost
{
    private const int MaximumConnectionAttempts = 1;

    public static int Run(
        string pipeName,
        int? parentProcessId,
        string targetHost,
        bool disableSdkActivation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetHost);
        if (parentProcessId is > 0)
        {
            StartParentMonitor(parentProcessId.Value);
        }

        var completion = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(
            () => completion.SetResult(
                RunOnSta(pipeName, targetHost, disableSdkActivation)))
        {
            IsBackground = false,
            Name = "Briosa worker control STA"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task.GetAwaiter().GetResult();
    }

    private static int RunOnSta(
        string pipeName,
        string targetHost,
        bool disableSdkActivation)
    {
        var connectionOwner = new SdkConnectionManager(
            targetHost,
            new SdkConnectionPolicy(MaximumConnectionAttempts, TimeSpan.Zero),
            disableSdkActivation
                ? static () => throw new InvalidOperationException(
                    "SDK activation is disabled for this worker smoke test.")
                : SpatialAnalyzerSdkAdapter.Create);
        try
        {
            var connection = connectionOwner.StartAsync().GetAwaiter().GetResult();
            using var pipe = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.InOut,
                PipeOptions.None);
            pipe.Connect(15_000);
            using var channel = new WorkerControlChannel(pipe, leaveOpen: true);
            channel.Send(
                WorkerControlMessage.Ready(
                    Environment.ProcessId,
                    ToControlSnapshot(connection)));

            while (true)
            {
                var message = channel.Receive();
                switch (message.Kind)
                {
                    case WorkerControlMessageKind.Ping:
                        var heartbeat = connectionOwner.ProbeLivenessAsync()
                            .GetAwaiter().GetResult();
                        channel.Send(WorkerControlMessage.Pong(
                            message.CorrelationId,
                            ToControlSnapshot(heartbeat)));
                        break;
                    case WorkerControlMessageKind.Execute:
                        channel.Send(Execute(connectionOwner, message));
                        break;
                    case WorkerControlMessageKind.Connect:
                        channel.Send(Connect(connectionOwner, message));
                        break;
                    case WorkerControlMessageKind.VerifyExecution:
                        channel.Send(VerifyExecution(connectionOwner, message));
                        break;
                    case WorkerControlMessageKind.Stop:
                        connectionOwner.DisposeAsync().AsTask().GetAwaiter().GetResult();
                        channel.Send(WorkerControlMessage.Stopped(message.CorrelationId));
                        return 0;
                    default:
                        return 4;
                }
            }
        }
        catch (TimeoutException)
        {
            return 2;
        }
        catch (IOException)
        {
            return 3;
        }
        catch (InvalidDataException)
        {
            return 4;
        }
        finally
        {
            connectionOwner.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    private static WorkerControlMessage Execute(
        SdkConnectionManager connectionOwner,
        WorkerControlMessage message)
    {
        var request = connectionOwner.ExecuteAsync(SdkCommandMapper.CreateCommand(message.Command!))
            .GetAwaiter().GetResult();
        var response = new WorkerExecutionResponse(
            request.Status == SdkRequestStatus.Completed
                ? WorkerExecutionResponseStatus.Completed
                : WorkerExecutionResponseStatus.Unavailable,
            request.Execution,
            ToControlSnapshot(request.Connection),
            request.DiagnosticCode);
        return WorkerControlMessage.ExecutionResult(message.CorrelationId, response);
    }

    private static WorkerControlMessage Connect(
        SdkConnectionManager connectionOwner,
        WorkerControlMessage message)
    {
        var connection = connectionOwner.ConnectAsync().GetAwaiter().GetResult();
        return WorkerControlMessage.ConnectionResult(
            message.CorrelationId,
            ToControlSnapshot(connection));
    }

    private static WorkerControlMessage VerifyExecution(
        SdkConnectionManager connectionOwner,
        WorkerControlMessage message)
    {
        var connection = connectionOwner.VerifyExecutionAsync()
            .GetAwaiter().GetResult();
        return WorkerControlMessage.ExecutionVerificationResult(
            message.CorrelationId,
            ToControlSnapshot(connection));
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
                    WorkerRuntimeIdentityEvidenceSource.Unavailable)));

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
