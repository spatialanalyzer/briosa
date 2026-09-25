using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.VectorOperations;

internal static class DeleteVectorsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "vector_operations.delete_vectors", "Delete Vectors",
        "briosa.VectorOperations", "DeleteVectors", "/briosa.VectorOperations/DeleteVectors",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.DeleteVectorsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Vector Name List", WorkerMpValueKind.VectorNameList,
                VectorNameMapper.RequiredList(request.VectorNameList, "vector_name_list"), "SetVectorNameRefListArg")], []);
    }
    public static Api.DeleteVectorsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
