using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetGeomRelationshipNominalGeometryOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_geom_relationship_nominal_geometry", "Get Geom Relationship Nominal Geometry",
        "briosa.RelationshipOperations", "GetGeomRelationshipNominalGeometry",
        "/briosa.RelationshipOperations/GetGeomRelationshipNominalGeometry",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [new("nominal_geometry", "Nominal Geometry", WorkerMpValueKind.CollectionObjectName)];

    public static WorkerMpCommand CreateCommand(Api.GetGeomRelationshipNominalGeometryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")],
            [new("Nominal Geometry", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg")]);
    }

    public static Api.GetGeomRelationshipNominalGeometryResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            NominalGeometry = CollectionObjectNameMapper.ToProtocol(
                completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameValue>()),
            Execution = completed.Details
        };
}
