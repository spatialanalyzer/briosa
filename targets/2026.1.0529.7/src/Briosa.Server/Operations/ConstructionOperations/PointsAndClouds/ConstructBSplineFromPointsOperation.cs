using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructBSplineFromPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_b_spline_from_points", "Construct B-Spline From Points",
        "briosa.ConstructionOperations", "ConstructBSplineFromPoints", "/briosa.ConstructionOperations/ConstructBSplineFromPoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructBSplineFromPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Resulting B-Spline Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingBSplineName, "resulting_b_spline_name", WorkerObjectTypeValue.BSpline), "SetCollectionObjectNameArg2"),
            new("B-Spline Fit Options", WorkerMpValueKind.BSplineFitOptions,
                BSplineFitOptionsMapper.Map(request.BSplineFitOptions), "SetBSplineFitOptionsArg"),
            new("Point List", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.PointList, "point_list"), "SetPointNameRefListArg")
        ], []);
    }

    public static Api.ConstructBSplineFromPointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
