using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class CreatePointsToObjectsMapOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.create_points_to_objects_map", "Create Points to Objects Map",
        "briosa.RelationshipOperations", "CreatePointsToObjectsMap",
        "/briosa.RelationshipOperations/CreatePointsToObjectsMap",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CreatePointsToObjectsMapRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var points = request.Points?.Values ??
            throw new ArgumentException("Request field 'points' is required.", nameof(request));
        var groups = request.Groups?.Values ??
            throw new ArgumentException("Request field 'groups' is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Points", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(points, "points"), "SetPointNameRefListArg"),
                new("Groups", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(groups, "groups"), "SetCollectionObjectNameRefListArg"),
                new("Objects", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.Objects, "objects"), "SetCollectionObjectNameRefListArg"),
                new("Proximity Tolerance", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasProximityTolerance ? request.ProximityTolerance : 0.0), "SetDoubleArg"),
                new("Points to Objects Map Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasPointsToObjectsMapName ? request.PointsToObjectsMapName : "Empty"), "SetStringArg")
            ], []);
    }

    public static Api.CreatePointsToObjectsMapResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
