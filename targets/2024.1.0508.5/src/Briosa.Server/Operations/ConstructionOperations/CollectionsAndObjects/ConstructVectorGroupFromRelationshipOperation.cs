using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructVectorGroupFromRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_vector_group_from_relationship", "Construct a Vector Group From a Relationship",
        "briosa.ConstructionOperations", "ConstructVectorGroupFromRelationship",
        "/briosa.ConstructionOperations/ConstructVectorGroupFromRelationship", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructVectorGroupFromRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.VectorGroupName is null || string.IsNullOrWhiteSpace(request.VectorGroupName.VectorGroupName))
            throw new ArgumentException("Vector group name is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
            new("Vector Group Name", WorkerMpValueKind.CollectionVectorGroupName,
                new WorkerCollectionVectorGroupNameValue(request.VectorGroupName.CollectionName,
                    request.VectorGroupName.VectorGroupName), "SetColVectorGroupNameArg")
        ], []);
    }

    public static Api.ConstructVectorGroupFromRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
