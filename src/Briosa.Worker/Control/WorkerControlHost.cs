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
            BooleanValue: argument.BooleanValue,
            IntegerValue: argument.IntegerValue,
            DoubleValue: argument.DoubleValue,
            StringValue: argument.StringValue,
            PointNameValue: argument.PointNameValue is null
                ? null
                : ToSdkPointName(argument.PointNameValue),
            VectorValue: argument.VectorValue is null
                ? null
                : new SdkVectorValue(
                    argument.VectorValue.X,
                    argument.VectorValue.Y,
                    argument.VectorValue.Z),
            ToleranceVectorOptionsValue: argument.ToleranceVectorOptionsValue is null
                ? null
                : ToSdkToleranceVectorOptions(argument.ToleranceVectorOptionsValue),
            CollectionInstrumentIdValue: argument.CollectionInstrumentIdValue is null
                ? null
                : ToSdkCollectionInstrumentId(argument.CollectionInstrumentIdValue),
            CollectionInstrumentIdListValue: argument.CollectionInstrumentIdListValue is null
                ? null
                : new SdkCollectionInstrumentIdListValue(
                    [.. argument.CollectionInstrumentIdListValue.Values.Select(ToSdkCollectionInstrumentId)]),
            CollectionMachineIdValue: argument.CollectionMachineIdValue is null
                ? null
                : new SdkCollectionMachineIdValue(
                    argument.CollectionMachineIdValue.CollectionName,
                    argument.CollectionMachineIdValue.MachineId),
            CollectionObjectNameValue: argument.CollectionObjectNameValue is null
                ? null
                : ToSdkCollectionObjectName(argument.CollectionObjectNameValue),
            CollectionObjectNameListValue: argument.CollectionObjectNameListValue is null
                ? null
                : new SdkCollectionObjectNameListValue(
                    [.. argument.CollectionObjectNameListValue.Values.Select(ToSdkCollectionObjectName)]),
            CollectionGroupNameListValue: argument.CollectionGroupNameListValue is null
                ? null
                : new SdkCollectionGroupNameListValue(
                    [.. argument.CollectionGroupNameListValue.Values.Select(value =>
                        new SdkCollectionGroupNameValue(value.CollectionName, value.GroupName))]),
            CollectionVectorGroupNameValue: argument.CollectionVectorGroupNameValue is null
                ? null
                : ToSdkCollectionVectorGroupName(argument.CollectionVectorGroupNameValue),
            CollectionVectorGroupNameListValue: argument.CollectionVectorGroupNameListValue is null
                ? null
                : new SdkCollectionVectorGroupNameListValue(
                    [.. argument.CollectionVectorGroupNameListValue.Values.Select(ToSdkCollectionVectorGroupName)]),
            PointNameListValue: argument.PointNameListValue is null
                ? null
                : new SdkPointNameListValue(
                    [.. argument.PointNameListValue.Values.Select(ToSdkPointName)]),
            StringListValue: argument.StringListValue is null
                ? null
                : new SdkStringListValue([.. argument.StringListValue.Values]),
            VectorNameListValue: argument.VectorNameListValue is null
                ? null
                : new SdkVectorNameListValue(
                    [.. argument.VectorNameListValue.Values.Select(value =>
                        new SdkVectorNameValue(
                            value.CollectionName,
                            value.GroupName,
                            value.VectorName))]),
            DoubleArrayValue: argument.DoubleArrayValue is null
                ? null
                : new SdkDoubleArrayValue([.. argument.DoubleArrayValue.Values]),
            TransformValue: argument.TransformValue is null
                ? null
                : new SdkTransformValue([.. argument.TransformValue.Values]),
            WorldTransformValue: argument.WorldTransformValue is null
                ? null
                : new SdkWorldTransformValue(
                    new SdkTransformValue([.. argument.WorldTransformValue.Transform.Values]),
                    argument.WorldTransformValue.ScaleFactor),
            RgbColorValue: argument.RgbColorValue is null
                ? null
                : new SdkRgbColorValue(
                    argument.RgbColorValue.Red,
                    argument.RgbColorValue.Green,
                    argument.RgbColorValue.Blue),
            FileReferenceValue: argument.FileReferenceValue is null
                ? null
                : new SdkFileReferenceValue(
                    argument.FileReferenceValue.Path,
                    argument.FileReferenceValue.EmbeddedFile),
            AngularUnitValue: argument.AngularUnitValue is null
                ? null
                : (SdkAngularUnitValue)(int)argument.AngularUnitValue.Value,
            DistanceUnitValue: argument.DistanceUnitValue is null
                ? null
                : (SdkDistanceUnitValue)(int)argument.DistanceUnitValue.Value,
            TemperatureUnitValue: argument.TemperatureUnitValue is null
                ? null
                : (SdkTemperatureUnitValue)(int)argument.TemperatureUnitValue.Value,
            FontValue: argument.FontValue is null
                ? null
                : new SdkFontValue(
                    argument.FontValue.FontName,
                    argument.FontValue.Size,
                    new SdkRgbColorValue(
                        argument.FontValue.Color.Red,
                        argument.FontValue.Color.Green,
                        argument.FontValue.Color.Blue)),
            SdkBinding: argument.SdkBinding);
    private static SdkOutputArgument ToSdkOutputArgument(WorkerMpOutputArgument argument) =>
        new(argument.Name, ToSdkValueKind(argument.Kind), argument.SdkBinding);

    private static SdkValueKind ToSdkValueKind(WorkerMpValueKind kind) =>
        kind switch
        {
            WorkerMpValueKind.Logical => SdkValueKind.Logical,
            WorkerMpValueKind.WholeNumber => SdkValueKind.WholeNumber,
            WorkerMpValueKind.FloatingPoint => SdkValueKind.FloatingPoint,
            WorkerMpValueKind.Text => SdkValueKind.Text,
            WorkerMpValueKind.DoubleArray => SdkValueKind.DoubleArray,
            WorkerMpValueKind.EditText => SdkValueKind.EditText,
            WorkerMpValueKind.Transform => SdkValueKind.Transform,
            WorkerMpValueKind.WorldTransform => SdkValueKind.WorldTransform,
            WorkerMpValueKind.RgbColor => SdkValueKind.RgbColor,
            WorkerMpValueKind.FileReference => SdkValueKind.FileReference,
            WorkerMpValueKind.AngularUnit => SdkValueKind.AngularUnit,
            WorkerMpValueKind.DistanceUnit => SdkValueKind.DistanceUnit,
            WorkerMpValueKind.TemperatureUnit => SdkValueKind.TemperatureUnit,
            WorkerMpValueKind.Font => SdkValueKind.Font,
            WorkerMpValueKind.PointName => SdkValueKind.PointName,
            WorkerMpValueKind.Vector => SdkValueKind.Vector,
            WorkerMpValueKind.ToleranceVectorOptions => SdkValueKind.ToleranceVectorOptions,
            WorkerMpValueKind.ChartName => SdkValueKind.ChartName,
            WorkerMpValueKind.CloudName => SdkValueKind.CloudName,
            WorkerMpValueKind.CollectionGroupNameList => SdkValueKind.CollectionGroupNameList,
            WorkerMpValueKind.CollectionInstrumentId => SdkValueKind.CollectionInstrumentId,
            WorkerMpValueKind.CollectionInstrumentIdList => SdkValueKind.CollectionInstrumentIdList,
            WorkerMpValueKind.CollectionMachineId => SdkValueKind.CollectionMachineId,
            WorkerMpValueKind.CollectionName => SdkValueKind.CollectionName,
            WorkerMpValueKind.CollectionObjectName => SdkValueKind.CollectionObjectName,
            WorkerMpValueKind.CollectionObjectNameList => SdkValueKind.CollectionObjectNameList,
            WorkerMpValueKind.CollectionVectorGroupName => SdkValueKind.CollectionVectorGroupName,
            WorkerMpValueKind.CollectionVectorGroupNameList => SdkValueKind.CollectionVectorGroupNameList,
            WorkerMpValueKind.FrameName => SdkValueKind.FrameName,
            WorkerMpValueKind.PointNameList => SdkValueKind.PointNameList,
            WorkerMpValueKind.StringList => SdkValueKind.StringList,
            WorkerMpValueKind.VectorGroupName => SdkValueKind.VectorGroupName,
            WorkerMpValueKind.VectorNameList => SdkValueKind.VectorNameList,
            WorkerMpValueKind.ViewName => SdkValueKind.ViewName,
            _ => throw new UnreachableException()
        };

    private static SdkPointNameValue ToSdkPointName(WorkerPointNameValue value) =>
        new(value.CollectionName, value.GroupName, value.TargetName);

    private static SdkCollectionInstrumentIdValue ToSdkCollectionInstrumentId(
        WorkerCollectionInstrumentIdValue value) =>
        new(value.CollectionName, value.InstrumentId);

    private static SdkCollectionObjectNameValue ToSdkCollectionObjectName(
        WorkerCollectionObjectNameValue value) =>
        new(value.CollectionName, value.ObjectName, value.ObjectType);

    private static SdkCollectionVectorGroupNameValue ToSdkCollectionVectorGroupName(
        WorkerCollectionVectorGroupNameValue value) =>
        new(value.CollectionName, value.VectorGroupName);
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
            execution.MpResult.Retrieved,
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
            BooleanValue: output.BooleanValue,
            IntegerValue: output.IntegerValue,
            DoubleValue: output.DoubleValue,
            StringValue: output.StringValue,
            PointNameValue: output.PointNameValue is null
                ? null
                : ToControlPointName(output.PointNameValue),
            VectorValue: output.VectorValue is null
                ? null
                : new WorkerVectorValue(
                    output.VectorValue.X,
                    output.VectorValue.Y,
                    output.VectorValue.Z),
            ToleranceVectorOptionsValue: output.ToleranceVectorOptionsValue is null
                ? null
                : ToControlToleranceVectorOptions(output.ToleranceVectorOptionsValue),
            CollectionInstrumentIdValue: output.CollectionInstrumentIdValue is null
                ? null
                : ToControlCollectionInstrumentId(output.CollectionInstrumentIdValue),
            CollectionInstrumentIdListValue: output.CollectionInstrumentIdListValue is null
                ? null
                : new WorkerCollectionInstrumentIdListValue(
                    [.. output.CollectionInstrumentIdListValue.Values.Select(ToControlCollectionInstrumentId)]),
            CollectionMachineIdValue: output.CollectionMachineIdValue is null
                ? null
                : new WorkerCollectionMachineIdValue(
                    output.CollectionMachineIdValue.CollectionName,
                    output.CollectionMachineIdValue.MachineId),
            CollectionObjectNameValue: output.CollectionObjectNameValue is null
                ? null
                : ToControlCollectionObjectName(output.CollectionObjectNameValue),
            CollectionObjectNameListValue: output.CollectionObjectNameListValue is null
                ? null
                : new WorkerCollectionObjectNameListValue(
                    [.. output.CollectionObjectNameListValue.Values.Select(ToControlCollectionObjectName)]),
            CollectionGroupNameListValue: output.CollectionGroupNameListValue is null
                ? null
                : new WorkerCollectionGroupNameListValue(
                    [.. output.CollectionGroupNameListValue.Values.Select(value =>
                        new WorkerCollectionGroupNameValue(value.CollectionName, value.GroupName))]),
            CollectionVectorGroupNameValue: output.CollectionVectorGroupNameValue is null
                ? null
                : ToControlCollectionVectorGroupName(output.CollectionVectorGroupNameValue),
            CollectionVectorGroupNameListValue: output.CollectionVectorGroupNameListValue is null
                ? null
                : new WorkerCollectionVectorGroupNameListValue(
                    [.. output.CollectionVectorGroupNameListValue.Values.Select(ToControlCollectionVectorGroupName)]),
            PointNameListValue: output.PointNameListValue is null
                ? null
                : new WorkerPointNameListValue(
                    [.. output.PointNameListValue.Values.Select(ToControlPointName)]),
            StringListValue: output.StringListValue is null
                ? null
                : new WorkerStringListValue([.. output.StringListValue.Values]),
            VectorNameListValue: output.VectorNameListValue is null
                ? null
                : new WorkerVectorNameListValue(
                    [.. output.VectorNameListValue.Values.Select(value =>
                        new WorkerVectorNameValue(
                            value.CollectionName,
                            value.GroupName,
                            value.VectorName))]),
            DoubleArrayValue: output.DoubleArrayValue is null
                ? null
                : new WorkerDoubleArrayValue([.. output.DoubleArrayValue.Values]),
            TransformValue: output.TransformValue is null
                ? null
                : new WorkerTransformValue([.. output.TransformValue.Values]),
            WorldTransformValue: output.WorldTransformValue is null
                ? null
                : new WorkerWorldTransformValue(
                    new WorkerTransformValue([.. output.WorldTransformValue.Transform.Values]),
                    output.WorldTransformValue.ScaleFactor),
            FileReferenceValue: output.FileReferenceValue is null
                ? null
                : new WorkerFileReferenceValue(
                    output.FileReferenceValue.Path,
                    output.FileReferenceValue.EmbeddedFile));
    private static WorkerMpValueKind ToControlValueKind(SdkValueKind kind) =>
        kind switch
        {
            SdkValueKind.Logical => WorkerMpValueKind.Logical,
            SdkValueKind.WholeNumber => WorkerMpValueKind.WholeNumber,
            SdkValueKind.FloatingPoint => WorkerMpValueKind.FloatingPoint,
            SdkValueKind.Text => WorkerMpValueKind.Text,
            SdkValueKind.DoubleArray => WorkerMpValueKind.DoubleArray,
            SdkValueKind.EditText => WorkerMpValueKind.EditText,
            SdkValueKind.Transform => WorkerMpValueKind.Transform,
            SdkValueKind.WorldTransform => WorkerMpValueKind.WorldTransform,
            SdkValueKind.RgbColor => WorkerMpValueKind.RgbColor,
            SdkValueKind.FileReference => WorkerMpValueKind.FileReference,
            SdkValueKind.AngularUnit => WorkerMpValueKind.AngularUnit,
            SdkValueKind.DistanceUnit => WorkerMpValueKind.DistanceUnit,
            SdkValueKind.TemperatureUnit => WorkerMpValueKind.TemperatureUnit,
            SdkValueKind.Font => WorkerMpValueKind.Font,
            SdkValueKind.PointName => WorkerMpValueKind.PointName,
            SdkValueKind.Vector => WorkerMpValueKind.Vector,
            SdkValueKind.ToleranceVectorOptions => WorkerMpValueKind.ToleranceVectorOptions,
            SdkValueKind.ChartName => WorkerMpValueKind.ChartName,
            SdkValueKind.CloudName => WorkerMpValueKind.CloudName,
            SdkValueKind.CollectionGroupNameList => WorkerMpValueKind.CollectionGroupNameList,
            SdkValueKind.CollectionInstrumentId => WorkerMpValueKind.CollectionInstrumentId,
            SdkValueKind.CollectionInstrumentIdList => WorkerMpValueKind.CollectionInstrumentIdList,
            SdkValueKind.CollectionMachineId => WorkerMpValueKind.CollectionMachineId,
            SdkValueKind.CollectionName => WorkerMpValueKind.CollectionName,
            SdkValueKind.CollectionObjectName => WorkerMpValueKind.CollectionObjectName,
            SdkValueKind.CollectionObjectNameList => WorkerMpValueKind.CollectionObjectNameList,
            SdkValueKind.CollectionVectorGroupName => WorkerMpValueKind.CollectionVectorGroupName,
            SdkValueKind.CollectionVectorGroupNameList => WorkerMpValueKind.CollectionVectorGroupNameList,
            SdkValueKind.FrameName => WorkerMpValueKind.FrameName,
            SdkValueKind.PointNameList => WorkerMpValueKind.PointNameList,
            SdkValueKind.StringList => WorkerMpValueKind.StringList,
            SdkValueKind.VectorGroupName => WorkerMpValueKind.VectorGroupName,
            SdkValueKind.VectorNameList => WorkerMpValueKind.VectorNameList,
            SdkValueKind.ViewName => WorkerMpValueKind.ViewName,
            _ => throw new UnreachableException()
        };

    private static WorkerPointNameValue ToControlPointName(SdkPointNameValue value) =>
        new(value.CollectionName, value.GroupName, value.TargetName);

    private static WorkerCollectionInstrumentIdValue ToControlCollectionInstrumentId(
        SdkCollectionInstrumentIdValue value) =>
        new(value.CollectionName, value.InstrumentId);

    private static WorkerCollectionObjectNameValue ToControlCollectionObjectName(
        SdkCollectionObjectNameValue value) =>
        new(value.CollectionName, value.ObjectName, value.ObjectType);

    private static WorkerCollectionVectorGroupNameValue ToControlCollectionVectorGroupName(
        SdkCollectionVectorGroupNameValue value) =>
        new(value.CollectionName, value.VectorGroupName);
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
