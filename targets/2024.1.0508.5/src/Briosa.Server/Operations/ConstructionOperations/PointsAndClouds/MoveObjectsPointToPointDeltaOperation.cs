using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MoveObjectsPointToPointDeltaOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.move_objects_point_to_point_delta", "Move Objects - Point to Point Delta",
        "briosa.ConstructionOperations", "MoveObjectsPointToPointDelta", "/briosa.ConstructionOperations/MoveObjectsPointToPointDelta",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MoveObjectsPointToPointDeltaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Objects to Move", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ObjectsToMove, "objects_to_move"), "SetCollectionObjectNameRefListArg"),
            new("First Delta Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.FirstDeltaPoint, "first_delta_point"), "SetPointNameArg"),
            new("Second Delta Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.SecondDeltaPoint, "second_delta_point"), "SetPointNameArg")
        ], []);
    }

    public static Api.MoveObjectsPointToPointDeltaResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
