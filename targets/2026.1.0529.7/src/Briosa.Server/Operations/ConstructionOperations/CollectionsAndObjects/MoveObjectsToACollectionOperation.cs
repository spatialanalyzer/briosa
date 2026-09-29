using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MoveObjectsToACollectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.move_objects_to_a_collection", "Move Objects to a collection",
        "briosa.ConstructionOperations", "MoveObjectsToACollection", "/briosa.ConstructionOperations/MoveObjectsToACollection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MoveObjectsToACollectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Source Objects", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.SourceObjects, "source_objects"), "SetCollectionObjectNameRefListArg"),
            new("Destination Collection Name", WorkerMpValueKind.CollectionName,
                CollectionNameMapper.Required(request.DestinationCollectionName, "destination_collection_name"), "SetCollectionNameArg")
        ], []);
    }

    public static Api.MoveObjectsToACollectionResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
