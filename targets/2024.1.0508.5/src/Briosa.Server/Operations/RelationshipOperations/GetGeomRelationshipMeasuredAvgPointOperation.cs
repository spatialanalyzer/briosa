using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetGeomRelationshipMeasuredAvgPointOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_geom_relationship_measured_avg_point", "Get Geom Relationship Measured Avg Point",
        "briosa.RelationshipOperations", "GetGeomRelationshipMeasuredAvgPoint",
        "/briosa.RelationshipOperations/GetGeomRelationshipMeasuredAvgPoint",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [new("measured_average_point", "Measured Average Point", WorkerMpValueKind.PointName)];

    public static WorkerMpCommand CreateCommand(Api.GetGeomRelationshipMeasuredAvgPointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")],
            [new("Measured Average Point", WorkerMpValueKind.PointName, "GetPointNameArg")]);
    }

    public static Api.GetGeomRelationshipMeasuredAvgPointResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            MeasuredAveragePoint = PointNameMapper.ToProtocol(
                completed.Execution.OutputValues[0].RequireValue<WorkerPointNameValue>()),
            Execution = completed.Details
        };
}
