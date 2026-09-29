using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetGeomRelationshipMeasuredGeometryOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_geom_relationship_measured_geometry", "Set Geom Relationship Measured Geometry",
        "briosa.RelationshipOperations", "SetGeomRelationshipMeasuredGeometry",
        "/briosa.RelationshipOperations/SetGeomRelationshipMeasuredGeometry",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetGeomRelationshipMeasuredGeometryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Measured Geometry", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.MeasuredGeometry, "measured_geometry"), "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.SetGeomRelationshipMeasuredGeometryResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
