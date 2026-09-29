using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class MakePlaneFitProfileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.make_plane_fit_profile", "Make Plane Fit Profile",
        "briosa.AnalysisOperations", "MakePlaneFitProfile", "/briosa.AnalysisOperations/MakePlaneFitProfile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakePlaneFitProfileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Fit Profile Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasFitProfileName ? request.FitProfileName : string.Empty), "SetStringArg"),
                new("Measured Side for Planar Offset", WorkerMpValueKind.MeasuredSideForPlanarOffset,
                    FitProfileChoiceMapper.MeasuredSideForPlanarOffset(request.HasMeasuredSideForPlanarOffset ? request.MeasuredSideForPlanarOffset : Api.MeasuredSideForPlanarOffset.AbovePlane, "measured_side_for_planar_offset"), "SetMeasuredSideForPlanarOffsetArg"),
                new("Override Planar Offset (-1.0 use current)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasOverridePlanarOffset ? request.OverridePlanarOffset : -1d), "SetDoubleArg"),
                new("Planar Offset Direction", WorkerMpValueKind.NormalDirection,
                    FitProfileChoiceMapper.NormalDirection(request.HasPlanarOffsetDirection ? request.PlanarOffsetDirection : Api.NormalDirection.ProbingDirection, "planar_offset_direction"), "SetNormalDirectionArg"),
                new("Reverse Normal Vector after fit?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasReverseNormalVectorAfterFit && request.ReverseNormalVectorAfterFit), "SetBoolArg"),
                new("Make Cardinal Points?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasMakeCardinalPoints || request.MakeCardinalPoints), "SetBoolArg"),
                new("Cardinal Pt.1: Centroid?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt1Centroid || request.CardinalPt1Centroid), "SetBoolArg"),
                new("Cardinal Pt.2: Point on Normal?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt2PointOnNormal || request.CardinalPt2PointOnNormal), "SetBoolArg")
            ], []);
    }

    public static Api.MakePlaneFitProfileResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
