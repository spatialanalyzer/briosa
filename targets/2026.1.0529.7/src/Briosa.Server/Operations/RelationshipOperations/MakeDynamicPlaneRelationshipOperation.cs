using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakeDynamicPlaneRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_dynamic_plane_relationship", "Make Dynamic Plane Relationship",
        "briosa.RelationshipOperations", "MakeDynamicPlaneRelationship",
        "/briosa.RelationshipOperations/MakeDynamicPlaneRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeDynamicPlaneRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("Construction Mode", WorkerMpValueKind.DynamicPlaneMode,
                    DynamicRelationshipModeMapper.Required(request.HasConstructionMode ? request.ConstructionMode : null, "construction_mode"), "SetDynamicPlaneModeArg"),
                new("First Reference Geometry", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.FirstReferenceGeometry, "first_reference_geometry"), "SetCollectionObjectNameArg2"),
                new("Second Reference Geometry", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.SecondReferenceGeometry, "second_reference_geometry"), "SetCollectionObjectNameArg2"),
                new("Offset Plane Offset", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasOffsetPlaneOffset ? request.OffsetPlaneOffset : 0d), "SetDoubleArg")
            ], []);
    }

    public static Api.MakeDynamicPlaneRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
