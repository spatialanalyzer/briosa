using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class CopyObjectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.copy_object", "Copy Object", "briosa.ConstructionOperations", "CopyObject",
        "/briosa.ConstructionOperations/CopyObject", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CopyObjectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Source Object", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SourceObject, "source_object"), "SetCollectionObjectNameArg2"),
            new("New Object Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.NewObjectName, "new_object_name"), "SetCollectionObjectNameArg2"),
            new("Overwrite if exists?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasOverwriteIfExists && request.OverwriteIfExists), "SetBoolArg")
        ], []);
    }

    public static Api.CopyObjectResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
