using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetGeomRelationshipNominalGeometryOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_geom_relationship_nominal_geometry", "Set Geom Relationship Nominal Geometry",
        "briosa.RelationshipOperations", "SetGeomRelationshipNominalGeometry",
        "/briosa.RelationshipOperations/SetGeomRelationshipNominalGeometry",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetGeomRelationshipNominalGeometryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Compare To Nominal?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasCompareToNominal ? request.CompareToNominal : true), "SetBoolArg"),
                new("Nominal Geometry", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.NominalGeometry, "nominal_geometry"), "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.SetGeomRelationshipNominalGeometryResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
