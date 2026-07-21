using System.Diagnostics;
using System.IO.Pipes;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Control;

internal static class WorkerControlHost
{
    private const int MaximumConnectionAttempts = 3;
    private static readonly TimeSpan ConnectionRetryDelay = TimeSpan.FromSeconds(1);

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
            new SdkConnectionPolicy(MaximumConnectionAttempts, ConnectionRetryDelay),
            disableSdkActivation
                ? static () => throw new InvalidOperationException(
                    "SDK activation is disabled for this worker smoke test.")
                : SpatialAnalyzerSdkAdapter.Create);
        try
        {
            var connection = connectionOwner.ConnectAsync().GetAwaiter().GetResult();
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
                        channel.Send(WorkerControlMessage.Pong(message.CorrelationId));
                        break;
                    case WorkerControlMessageKind.Execute:
                        channel.Send(Execute(connectionOwner, message));
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
        var request = connectionOwner.ExecuteAsync(ToSdkCommand(message.Command!))
            .GetAwaiter().GetResult();
        var response = new WorkerExecutionResponse(
            request.Status == SdkRequestStatus.Completed
                ? WorkerExecutionResponseStatus.Completed
                : WorkerExecutionResponseStatus.Unavailable,
            request.Execution is null ? null : ToControlResult(request.Execution),
            ToControlSnapshot(request.Connection),
            request.DiagnosticCode);
        return WorkerControlMessage.ExecutionResult(message.CorrelationId, response);
    }

    private static SdkCommand ToSdkCommand(WorkerMpCommand command) =>
        new(
            command.OperationId,
            command.StepName,
            [.. command.InputArguments.Select(ToSdkInputArgument)],
            [.. command.OutputArguments.Select(ToSdkOutputArgument)]);

    private static SdkInputArgument ToSdkInputArgument(WorkerMpInputArgument argument) =>
        new(
            argument.Name,
            ToSdkValueKind(argument.Kind),
            argument.BooleanValue,
            argument.IntegerValue,
            argument.DoubleValue,
            argument.StringValue,
            argument.PointNameValue is null
                ? null
                : new SdkPointNameValue(
                    argument.PointNameValue.CollectionName,
                    argument.PointNameValue.GroupName,
                    argument.PointNameValue.TargetName),
            argument.VectorValue is null
                ? null
                : new SdkVectorValue(
                    argument.VectorValue.X,
                    argument.VectorValue.Y,
                    argument.VectorValue.Z),
            argument.ToleranceVectorOptionsValue is null
                ? null
                : ToSdkToleranceVectorOptions(argument.ToleranceVectorOptionsValue));

    private static SdkOutputArgument ToSdkOutputArgument(WorkerMpOutputArgument argument) =>
        new(argument.Name, ToSdkValueKind(argument.Kind));

    private static SdkValueKind ToSdkValueKind(WorkerMpValueKind kind) =>
        kind switch
        {
            WorkerMpValueKind.Logical => SdkValueKind.Logical,
            WorkerMpValueKind.WholeNumber => SdkValueKind.WholeNumber,
            WorkerMpValueKind.FloatingPoint => SdkValueKind.FloatingPoint,
            WorkerMpValueKind.Text => SdkValueKind.Text,
            WorkerMpValueKind.PointName => SdkValueKind.PointName,
            WorkerMpValueKind.Vector => SdkValueKind.Vector,
            WorkerMpValueKind.ToleranceVectorOptions => SdkValueKind.ToleranceVectorOptions,
            _ => throw new UnreachableException()
        };

    private static SdkToleranceVectorOptionsValue ToSdkToleranceVectorOptions(
        WorkerToleranceVectorOptionsValue value) =>
        new(
            ToSdkToleranceLimit(value.HighX),
            ToSdkToleranceLimit(value.HighY),
            ToSdkToleranceLimit(value.HighZ),
            ToSdkToleranceLimit(value.HighMagnitude),
            ToSdkToleranceLimit(value.LowX),
            ToSdkToleranceLimit(value.LowY),
            ToSdkToleranceLimit(value.LowZ),
            ToSdkToleranceLimit(value.LowMagnitude));

    private static SdkToleranceLimit ToSdkToleranceLimit(WorkerToleranceLimit value) =>
        new(value.Enabled, value.Value);

    private static WorkerMpExecutionResult ToControlResult(SdkExecutionResult execution) =>
        new(
            execution.ExecuteStepReturned,
            execution.MpResult.Succeeded,
            execution.MpResult.ResultCode,
            (long)execution.Duration.TotalMilliseconds,
            [.. execution.OutputValues.Select(ToControlOutputValue)],
            execution.DiagnosticCode);

    private static WorkerMpOutputValue ToControlOutputValue(SdkOutputValue output) =>
        new(
            output.Name,
            ToControlValueKind(output.Kind),
            output.Retrieved,
            output.BooleanValue,
            output.IntegerValue,
            output.DoubleValue,
            output.StringValue,
            output.PointNameValue is null
                ? null
                : new WorkerPointNameValue(
                    output.PointNameValue.CollectionName,
                    output.PointNameValue.GroupName,
                    output.PointNameValue.TargetName),
            output.VectorValue is null
                ? null
                : new WorkerVectorValue(
                    output.VectorValue.X,
                    output.VectorValue.Y,
                    output.VectorValue.Z),
            output.ToleranceVectorOptionsValue is null
                ? null
                : ToControlToleranceVectorOptions(output.ToleranceVectorOptionsValue));

    private static WorkerMpValueKind ToControlValueKind(SdkValueKind kind) =>
        kind switch
        {
            SdkValueKind.Logical => WorkerMpValueKind.Logical,
            SdkValueKind.WholeNumber => WorkerMpValueKind.WholeNumber,
            SdkValueKind.FloatingPoint => WorkerMpValueKind.FloatingPoint,
            SdkValueKind.Text => WorkerMpValueKind.Text,
            SdkValueKind.PointName => WorkerMpValueKind.PointName,
            SdkValueKind.Vector => WorkerMpValueKind.Vector,
            SdkValueKind.ToleranceVectorOptions => WorkerMpValueKind.ToleranceVectorOptions,
            _ => throw new UnreachableException()
        };

    private static WorkerToleranceVectorOptionsValue ToControlToleranceVectorOptions(
        SdkToleranceVectorOptionsValue value) =>
        new(
            ToControlToleranceLimit(value.HighX),
            ToControlToleranceLimit(value.HighY),
            ToControlToleranceLimit(value.HighZ),
            ToControlToleranceLimit(value.HighMagnitude),
            ToControlToleranceLimit(value.LowX),
            ToControlToleranceLimit(value.LowY),
            ToControlToleranceLimit(value.LowZ),
            ToControlToleranceLimit(value.LowMagnitude));

    private static WorkerToleranceLimit ToControlToleranceLimit(SdkToleranceLimit value) =>
        new(value.Enabled, value.Value);

    private static WorkerConnectionSnapshot ToControlSnapshot(
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
            connection.TargetHost,
            connection.StatusCode,
            connection.Attempt,
            connection.MaximumAttempts,
            connection.DiagnosticCode,
            connection.TransitionedAt);

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
