using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakeDynamicPointRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_dynamic_point_relationship", "Make Dynamic Point Relationship",
        "briosa.RelationshipOperations", "MakeDynamicPointRelationship",
        "/briosa.RelationshipOperations/MakeDynamicPointRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeDynamicPointRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("Construction Mode", WorkerMpValueKind.DynamicPointMode,
                    DynamicRelationshipModeMapper.Required(request.HasConstructionMode ? request.ConstructionMode : null, "construction_mode"), "SetDynamicPointModeArg"),
                new("First Reference Geometry", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.FirstReferenceGeometry, "first_reference_geometry"), "SetCollectionObjectNameArg2"),
                new("Second Reference Geometry", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.SecondReferenceGeometry, "second_reference_geometry"), "SetCollectionObjectNameArg2"),
                new("Third Reference Geometry", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ThirdReferenceGeometry, "third_reference_geometry"), "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.MakeDynamicPointRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
