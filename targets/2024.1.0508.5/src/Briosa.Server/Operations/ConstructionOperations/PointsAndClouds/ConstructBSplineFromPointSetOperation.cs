using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructBSplineFromPointSetOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_b_spline_from_point_set", "Construct B-Spline From Point Set",
        "briosa.ConstructionOperations", "ConstructBSplineFromPointSet", "/briosa.ConstructionOperations/ConstructBSplineFromPointSet",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructBSplineFromPointSetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Resulting B-Spline Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingBSplineName, "resulting_b_spline_name", WorkerObjectTypeValue.BSpline), "SetCollectionObjectNameArg2"),
            new("B-Spline Fit Options", WorkerMpValueKind.BSplineFitOptions,
                BSplineFitOptionsMapper.Map(request.BSplineFitOptions), "SetBSplineFitOptionsArg"),
            new("Point Set Container", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.PointSetContainer, "point_set_container", WorkerObjectTypeValue.PointSet), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructBSplineFromPointSetResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
