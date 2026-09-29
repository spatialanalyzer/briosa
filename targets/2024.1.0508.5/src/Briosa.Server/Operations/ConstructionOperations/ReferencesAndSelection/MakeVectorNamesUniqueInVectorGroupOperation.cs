using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeVectorNamesUniqueInVectorGroupOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_vector_names_unique_in_vector_group",
        "Make Vector Names Unique in Vector Group", "briosa.ConstructionOperations",
        "MakeVectorNamesUniqueInVectorGroup", "/briosa.ConstructionOperations/MakeVectorNamesUniqueInVectorGroup",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeVectorNamesUniqueInVectorGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Vector Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroupName, "vector_group_name", WorkerObjectTypeValue.VectorGroup),
                "SetCollectionObjectNameArg2")], []);
    }

    public static Api.MakeVectorNamesUniqueInVectorGroupResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
