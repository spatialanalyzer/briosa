using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class MakeCylinderFitProfileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.make_cylinder_fit_profile", "Make Cylinder Fit Profile",
        "briosa.AnalysisOperations", "MakeCylinderFitProfile", "/briosa.AnalysisOperations/MakeCylinderFitProfile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeCylinderFitProfileRequest request)
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
                new("Locked Radius Fit Method", WorkerMpValueKind.FitMethod,
                    FitProfileChoiceMapper.FitMethod(request.HasLockedRadiusFitMethod ? request.LockedRadiusFitMethod : Api.FitMethod.MinimumRms, "locked_radius_fit_method"), "SetFitMethodArg"),
                new("Constrain to Nominal Axis?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasConstrainToNominalAxis && request.ConstrainToNominalAxis), "SetBoolArg"),
                new("Constrain to Nominal Orientation?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasConstrainToNominalOrientation && request.ConstrainToNominalOrientation), "SetBoolArg"),
                new("Align with Nominal?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasAlignWithNominal && request.AlignWithNominal), "SetBoolArg"),
                new("Reverse Axis?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasReverseAxis && request.ReverseAxis), "SetBoolArg"),
                new("Set Axis First to Last Point?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasSetAxisFirstToLastPoint && request.SetAxisFirstToLastPoint), "SetBoolArg"),
                new("Cylinder Computation Technique", WorkerMpValueKind.CompTechnique,
                    FitProfileChoiceMapper.CompTechnique(request.HasCylinderComputationTechnique ? request.CylinderComputationTechnique : Api.CompTechnique.Standard, "cylinder_computation_technique"), "SetCompTechniqueArg"),
                new("Use Exhaustive Search?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasUseExhaustiveSearch && request.UseExhaustiveSearch), "SetBoolArg"),
                new("Make Cardinal Points?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasMakeCardinalPoints || request.MakeCardinalPoints), "SetBoolArg"),
                new("Cardinal Pt.1: Begin Pt?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt1BeginPt || request.CardinalPt1BeginPt), "SetBoolArg"),
                new("Cardinal Pt.2: End Pt?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt2EndPt || request.CardinalPt2EndPt), "SetBoolArg"),
                new("Cardinal Pt.3: Center?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt3Center || request.CardinalPt3Center), "SetBoolArg")
            ], []);
    }

    public static Api.MakeCylinderFitProfileResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
