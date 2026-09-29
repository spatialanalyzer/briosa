using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class DynamicRelationshipModeMapper
{
    public static WorkerChoiceValue<WorkerDynamicCircleModeValue> Required(Api.DynamicCircleMode? value, string field) =>
        new(value switch
        {
            Api.DynamicCircleMode.CylinderAndPlaneHoldPlaneNormal => WorkerDynamicCircleModeValue.CylinderPlaneHoldPlaneNormal,
            Api.DynamicCircleMode.CylinderAndPlaneHoldCylinderAxis => WorkerDynamicCircleModeValue.CylinderPlaneHoldCylinderAxis,
            Api.DynamicCircleMode.ConeAndPlaneHoldPlaneNormal => WorkerDynamicCircleModeValue.ConePlaneHoldPlaneNormal,
            Api.DynamicCircleMode.ConeAndPlaneHoldConeAxis => WorkerDynamicCircleModeValue.ConePlaneHoldConeAxis,
            Api.DynamicCircleMode.SphereAndPlaneIntersection => WorkerDynamicCircleModeValue.SpherePlaneIntersection,
            Api.DynamicCircleMode.TwoConesIntersection => WorkerDynamicCircleModeValue.TwoConesIntersection,
            Api.DynamicCircleMode.ConeAndCylinderIntersection => WorkerDynamicCircleModeValue.ConeCylinderIntersection,
            _ => throw Invalid(field)
        });

    public static WorkerChoiceValue<WorkerDynamicEllipseModeValue> Required(Api.DynamicEllipseMode? value, string field) =>
        new(value switch
        {
            Api.DynamicEllipseMode.CylinderAndPlaneIntersection => WorkerDynamicEllipseModeValue.CylinderPlaneIntersection,
            Api.DynamicEllipseMode.ConeAndPlaneIntersection => WorkerDynamicEllipseModeValue.ConePlaneIntersection,
            _ => throw Invalid(field)
        });

    public static WorkerChoiceValue<WorkerDynamicLineModeValue> Required(Api.DynamicLineMode? value, string field) =>
        new(value switch
        {
            Api.DynamicLineMode.ConeAxis => WorkerDynamicLineModeValue.ConeAxis,
            Api.DynamicLineMode.CylinderAxis => WorkerDynamicLineModeValue.CylinderAxis,
            Api.DynamicLineMode.IntersectionOfTwoPlanes => WorkerDynamicLineModeValue.IntersectionOfTwoPlanes,
            Api.DynamicLineMode.BisectTwoLines => WorkerDynamicLineModeValue.BisectTwoLines,
            Api.DynamicLineMode.SlotCenterlineAlongLength => WorkerDynamicLineModeValue.SlotCenterlineAlongLength,
            _ => throw Invalid(field)
        });

    public static WorkerChoiceValue<WorkerDynamicPlaneModeValue> Required(Api.DynamicPlaneMode? value, string field) =>
        new(value switch
        {
            Api.DynamicPlaneMode.BisectTwoPlanes => WorkerDynamicPlaneModeValue.BisectTwoPlanes,
            Api.DynamicPlaneMode.TwoConesHoldNormalToBestFitPlane => WorkerDynamicPlaneModeValue.TwoConesBestFitPlane,
            Api.DynamicPlaneMode.TwoConesHoldNormalToFirstConeAxis => WorkerDynamicPlaneModeValue.TwoConesFirstConeAxis,
            Api.DynamicPlaneMode.TwoConesHoldNormalToSecondConeAxis => WorkerDynamicPlaneModeValue.TwoConesSecondConeAxis,
            Api.DynamicPlaneMode.ConeAndCylinderHoldNormalToBestFitPlane => WorkerDynamicPlaneModeValue.ConeCylinderBestFitPlane,
            Api.DynamicPlaneMode.ConeAndCylinderHoldNormalToConeAxis => WorkerDynamicPlaneModeValue.ConeCylinderConeAxis,
            Api.DynamicPlaneMode.ConeAndCylinderHoldNormalToCylinderAxis => WorkerDynamicPlaneModeValue.ConeCylinderCylinderAxis,
            Api.DynamicPlaneMode.OffsetPlaneFromPlane => WorkerDynamicPlaneModeValue.OffsetPlaneFromPlane,
            _ => throw Invalid(field)
        });

    public static WorkerChoiceValue<WorkerDynamicPointModeValue> Required(Api.DynamicPointMode? value, string field) =>
        new(value switch
        {
            Api.DynamicPointMode.IntersectionLineAndPlane => WorkerDynamicPointModeValue.IntersectionLinePlane,
            Api.DynamicPointMode.IntersectionCylinderAndPlane => WorkerDynamicPointModeValue.IntersectionCylinderPlane,
            Api.DynamicPointMode.IntersectionConeAndPlane => WorkerDynamicPointModeValue.IntersectionConePlane,
            Api.DynamicPointMode.IntersectionThreePlanes => WorkerDynamicPointModeValue.IntersectionThreePlanes,
            Api.DynamicPointMode.MidPointPerpendicularToTwoLines => WorkerDynamicPointModeValue.MidPointPerpendicularTwoLines,
            _ => throw Invalid(field)
        });

    private static ArgumentException Invalid(string field) =>
        new($"Request field '{field}' requires a supported construction mode.", field);
}
