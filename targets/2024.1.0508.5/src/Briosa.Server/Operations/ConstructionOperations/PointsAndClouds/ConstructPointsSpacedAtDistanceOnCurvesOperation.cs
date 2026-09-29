using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsSpacedAtDistanceOnCurvesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_spaced_at_distance_on_curves", "Construct Points Spaced at a Distance on Curves",
        "briosa.ConstructionOperations", "ConstructPointsSpacedAtDistanceOnCurves", "/briosa.ConstructionOperations/ConstructPointsSpacedAtDistanceOnCurves",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsSpacedAtDistanceOnCurvesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("B-Spline List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.BSplineList, "b_spline_list"), "SetCollectionObjectNameRefListArg"),
            new("Distance Between Points", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasDistanceBetweenPoints ? request.DistanceBetweenPoints : 0.5), "SetDoubleArg"),
            new("Resultant Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultantGroupName, "resultant_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Resultant Point Name Prefix", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasResultantPointNamePrefix ? request.ResultantPointNamePrefix : string.Empty), "SetStringArg")
        ], []);
    }

    public static Api.ConstructPointsSpacedAtDistanceOnCurvesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
