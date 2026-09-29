using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetGeomRelationshipNominalAvgPointOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_geom_relationship_nominal_avg_point", "Get Geom Relationship Nominal Avg Point",
        "briosa.RelationshipOperations", "GetGeomRelationshipNominalAvgPoint",
        "/briosa.RelationshipOperations/GetGeomRelationshipNominalAvgPoint",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [new("nominal_average_point", "Nominal Average Point", WorkerMpValueKind.PointName)];

    public static WorkerMpCommand CreateCommand(Api.GetGeomRelationshipNominalAvgPointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")],
            [new("Nominal Average Point", WorkerMpValueKind.PointName, "GetPointNameArg")]);
    }

    public static Api.GetGeomRelationshipNominalAvgPointResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            NominalAveragePoint = PointNameMapper.ToProtocol(
                completed.Execution.OutputValues[0].RequireValue<WorkerPointNameValue>()),
            Execution = completed.Details
        };
}
