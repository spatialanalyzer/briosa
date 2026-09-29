using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class AutoFilterPointsToNominalGeometry3DOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.auto_filter_points_to_nominal_geometry_3d",
        "Auto Filter Points to Nominal Geometry 3D",
        "briosa.RelationshipOperations", "AutoFilterPointsToNominalGeometry3D",
        "/briosa.RelationshipOperations/AutoFilterPointsToNominalGeometry3D",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AutoFilterPointsToNominalGeometry3DRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Auto Filter Target Relationships", WorkerMpValueKind.CollectionItemNameList,
                    RelationshipOperationValueMapper.RequiredRelationships(request.AutoFilterTargetRelationships,
                        "auto_filter_target_relationships"), "SetCollectionObjectNameRefListArg"),
                new("Points", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.Points, "points"), "SetPointNameRefListArg"),
                new("Filter Proximity Settings 3D", WorkerMpValueKind.AutoFilterProximitySettings,
                    RelationshipOperationValueMapper.RequiredProximity(request.FilterProximitySettings3D,
                        "filter_proximity_settings_3d"), "SetAutoFilterProximitySettingsArg")
            ], []);
    }

    public static Api.AutoFilterPointsToNominalGeometry3DResult CreateResult(
        SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
