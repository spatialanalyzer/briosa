using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class MakeConeFitProfileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.make_cone_fit_profile", "Make Cone Fit Profile",
        "briosa.AnalysisOperations", "MakeConeFitProfile", "/briosa.AnalysisOperations/MakeConeFitProfile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeConeFitProfileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Fit Profile Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasFitProfileName ? request.FitProfileName : string.Empty), "SetStringArg"),
                new("Measured Side for Radial Offset", WorkerMpValueKind.MeasuredSideForRadialOffset,
                    FitProfileChoiceMapper.MeasuredSideForRadialOffset(request.HasMeasuredSideForRadialOffset ? request.MeasuredSideForRadialOffset : Api.MeasuredSideForRadialOffset.Outside, "measured_side_for_radial_offset"), "SetMeasuredSideForRadialOffsetArg"),
                new("Override Radial Offset (-1.0 use current)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasOverrideRadialOffset ? request.OverrideRadialOffset : -1d), "SetDoubleArg"),
                new("Lock Angle in degrees (-1.0 do not lock)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasLockAngleInDegrees ? request.LockAngleInDegrees : -1d), "SetDoubleArg"),
                new("Use Exhaustive Search?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasUseExhaustiveSearch || request.UseExhaustiveSearch), "SetBoolArg"),
                new("Make Cardinal Points?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasMakeCardinalPoints || request.MakeCardinalPoints), "SetBoolArg"),
                new("Cardinal Pt.1: Vertex?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt1Vertex || request.CardinalPt1Vertex), "SetBoolArg"),
                new("Cardinal Pt.2: Point on Axis?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt2PointOnAxis || request.CardinalPt2PointOnAxis), "SetBoolArg"),
                new("Cardinal Pt.3: Cut Point on Axis?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt3CutPointOnAxis || request.CardinalPt3CutPointOnAxis), "SetBoolArg")
            ], []);
    }

    public static Api.MakeConeFitProfileResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
