using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.VectorOperations;

internal static class DeleteVectorByNameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "vector_operations.delete_vector_by_name", "Delete Vector by Name",
        "briosa.VectorOperations", "DeleteVectorByName", "/briosa.VectorOperations/DeleteVectorByName",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.DeleteVectorByNameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Vector Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroupName, "vector_group_name"), "SetCollectionObjectNameArg2"),
            new("Vector Name", WorkerMpValueKind.Text, new WorkerTextValue(request.VectorName), "SetStringArg")
        ], []);
    }
    public static Api.DeleteVectorByNameResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
