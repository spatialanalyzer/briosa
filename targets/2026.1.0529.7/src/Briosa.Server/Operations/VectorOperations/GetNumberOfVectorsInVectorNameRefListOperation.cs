using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.VectorOperations;

internal static class GetNumberOfVectorsInVectorNameRefListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "vector_operations.get_number_of_vectors_in_vector_name_ref_list",
        "Get Number of Vectors in Vector Name Ref List", "briosa.VectorOperations",
        "GetNumberOfVectorsInVectorNameRefList",
        "/briosa.VectorOperations/GetNumberOfVectorsInVectorNameRefList",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("total_count", "Total Count", WorkerMpValueKind.WholeNumber)];
    public static WorkerMpCommand CreateCommand(Api.GetNumberOfVectorsInVectorNameRefListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Vector Name List", WorkerMpValueKind.VectorNameList,
                VectorNameMapper.RequiredList(request.VectorNameList, "vector_name_list"), "SetVectorNameRefListArg")],
            [new("Total Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }
    public static Api.GetNumberOfVectorsInVectorNameRefListResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { TotalCount = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
            Execution = completed.Details };
}
