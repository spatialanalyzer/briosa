using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class MakeSlotFitProfileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.make_slot_fit_profile", "Make Slot Fit Profile",
        "briosa.AnalysisOperations", "MakeSlotFitProfile", "/briosa.AnalysisOperations/MakeSlotFitProfile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeSlotFitProfileRequest request)
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
                new("Measured Side for Planar Offset", WorkerMpValueKind.MeasuredSideForPlanarOffset,
                    FitProfileChoiceMapper.MeasuredSideForPlanarOffset(request.HasMeasuredSideForPlanarOffset ? request.MeasuredSideForPlanarOffset : Api.MeasuredSideForPlanarOffset.AbovePlane, "measured_side_for_planar_offset"), "SetMeasuredSideForPlanarOffsetArg"),
                new("Override Planar Offset (-1.0 use current)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasOverridePlanarOffset ? request.OverridePlanarOffset : -1d), "SetDoubleArg"),
                new("Planar Offset Direction", WorkerMpValueKind.NormalDirection,
                    FitProfileChoiceMapper.NormalDirection(request.HasPlanarOffsetDirection ? request.PlanarOffsetDirection : Api.NormalDirection.ProbingDirection, "planar_offset_direction"), "SetNormalDirectionArg"),
                new("Slot Type", WorkerMpValueKind.SlotType,
                    FitProfileChoiceMapper.SlotType(request.HasSlotType ? request.SlotType : Api.SlotType.Round, "slot_type"), "SetSlotTypeArg"),
                new("Slot Computation Technique", WorkerMpValueKind.CompTechnique,
                    FitProfileChoiceMapper.CompTechnique(request.HasSlotComputationTechnique ? request.SlotComputationTechnique : Api.CompTechnique.Standard, "slot_computation_technique"), "SetCompTechniqueArg"),
                new("Reverse Normal Vector after fit?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasReverseNormalVectorAfterFit && request.ReverseNormalVectorAfterFit), "SetBoolArg"),
                new("Make Cardinal Points?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasMakeCardinalPoints || request.MakeCardinalPoints), "SetBoolArg"),
                new("Cardinal Pt.1: Center?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt1Center || request.CardinalPt1Center), "SetBoolArg"),
                new("Cardinal Pt.2: Point on Normal?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt2PointOnNormal || request.CardinalPt2PointOnNormal), "SetBoolArg"),
                new("Cardinal Pt.3: Centerline Pt.1?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt3CenterlinePt1 || request.CardinalPt3CenterlinePt1), "SetBoolArg"),
                new("Cardinal Pt.4: Centerline Pt.2?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt4CenterlinePt2 || request.CardinalPt4CenterlinePt2), "SetBoolArg")
            ], []);
    }

    public static Api.MakeSlotFitProfileResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
