using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPolygonizedSurfaceFromPointCloudsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_polygonized_surface_from_point_clouds", "Construct Polygonized Surface from Point Clouds",
        "briosa.ConstructionOperations", "ConstructPolygonizedSurfaceFromPointClouds",
        "/briosa.ConstructionOperations/ConstructPolygonizedSurfaceFromPointClouds",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPolygonizedSurfaceFromPointCloudsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var orientation = request.MeshOrientation switch
        {
            Api.MeshOrientationType.UseCurrentPointOfView => "Use Current Point of View",
            Api.MeshOrientationType.UseCurrentWorkingFrame => "Use Current Working Frame",
            _ => throw new ArgumentException("A supported mesh_orientation is required.", nameof(request))
        };
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Cloud List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.PointCloudList, "point_cloud_list", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameRefListArg"),
            new("Mesh Orientation", WorkerMpValueKind.Text, new WorkerTextValue(orientation), "SetStringArg"),
            new("Grid Resolution", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasGridResolution ? request.GridResolution : 0), "SetDoubleArg"),
            new("Polygonized Surface Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.PolygonizedSurfaceName, "polygonized_surface_name", WorkerObjectTypeValue.ScanStripeMesh), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructPolygonizedSurfaceFromPointCloudsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
