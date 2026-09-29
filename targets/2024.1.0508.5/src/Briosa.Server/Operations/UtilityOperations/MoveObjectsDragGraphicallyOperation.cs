using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class MoveObjectsDragGraphicallyOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.move_objects_drag_graphically", "Move Objects Drag Graphically",
        "briosa.UtilityOperations", "MoveObjectsDragGraphically",
        "/briosa.UtilityOperations/MoveObjectsDragGraphically", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MoveObjectsDragGraphicallyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Objects", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.Objects, "objects"), "SetCollectionObjectNameRefListArg")], []);
    }

    public static Api.MoveObjectsDragGraphicallyResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
