using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class MakeSphereFitProfileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.make_sphere_fit_profile", "Make Sphere Fit Profile",
        "briosa.AnalysisOperations", "MakeSphereFitProfile", "/briosa.AnalysisOperations/MakeSphereFitProfile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeSphereFitProfileRequest request)
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
                new("Lock Radius (-1.0 do not lock)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasLockRadius ? request.LockRadius : -1d), "SetDoubleArg"),
                new("Make Cardinal Points?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasMakeCardinalPoints || request.MakeCardinalPoints), "SetBoolArg"),
                new("Cardinal Pt.1: Center?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt1Center || request.CardinalPt1Center), "SetBoolArg"),
                new("Computation Method", WorkerMpValueKind.SphereFitComputationMode,
                    FitProfileChoiceMapper.SphereFitComputationMode(request.HasComputationMethod ? request.ComputationMethod : Api.SphereFitComputationMode.Standard, "computation_method"), "SetSphereFitComputationModeArg")
            ], []);
    }

    public static Api.MakeSphereFitProfileResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
