using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakeVectorGroupToVectorGroupRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_vector_group_to_vector_group_relationship", "Make Vector Group To Vector Group Relationship",
        "briosa.RelationshipOperations", "MakeVectorGroupToVectorGroupRelationship",
        "/briosa.RelationshipOperations/MakeVectorGroupToVectorGroupRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeVectorGroupToVectorGroupRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("New VG To VG Relationship", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.NewVgToVgRelationship, "new_vg_to_vg_relationship", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("Reference Vector Group", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ReferenceVectorGroup, "reference_vector_group", WorkerObjectTypeValue.VectorGroup), "SetCollectionObjectNameArg2"),
                new("Corresponding Vector Group", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.CorrespondingVectorGroup, "corresponding_vector_group", WorkerObjectTypeValue.VectorGroup), "SetCollectionObjectNameArg2"),
                new("Set Opposing Vector Group Polarity", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasSetOpposingVectorGroupPolarity ? request.SetOpposingVectorGroupPolarity : true), "SetBoolArg")
            ], []);
    }

    public static Api.MakeVectorGroupToVectorGroupRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
