using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakeGroupToGroupRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_group_to_group_relationship", "Make Group to Group Relationship",
        "briosa.RelationshipOperations", "MakeGroupToGroupRelationship",
        "/briosa.RelationshipOperations/MakeGroupToGroupRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeGroupToGroupRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("First Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.FirstGroupName, "first_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
                new("Second Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.SecondGroupName, "second_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
                new("Auto Update a Vector Group?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasAutoUpdateAVectorGroup && request.AutoUpdateAVectorGroup), "SetBoolArg"),
                new("Tolerance", WorkerMpValueKind.ToleranceVectorOptions,
                    ToleranceVectorOptionsMapper.Required(request.Tolerance, "tolerance"), "SetToleranceVectorOptionsArg"),
                new("Constraint", WorkerMpValueKind.ToleranceVectorOptions,
                    ToleranceVectorOptionsMapper.Required(request.Constraint, "constraint"), "SetToleranceVectorOptionsArg")
            ], []);
    }

    public static Api.MakeGroupToGroupRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
