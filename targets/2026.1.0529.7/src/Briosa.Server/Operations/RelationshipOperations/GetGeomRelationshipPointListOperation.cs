using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetGeomRelationshipPointListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_geom_relationship_point_list", "Get Geom Relationship Point List",
        "briosa.RelationshipOperations", "GetGeomRelationshipPointList",
        "/briosa.RelationshipOperations/GetGeomRelationshipPointList",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("all_points", "All Points", WorkerMpValueKind.PointNameList),
        new("used_points", "Used Points", WorkerMpValueKind.PointNameList),
        new("ignored_points", "Ignored Points", WorkerMpValueKind.PointNameList)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetGeomRelationshipPointListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")],
            [
                new("All Points", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg"),
                new("Used Points", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg"),
                new("Ignored Points", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg")
            ]);
    }

    public static Api.GetGeomRelationshipPointListResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        var result = new Api.GetGeomRelationshipPointListResult { Execution = completed.Details };
        AddPoints(result.AllPoints, outputs[0]);
        AddPoints(result.UsedPoints, outputs[1]);
        AddPoints(result.IgnoredPoints, outputs[2]);
        return result;
    }

    private static void AddPoints(
        Google.Protobuf.Collections.RepeatedField<Api.PointName> destination,
        WorkerMpOutputValue output)
    {
        foreach (var point in output.RequireValue<WorkerPointNameListValue>().Values)
        {
            destination.Add(PointNameMapper.ToProtocol(point));
        }
    }
}
