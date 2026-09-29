using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class AutoFilterPointsGroupsCloudsToSurfaceFacesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.auto_filter_points_groups_clouds_to_surface_faces",
        "Auto Filter Points/Groups/Clouds to Surface Faces",
        "briosa.RelationshipOperations", "AutoFilterPointsGroupsCloudsToSurfaceFaces",
        "/briosa.RelationshipOperations/AutoFilterPointsGroupsCloudsToSurfaceFaces",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AutoFilterPointsGroupsCloudsToSurfaceFacesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var points = request.Points?.Values ??
            throw new ArgumentException("Request field 'points' is required.", nameof(request));
        var groups = request.Groups?.Values ??
            throw new ArgumentException("Request field 'groups' is required.", nameof(request));
        var clouds = request.Clouds?.Values ??
            throw new ArgumentException("Request field 'clouds' is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Points", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(points, "points"), "SetPointNameRefListArg"),
                new("Groups", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(groups, "groups"), "SetCollectionObjectNameRefListArg"),
                new("Clouds", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(clouds, "clouds"), "SetCollectionObjectNameRefListArg"),
                new("Surface Offset", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasSurfaceOffset ? request.SurfaceOffset : 0.1), "SetDoubleArg"),
                new("Edge Offset", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasEdgeOffset ? request.EdgeOffset : 0.1), "SetDoubleArg"),
                new("Offset Direction", WorkerMpValueKind.OffsetDirectionType,
                    RelationshipOperationValueMapper.RequiredOffsetDirection(
                        request.HasOffsetDirection ? request.OffsetDirection : null, "offset_direction"),
                    "SetOffsetDirectionTypeArg"),
                new("Enforce Max Pts per Face in Output?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasEnforceMaxPointsPerFaceInOutput &&
                        request.EnforceMaxPointsPerFaceInOutput), "SetBoolArg"),
                new("Max Pts per Face", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasMaxPointsPerFace ? request.MaxPointsPerFace : 0), "SetIntegerArg"),
                new("Surfaces", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.Surfaces, "surfaces"), "SetCollectionObjectNameRefListArg"),
                new("Cloud Thinning Settings", WorkerMpValueKind.CloudThinningOptions,
                    CloudThinningOptionsMapper.ToWorker(request.CloudThinningSettings), "SetCloudThinningOptionsArg"),
                new("Output Cloud Base Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasOutputCloudBaseName ? request.OutputCloudBaseName : "InspAutoFilteredCloud"),
                    "SetStringArg"),
                new("Use Face IDs for suffix", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasUseFaceIdsForSuffix || request.UseFaceIdsForSuffix), "SetBoolArg")
            ], []);
    }

    public static Api.AutoFilterPointsGroupsCloudsToSurfaceFacesResult CreateResult(
        SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
