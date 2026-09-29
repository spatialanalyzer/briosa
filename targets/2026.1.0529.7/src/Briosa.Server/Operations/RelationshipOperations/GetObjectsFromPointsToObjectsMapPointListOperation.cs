using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetObjectsFromPointsToObjectsMapPointListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_objects_from_points_to_objects_map_point_list",
        "Get Objects From Points to Objects Map (Point List)",
        "briosa.RelationshipOperations", "GetObjectsFromPointsToObjectsMapPointList",
        "/briosa.RelationshipOperations/GetObjectsFromPointsToObjectsMapPointList",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("objects", "Objects", WorkerMpValueKind.CollectionObjectNameList)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetObjectsFromPointsToObjectsMapPointListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var mapName = request.HasPointsToObjectsMapName ? request.PointsToObjectsMapName : "Empty";
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Points to Objects Map Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(mapName), "SetStringArg"),
                new("Points", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.Points, "points"), "SetPointNameRefListArg")
            ],
            [new("Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.GetObjectsFromPointsToObjectsMapPointListResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetObjectsFromPointsToObjectsMapPointListResult { Execution = completed.Details };
        result.Objects.AddRange(completed.Execution.OutputValues[0]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values
            .Select(CollectionObjectNameMapper.ToProtocol));
        return result;
    }
}