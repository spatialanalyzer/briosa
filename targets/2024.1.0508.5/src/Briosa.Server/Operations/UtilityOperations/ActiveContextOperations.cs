using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

/// <summary>
/// Implements the exact "Get Active Units" MP command contract.
/// </summary>
internal static class GetActiveUnitsOperation
{
    public const string OperationId = "utility_operations.get_active_units";
    public const string StepName = "Get Active Units";
    public const string LengthArgumentName = "Length";
    public const string AngularArgumentName = "Angular";
    public const string TemperatureArgumentName = "Temperature";
    public const string StringGetter = "GetStringArg";

    public static OperationDescriptor Descriptor { get; } = new(
        OperationId,
        StepName,
        "briosa.UtilityOperations",
        "GetActiveUnits",
        "/briosa.UtilityOperations/GetActiveUnits",
        "read_only",
        Api.OperationExecutionScope.GlobalStateRead,
        Api.ReplaySafety.Safe,
        []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [
            new("length", LengthArgumentName, WorkerMpValueKind.Text),
            new("angular", AngularArgumentName, WorkerMpValueKind.Text),
            new("temperature", TemperatureArgumentName, WorkerMpValueKind.Text)
        ];

    public static WorkerMpCommand CreateCommand(Api.GetActiveUnitsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new WorkerMpCommand(
            OperationId,
            StepName,
            inputArguments: [],
            outputArguments:
            [
                new(LengthArgumentName, WorkerMpValueKind.Text, StringGetter),
                new(AngularArgumentName, WorkerMpValueKind.Text, StringGetter),
                new(TemperatureArgumentName, WorkerMpValueKind.Text, StringGetter)
            ]);
    }

    public static Api.GetActiveUnitsResult CreateResult(
        SuccessfulOperationExecution completed)
    {
        ArgumentNullException.ThrowIfNull(completed);
        var outputs = completed.Execution.OutputValues;

        return new Api.GetActiveUnitsResult
        {
            Length = outputs.Single(value =>
                value.Name == LengthArgumentName &&
                value.Kind == WorkerMpValueKind.Text).StringValue!,
            Angular = outputs.Single(value =>
                value.Name == AngularArgumentName &&
                value.Kind == WorkerMpValueKind.Text).StringValue!,
            Temperature = outputs.Single(value =>
                value.Name == TemperatureArgumentName &&
                value.Kind == WorkerMpValueKind.Text).StringValue!,
            Execution = completed.Details
        };
    }
}

/// <summary>
/// Implements the exact "Get Working Frame Properties" MP command contract.
/// </summary>
internal static class GetWorkingFramePropertiesOperation
{
    public const string OperationId =
        "utility_operations.get_working_frame_properties";
    public const string StepName = "Get Working Frame Properties";
    public const string FrameNameArgumentName = "Frame Name";
    public const string CollectionNameArgumentName = "Collection Name";
    public const string WorkingFrameArgumentName = "Working Frame";
    public const string StringGetter = "GetStringArg";
    public const string WorkingFrameGetter = "GetCollectionObjectNameArg";

    public static OperationDescriptor Descriptor { get; } = new(
        OperationId,
        StepName,
        "briosa.UtilityOperations",
        "GetWorkingFrameProperties",
        "/briosa.UtilityOperations/GetWorkingFrameProperties",
        "read_only",
        Api.OperationExecutionScope.GlobalStateRead,
        Api.ReplaySafety.Safe,
        []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [
            new("frame_name", FrameNameArgumentName, WorkerMpValueKind.Text),
            new("collection_name", CollectionNameArgumentName, WorkerMpValueKind.Text),
            new(
                "working_frame",
                WorkingFrameArgumentName,
                WorkerMpValueKind.CollectionObjectName)
        ];

    public static WorkerMpCommand CreateCommand(
        Api.GetWorkingFramePropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new WorkerMpCommand(
            OperationId,
            StepName,
            inputArguments: [],
            outputArguments:
            [
                new(FrameNameArgumentName, WorkerMpValueKind.Text, StringGetter),
                new(CollectionNameArgumentName, WorkerMpValueKind.Text, StringGetter),
                new(
                    WorkingFrameArgumentName,
                    WorkerMpValueKind.CollectionObjectName,
                    WorkingFrameGetter,
                    // Live exact-target validation returned the frame name without
                    // an embedded type literal. Keep this fallback operation-local.
                    WorkerObjectTypeValue.Frame)
            ]);
    }

    public static Api.GetWorkingFramePropertiesResult CreateResult(
        SuccessfulOperationExecution completed)
    {
        ArgumentNullException.ThrowIfNull(completed);
        var outputs = completed.Execution.OutputValues;
        var workingFrame = outputs.Single(value =>
            value.Name == WorkingFrameArgumentName &&
            value.Kind == WorkerMpValueKind.CollectionObjectName);

        return new Api.GetWorkingFramePropertiesResult
        {
            FrameName = outputs.Single(value =>
                value.Name == FrameNameArgumentName &&
                value.Kind == WorkerMpValueKind.Text).StringValue!,
            CollectionName = outputs.Single(value =>
                value.Name == CollectionNameArgumentName &&
                value.Kind == WorkerMpValueKind.Text).StringValue!,
            WorkingFrame = SpatialAnalyzerValueMapper.ToProtocol(
                workingFrame.CollectionObjectNameValue!),
            Execution = completed.Details
        };
    }
}
