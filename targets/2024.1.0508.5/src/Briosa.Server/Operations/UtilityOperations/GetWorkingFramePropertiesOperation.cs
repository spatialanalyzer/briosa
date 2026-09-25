using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

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
            FrameName = (outputs.Single(value =>
                value.Name == FrameNameArgumentName &&
                value.Kind == WorkerMpValueKind.Text).ReadValue() as WorkerTextValue)?.Value!,
            CollectionName = (outputs.Single(value =>
                value.Name == CollectionNameArgumentName &&
                value.Kind == WorkerMpValueKind.Text).ReadValue() as WorkerTextValue)?.Value!,
            WorkingFrame = SpatialAnalyzerValueMapper.ToProtocol(
                workingFrame.RequireValue<WorkerCollectionObjectNameValue>()),
            Execution = completed.Details
        };
    }
}
