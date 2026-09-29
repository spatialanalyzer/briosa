using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetVectorGroupToVectorGroupRelativePolarityOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_vector_group_to_vector_group_relative_polarity",
        "Set Vector Group To Vector Group Relative Polarity",
        "briosa.RelationshipOperations", "SetVectorGroupToVectorGroupRelativePolarity",
        "/briosa.RelationshipOperations/SetVectorGroupToVectorGroupRelativePolarity",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetVectorGroupToVectorGroupRelativePolarityRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.VgToVgRelationship, "vg_to_vg_relationship", WorkerItemTypeValue.Relationship);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("VG To VG Relationship", WorkerMpValueKind.CollectionItemName,
                    relationship, "SetCollectionObjectNameArg2"),
                new("Set Opposing Vector Group Polarity", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasSetOpposingVectorGroupPolarity
                        ? request.SetOpposingVectorGroupPolarity : true), "SetBoolArg")
            ], []);
    }

    public static Api.SetVectorGroupToVectorGroupRelativePolarityResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
