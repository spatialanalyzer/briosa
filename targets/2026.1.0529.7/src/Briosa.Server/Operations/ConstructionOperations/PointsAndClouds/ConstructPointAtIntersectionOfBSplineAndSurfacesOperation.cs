using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointAtIntersectionOfBSplineAndSurfacesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_at_intersection_of_b_spline_and_surfaces", "Construct Point at intersection of B-Spline and Surfaces",
        "briosa.ConstructionOperations", "ConstructPointAtIntersectionOfBSplineAndSurfaces", "/briosa.ConstructionOperations/ConstructPointAtIntersectionOfBSplineAndSurfaces",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointAtIntersectionOfBSplineAndSurfacesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("B-Spline Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.BSplineName, "b_spline_name", WorkerObjectTypeValue.BSpline), "SetCollectionObjectNameArg2"),
            new("Surface List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.SurfaceList, "surface_list"), "SetCollectionObjectNameRefListArg"),
            new("Approximation Tolerance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasApproximationTolerance ? request.ApproximationTolerance : 0.001), "SetDoubleArg"),
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg")
        ], []);
    }

    public static Api.ConstructPointAtIntersectionOfBSplineAndSurfacesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
