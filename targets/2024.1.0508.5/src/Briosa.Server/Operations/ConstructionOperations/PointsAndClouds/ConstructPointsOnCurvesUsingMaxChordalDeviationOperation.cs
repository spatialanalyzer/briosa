using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsOnCurvesUsingMaxChordalDeviationOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_on_curves_using_max_chordal_deviation", "Construct Points on Curves Using Max Chordal Deviation",
        "briosa.ConstructionOperations", "ConstructPointsOnCurvesUsingMaxChordalDeviation", "/briosa.ConstructionOperations/ConstructPointsOnCurvesUsingMaxChordalDeviation",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsOnCurvesUsingMaxChordalDeviationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("B-Spline List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.BSplineList, "b_spline_list"), "SetCollectionObjectNameRefListArg"),
            new("Maximum Chordal Deviation", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMaximumChordalDeviation ? request.MaximumChordalDeviation : 0.05), "SetDoubleArg"),
            new("Maximum Trim Edge Angle", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMaximumTrimEdgeAngle ? request.MaximumTrimEdgeAngle : 15), "SetDoubleArg"),
            new("Maximum Chord Length", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMaximumChordLength ? request.MaximumChordLength : 0), "SetDoubleArg"),
            new("Resultant Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultantGroupName, "resultant_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Resultant Point Name Prefix", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasResultantPointNamePrefix ? request.ResultantPointNamePrefix : string.Empty), "SetStringArg")
        ], []);
    }

    public static Api.ConstructPointsOnCurvesUsingMaxChordalDeviationResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
