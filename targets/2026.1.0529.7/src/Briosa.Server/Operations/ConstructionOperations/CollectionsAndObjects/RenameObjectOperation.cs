using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class RenameObjectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.rename_object", "Rename Object", "briosa.ConstructionOperations", "RenameObject",
        "/briosa.ConstructionOperations/RenameObject", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RenameObjectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Original Object Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.OriginalObjectName, "original_object_name"), "SetCollectionObjectNameArg2"),
            new("New Object Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.NewObjectName, "new_object_name"), "SetCollectionObjectNameArg2"),
            new("Overwrite if exists?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasOverwriteIfExists && request.OverwriteIfExists), "SetBoolArg")
        ], []);
    }

    public static Api.RenameObjectResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
