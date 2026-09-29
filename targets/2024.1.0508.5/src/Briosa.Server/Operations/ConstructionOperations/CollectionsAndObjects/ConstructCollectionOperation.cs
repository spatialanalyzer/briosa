using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructCollectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_collection", "Construct Collection",
        "briosa.ConstructionOperations", "ConstructCollection", "/briosa.ConstructionOperations/ConstructCollection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructCollectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection Name", WorkerMpValueKind.CollectionName,
                CollectionNameMapper.Required(request.CollectionName, "collection_name"), "SetCollectionNameArg"),
            new("Folder Path", WorkerMpValueKind.Text, new WorkerTextValue(request.FolderPath), "SetStringArg"),
            new("Make Default Collection?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.MakeDefaultCollection), "SetBoolArg")
        ], []);
    }

    public static Api.ConstructCollectionResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
