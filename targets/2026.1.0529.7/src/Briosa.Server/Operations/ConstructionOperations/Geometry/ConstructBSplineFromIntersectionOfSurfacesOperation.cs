using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructBSplineFromIntersectionOfSurfacesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_b_spline_from_intersection_of_surfaces",
        "Construct B-Spline From Intersection of Surfaces", "briosa.ConstructionOperations",
        "ConstructBSplineFromIntersectionOfSurfaces",
        "/briosa.ConstructionOperations/ConstructBSplineFromIntersectionOfSurfaces",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructBSplineFromIntersectionOfSurfacesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Resulting B-Spline Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingBSplineName, "resulting_b_spline_name", WorkerObjectTypeValue.BSpline), "SetCollectionObjectNameArg2"),
            new("First Surface Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.FirstSurfaceName, "first_surface_name", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2"),
            new("Second Surface Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SecondSurfaceName, "second_surface_name", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2"),
            new("Approximation Tolerance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasApproximationTolerance ? request.ApproximationTolerance : 0.0001), "SetDoubleArg")
        ], []);
    }

    public static Api.ConstructBSplineFromIntersectionOfSurfacesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
