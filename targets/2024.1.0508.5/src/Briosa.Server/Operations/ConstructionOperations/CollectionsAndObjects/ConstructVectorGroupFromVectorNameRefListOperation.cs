using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructVectorGroupFromVectorNameRefListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_vector_group_from_vector_name_ref_list",
        "Construct a Vector Group From Vector Name Ref List", "briosa.ConstructionOperations",
        "ConstructVectorGroupFromVectorNameRefList", "/briosa.ConstructionOperations/ConstructVectorGroupFromVectorNameRefList",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructVectorGroupFromVectorNameRefListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ResultantVectorGroupName is null ||
            string.IsNullOrWhiteSpace(request.ResultantVectorGroupName.VectorGroupName))
            throw new ArgumentException("Request field 'resultant_vector_group_name' is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Vector Name List", WorkerMpValueKind.VectorNameList,
                VectorNameMapper.RequiredList(request.VectorNameList, "vector_name_list"), "SetVectorNameRefListArg"),
            new("Resultant Vector Group Name", WorkerMpValueKind.CollectionVectorGroupName,
                new WorkerCollectionVectorGroupNameValue(request.ResultantVectorGroupName.CollectionName,
                    request.ResultantVectorGroupName.VectorGroupName), "SetColVectorGroupNameArg")
        ], []);
    }

    public static Api.ConstructVectorGroupFromVectorNameRefListResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
