using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class AutoFilterCloudsToNominalGeometry2DOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.auto_filter_clouds_to_nominal_geometry_2d", "Auto Filter Clouds to Nominal Geometry 2D",
        "briosa.RelationshipOperations", "AutoFilterCloudsToNominalGeometry2D",
        "/briosa.RelationshipOperations/AutoFilterCloudsToNominalGeometry2D",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AutoFilterCloudsToNominalGeometry2DRequest request)
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
                new("Filter Proximity Settings 2D", WorkerMpValueKind.AutoFilterProximitySettings,
                    RelationshipOperationValueMapper.RequiredProximity(request.FilterProximitySettings2D,
                        "filter_proximity_settings_2D"), "SetAutoFilterProximitySettingsArg"),
                new("Geometry Extraction Tolerance", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasGeometryExtractionTolerance ? request.GeometryExtractionTolerance : 0.01),
                    "SetDoubleArg"),
                new("Use Feature Specific Filter Settings?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasUseFeatureSpecificFilterSettings &&
                        request.UseFeatureSpecificFilterSettings), "SetBoolArg")
            ], []);
    }

    public static Api.AutoFilterCloudsToNominalGeometry2DResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
