using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetGeomRelationshipCardinalPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_geom_relationship_cardinal_points", "Get Geom Relationship Cardinal Points",
        "briosa.RelationshipOperations", "GetGeomRelationshipCardinalPoints",
        "/briosa.RelationshipOperations/GetGeomRelationshipCardinalPoints",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [new("cardinal_point_name_list", "Cardinal Point Name List", WorkerMpValueKind.PointNameList)];

    public static WorkerMpCommand CreateCommand(Api.GetGeomRelationshipCardinalPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")],
            [new("Cardinal Point Name List", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg")]);
    }

    public static Api.GetGeomRelationshipCardinalPointsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetGeomRelationshipCardinalPointsResult { Execution = completed.Details };
        foreach (var point in completed.Execution.OutputValues[0].RequireValue<WorkerPointNameListValue>().Values)
        {
            result.CardinalPointNameList.Add(PointNameMapper.ToProtocol(point));
        }

        return result;
    }
}
