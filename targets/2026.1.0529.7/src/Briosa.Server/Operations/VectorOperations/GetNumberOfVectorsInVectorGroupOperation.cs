using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.VectorOperations;

internal static class GetNumberOfVectorsInVectorGroupOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "vector_operations.get_number_of_vectors_in_vector_group",
        "Get Number of Vectors in Vector Group", "briosa.VectorOperations",
        "GetNumberOfVectorsInVectorGroup", "/briosa.VectorOperations/GetNumberOfVectorsInVectorGroup",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("total_count", "Total Count", WorkerMpValueKind.WholeNumber)];
    public static WorkerMpCommand CreateCommand(Api.GetNumberOfVectorsInVectorGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Vector Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroupName, "vector_group_name"), "SetCollectionObjectNameArg2")],
            [new("Total Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }
    public static Api.GetNumberOfVectorsInVectorGroupResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { TotalCount = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
            Execution = completed.Details };
}
