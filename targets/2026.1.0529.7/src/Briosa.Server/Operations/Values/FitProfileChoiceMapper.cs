using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class FitProfileChoiceMapper
{
    public static WorkerMpValue CompTechnique(Api.CompTechnique value, string fieldName) =>
        Choice(WorkerMpValueKind.CompTechnique, (int)value, 3, fieldName);

    public static WorkerMpValue DegreeOfFreedom(Api.DegreeOfFreedom value, string fieldName) =>
        Choice(WorkerMpValueKind.DegreeOfFreedom, (int)value, 3, fieldName);

    public static WorkerMpValue FitMethod(Api.FitMethod value, string fieldName) =>
        Choice(WorkerMpValueKind.FitMethod, (int)value, 2, fieldName);

    public static WorkerMpValue MeasuredSideForPlanarOffset(Api.MeasuredSideForPlanarOffset value, string fieldName) =>
        Choice(WorkerMpValueKind.MeasuredSideForPlanarOffset, (int)value, 3, fieldName);

    public static WorkerMpValue MeasuredSideForRadialOffset(Api.MeasuredSideForRadialOffset value, string fieldName) =>
        Choice(WorkerMpValueKind.MeasuredSideForRadialOffset, (int)value, 3, fieldName);

    public static WorkerMpValue NormalDirection(Api.NormalDirection value, string fieldName) =>
        Choice(WorkerMpValueKind.NormalDirection, (int)value, 3, fieldName);

    public static WorkerMpValue SlotType(Api.SlotType value, string fieldName) =>
        Choice(WorkerMpValueKind.SlotType, (int)value, 2, fieldName);

    public static WorkerMpValue SphereFitComputationMode(Api.SphereFitComputationMode value, string fieldName) =>
        Choice(WorkerMpValueKind.SphereFitComputationMode, (int)value, 3, fieldName);

    private static WorkerMpValue Choice(WorkerMpValueKind kind, int value, int choiceCount, string fieldName)
    {
        if (value < 1 || value > choiceCount)
        {
            throw new ArgumentOutOfRangeException(fieldName, "The fit-profile choice is not supported by this SA target.");
        }

        return WorkerChoiceFactory.FromOrdinal(kind, value - 1);
    }
}
