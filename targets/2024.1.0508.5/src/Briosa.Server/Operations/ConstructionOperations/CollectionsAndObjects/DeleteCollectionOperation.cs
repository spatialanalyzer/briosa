using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class DeleteCollectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.delete_collection", "Delete Collection",
        "briosa.ConstructionOperations", "DeleteCollection", "/briosa.ConstructionOperations/DeleteCollection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["destructive"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteCollectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name of Collection to Delete", WorkerMpValueKind.CollectionName,
                CollectionNameMapper.Required(request.CollectionName, "collection_name"), "SetCollectionNameArg")], []);
    }

    public static Api.DeleteCollectionResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
