using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.VectorOperations;

internal static class DeleteIthVectorFromVectorGroupOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "vector_operations.delete_ith_vector_from_vector_group", "Delete i-th Vector From Vector Group",
        "briosa.VectorOperations", "DeleteIthVectorFromVectorGroup",
        "/briosa.VectorOperations/DeleteIthVectorFromVectorGroup",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.DeleteIthVectorFromVectorGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Vector Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroupName, "vector_group_name"), "SetCollectionObjectNameArg2"),
            new("Vector Index", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.VectorIndex), "SetIntegerArg")
        ], []);
    }
    public static Api.DeleteIthVectorFromVectorGroupResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
