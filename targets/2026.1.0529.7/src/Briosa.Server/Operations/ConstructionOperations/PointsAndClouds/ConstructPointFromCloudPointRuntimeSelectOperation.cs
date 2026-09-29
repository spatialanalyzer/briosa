using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointFromCloudPointRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_from_cloud_point_runtime_select", "Construct Point From Cloud Point - Runtime Select",
        "briosa.ConstructionOperations", "ConstructPointFromCloudPointRuntimeSelect", "/briosa.ConstructionOperations/ConstructPointFromCloudPointRuntimeSelect",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("selection_cloud_point_coordinates", "Selection Cloud Point Coordinates", WorkerMpValueKind.Vector)];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointFromCloudPointRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Selection Prompt", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasSelectionPrompt ? request.SelectionPrompt : "Select cloud point"), "SetStringArg"),
            new("Construct Point?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasConstructPoint && request.ConstructPoint), "SetBoolArg"),
            new("Constructed Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.ConstructedPointName, "constructed_point_name"), "SetPointNameArg")
        ], [new("Selection Cloud Point Coordinates", WorkerMpValueKind.Vector, "GetVectorArg")]);
    }

    public static Api.ConstructPointFromCloudPointRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            Execution = completed.Details,
            SelectionCloudPointCoordinates = VectorMapper.ToProtocol(
                completed.Execution.OutputValues[0].RequireValue<WorkerVectorValue>())
        };
}
