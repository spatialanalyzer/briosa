using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class ExtractGeometryFromPointCloudsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.extract_geometry_from_point_clouds", "Extract Geometry From Point Clouds",
        "briosa.RelationshipOperations", "ExtractGeometryFromPointClouds",
        "/briosa.RelationshipOperations/ExtractGeometryFromPointClouds",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ExtractGeometryFromPointCloudsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.HasGeometryType)
            throw new ArgumentException("Request field 'geometry_type' is required.", nameof(request));
        var boundingPoints = request.BoundingPoints?.Values ??
            throw new ArgumentException("Request field 'bounding_points' is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("Geometry Type", WorkerMpValueKind.GeometryType,
                    GeometryTypeMapper.Required(request.GeometryType, "geometry_type"), "SetGeometryTypeArg"),
                new("Cloud Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.CloudName, "cloud_name", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2"),
                new("Bounding Points", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(boundingPoints, "bounding_points"), "SetPointNameRefListArg"),
                new("Seed Points", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.SeedPoints, "seed_points"), "SetPointNameRefListArg"),
                new("Tolerance", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasTolerance ? request.Tolerance : 0.1), "SetDoubleArg"),
                new("Reverse Normal", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasReverseNormal && request.ReverseNormal), "SetBoolArg"),
                new("Planar Point Count", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasPlanarPointCount ? request.PlanarPointCount : 1000), "SetIntegerArg")
            ], []);
    }

    public static Api.ExtractGeometryFromPointCloudsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
