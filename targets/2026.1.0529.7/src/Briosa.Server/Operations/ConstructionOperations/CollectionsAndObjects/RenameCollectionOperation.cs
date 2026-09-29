using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class RenameCollectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.rename_collection", "Rename Collection",
        "briosa.ConstructionOperations", "RenameCollection", "/briosa.ConstructionOperations/RenameCollection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RenameCollectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Original Collection Name", WorkerMpValueKind.CollectionName,
                CollectionNameMapper.Required(request.OriginalCollectionName, "original_collection_name"), "SetCollectionNameArg"),
            new("New Collection Name", WorkerMpValueKind.CollectionName,
                CollectionNameMapper.Required(request.NewCollectionName, "new_collection_name"), "SetCollectionNameArg")
        ], []);
    }

    public static Api.RenameCollectionResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
