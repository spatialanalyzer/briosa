using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeCollectionObjectNameEnsureUniqueOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_collection_object_name_ensure_unique", "Make a Collection Object Name - Ensure Unique",
        "briosa.ConstructionOperations", "MakeCollectionObjectNameEnsureUnique", "/briosa.ConstructionOperations/MakeCollectionObjectNameEnsureUnique",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("collection_object_name", "Collection Object Name", WorkerMpValueKind.CollectionObjectName)];

    public static WorkerMpCommand CreateCommand(Api.MakeCollectionObjectNameEnsureUniqueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection Object Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CollectionObjectName, "collection_object_name"), "SetCollectionObjectNameArg2"),
            new("Use Number Suffix?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.UseNumberSuffix), "SetBoolArg")
        ], [new("Collection Object Name", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg", WorkerObjectTypeValue.Any)]);
    }

    public static Api.MakeCollectionObjectNameEnsureUniqueResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            CollectionObjectName = CollectionObjectNameMapper.ToProtocol(completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameValue>()),
            Execution = completed.Details
        };
}
