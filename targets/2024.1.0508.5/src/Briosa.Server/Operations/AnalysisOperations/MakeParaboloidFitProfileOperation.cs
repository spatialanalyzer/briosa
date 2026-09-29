using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class MakeParaboloidFitProfileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.make_paraboloid_fit_profile", "Make Paraboloid Fit Profile",
        "briosa.AnalysisOperations", "MakeParaboloidFitProfile", "/briosa.AnalysisOperations/MakeParaboloidFitProfile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeParaboloidFitProfileRequest request)
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
                new("Lock Focal Length (-1.0 do not lock)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasLockFocalLength ? request.LockFocalLength : -1d), "SetDoubleArg"),
                new("Degree of Freedom", WorkerMpValueKind.DegreeOfFreedom,
                    FitProfileChoiceMapper.DegreeOfFreedom(request.HasDegreeOfFreedom ? request.DegreeOfFreedom : Api.DegreeOfFreedom.Any, "degree_of_freedom"), "SetDegreeOfFreedomArg"),
                new("Make Cardinal Points?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasMakeCardinalPoints || request.MakeCardinalPoints), "SetBoolArg"),
                new("Cardinal Pt.1: Vertex?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt1Vertex || request.CardinalPt1Vertex), "SetBoolArg"),
                new("Cardinal Pt.2: Focal Point?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt2FocalPoint || request.CardinalPt2FocalPoint), "SetBoolArg")
            ], []);
    }

    public static Api.MakeParaboloidFitProfileResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
