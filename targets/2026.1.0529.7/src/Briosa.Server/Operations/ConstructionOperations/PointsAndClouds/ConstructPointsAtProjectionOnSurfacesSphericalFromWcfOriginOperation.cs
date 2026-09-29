using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOriginOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_at_projection_on_surfaces_spherical_from_wcf_origin",
        "Construct Points at Projection on Surfaces - Spherical from WCF Origin", "briosa.ConstructionOperations",
        "ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOrigin",
        "/briosa.ConstructionOperations/ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOrigin",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOriginRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Surface List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.SurfaceList, "surface_list"), "SetCollectionObjectNameRefListArg"),
            new("Point Names", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.PointNames, "point_names"), "SetPointNameRefListArg"),
            new("Group Name to Contain New Points", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasGroupNameToContainNewPoints ? request.GroupNameToContainNewPoints : ""), "SetStringArg"),
            new("Point Name Prefix", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasPointNamePrefix ? request.PointNamePrefix : ""), "SetStringArg"),
            new("Point Name Suffix", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasPointNameSuffix ? request.PointNameSuffix : ""), "SetStringArg")
        ], []);
    }

    public static Api.ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOriginResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
