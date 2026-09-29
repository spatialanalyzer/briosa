using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class AutoFilterCloudsToNominalGeometry3DOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.auto_filter_clouds_to_nominal_geometry_3d", "Auto Filter Clouds to Nominal Geometry 3D",
        "briosa.RelationshipOperations", "AutoFilterCloudsToNominalGeometry3D",
        "/briosa.RelationshipOperations/AutoFilterCloudsToNominalGeometry3D",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AutoFilterCloudsToNominalGeometry3DRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Auto Filter Target Relationships", WorkerMpValueKind.CollectionItemNameList,
                    RelationshipOperationValueMapper.RequiredRelationships(request.AutoFilterTargetRelationships,
                        "auto_filter_target_relationships"), "SetCollectionObjectNameRefListArg"),
                new("Clouds", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.Clouds, "clouds"), "SetCollectionObjectNameRefListArg"),
                new("Cloud Thinning Settings", WorkerMpValueKind.CloudThinningOptions,
                    CloudThinningOptionsMapper.ToWorker(request.CloudThinningSettings), "SetCloudThinningOptionsArg"),
                new("Filter Proximity Settings 3D", WorkerMpValueKind.AutoFilterProximitySettings,
                    RelationshipOperationValueMapper.RequiredProximity(request.FilterProximitySettings3D,
                        "filter_proximity_settings_3D"), "SetAutoFilterProximitySettingsArg"),
                new("Use Feature Specific Filter Settings?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasUseFeatureSpecificFilterSettings &&
                        request.UseFeatureSpecificFilterSettings), "SetBoolArg")
            ], []);
    }

    public static Api.AutoFilterCloudsToNominalGeometry3DResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
