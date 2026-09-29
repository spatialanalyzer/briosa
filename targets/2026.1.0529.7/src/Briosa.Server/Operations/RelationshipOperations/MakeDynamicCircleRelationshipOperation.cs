using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakeDynamicCircleRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_dynamic_circle_relationship", "Make Dynamic Circle Relationship",
        "briosa.RelationshipOperations", "MakeDynamicCircleRelationship",
        "/briosa.RelationshipOperations/MakeDynamicCircleRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeDynamicCircleRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("Construction Mode", WorkerMpValueKind.DynamicCircleMode,
                    DynamicRelationshipModeMapper.Required(request.HasConstructionMode ? request.ConstructionMode : null, "construction_mode"), "SetDynamicCircleModeArg"),
                new("First Reference Geometry", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.FirstReferenceGeometry, "first_reference_geometry"), "SetCollectionObjectNameArg2"),
                new("Second Reference Geometry", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.SecondReferenceGeometry, "second_reference_geometry"), "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.MakeDynamicCircleRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
