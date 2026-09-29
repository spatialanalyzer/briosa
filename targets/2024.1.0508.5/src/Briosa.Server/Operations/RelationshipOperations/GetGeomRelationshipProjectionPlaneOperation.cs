using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetGeomRelationshipProjectionPlaneOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_geom_relationship_projection_plane", "Get Geom Relationship Projection Plane",
        "briosa.RelationshipOperations", "GetGeomRelationshipProjectionPlane",
        "/briosa.RelationshipOperations/GetGeomRelationshipProjectionPlane",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [new("projection_plane_name", "Projection Plane Name", WorkerMpValueKind.CollectionObjectName)];

    public static WorkerMpCommand CreateCommand(Api.GetGeomRelationshipProjectionPlaneRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")],
            [new("Projection Plane Name", WorkerMpValueKind.CollectionObjectName,
                "GetCollectionObjectNameArg", WorkerObjectTypeValue.Plane)]);
    }

    public static Api.GetGeomRelationshipProjectionPlaneResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            ProjectionPlaneName = CollectionObjectNameMapper.ToProtocol(
                completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameValue>()),
            Execution = completed.Details
        };
}
