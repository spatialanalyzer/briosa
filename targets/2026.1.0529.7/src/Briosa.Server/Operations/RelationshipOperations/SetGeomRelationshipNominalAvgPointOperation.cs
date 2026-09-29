using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetGeomRelationshipNominalAvgPointOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_geom_relationship_nominal_avg_point", "Set Geom Relationship Nominal Avg Point",
        "briosa.RelationshipOperations", "SetGeomRelationshipNominalAvgPoint",
        "/briosa.RelationshipOperations/SetGeomRelationshipNominalAvgPoint",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetGeomRelationshipNominalAvgPointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Compare To Nominal?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasCompareToNominal ? request.CompareToNominal : true), "SetBoolArg"),
                new("Nominal Average Point", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.NominalAveragePoint, "nominal_average_point"), "SetPointNameArg")
            ], []);
    }

    public static Api.SetGeomRelationshipNominalAvgPointResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
