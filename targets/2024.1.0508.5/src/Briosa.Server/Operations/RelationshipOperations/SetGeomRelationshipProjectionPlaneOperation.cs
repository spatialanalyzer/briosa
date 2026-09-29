using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetGeomRelationshipProjectionPlaneOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_geom_relationship_projection_plane", "Set Geom Relationship Projection Plane",
        "briosa.RelationshipOperations", "SetGeomRelationshipProjectionPlane",
        "/briosa.RelationshipOperations/SetGeomRelationshipProjectionPlane",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetGeomRelationshipProjectionPlaneRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Project to Plane?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasProjectToPlane ? request.ProjectToPlane : true), "SetBoolArg"),
                new("Projection Plane Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ProjectionPlaneName, "projection_plane_name"), "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.SetGeomRelationshipProjectionPlaneResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
