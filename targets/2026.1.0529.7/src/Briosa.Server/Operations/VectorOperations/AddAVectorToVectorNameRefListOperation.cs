using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.VectorOperations;

internal static class AddAVectorToVectorNameRefListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "vector_operations.add_a_vector_to_vector_name_ref_list", "Add a Vector To Vector Name Ref List",
        "briosa.VectorOperations", "AddAVectorToVectorNameRefList",
        "/briosa.VectorOperations/AddAVectorToVectorNameRefList",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.AddAVectorToVectorNameRefListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Vector Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroupName, "vector_group_name"), "SetCollectionObjectNameArg2"),
            new("Vector Name", WorkerMpValueKind.Text, new WorkerTextValue(request.VectorName), "SetStringArg"),
            new("Vector Name List", WorkerMpValueKind.VectorNameList,
                VectorNameMapper.RequiredList(request.VectorNameList, "vector_name_list"), "SetVectorNameRefListArg")
        ], []);
    }
    public static Api.AddAVectorToVectorNameRefListResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
