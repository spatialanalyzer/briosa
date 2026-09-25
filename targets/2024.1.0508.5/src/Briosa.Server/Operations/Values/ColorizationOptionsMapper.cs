using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class ColorizationOptionsMapper
{
    public static WorkerColorizationOptionsValue WithDefaults(Api.ColorizationOptions? value)
    {
        value ??= new Api.ColorizationOptions();
        return new(
            (int)(value.HasColorRangeMethod ? value.ColorRangeMethod : Api.ColorRangeMethod.Continuous) - 1,
            (int)(value.HasBaseHighColor ? value.BaseHighColor : Api.BaseColorType.Blue) - 1,
            (int)(value.HasBaseMidColor ? value.BaseMidColor : Api.BaseMidColorType.Green) - 1,
            (int)(value.HasBaseLowColor ? value.BaseLowColor : Api.BaseColorType.Red) - 1,
            value.HasDrawTubes && value.DrawTubes,
            !value.HasDrawArrowheads || value.DrawArrowheads,
            value.HasIndicateValues && value.IndicateValues,
            value.HasVectorMagnification ? value.VectorMagnification : 100,
            value.HasVectorWidth ? value.VectorWidth : 1,
            value.HasDrawBlotches && value.DrawBlotches,
            value.HasBlotchSize ? value.BlotchSize : 0.1,
            value.HasShowOutOfToleranceOnly && value.ShowOutOfToleranceOnly,
            value.HasShowColorBarInView && value.ShowColorBarInView,
            !value.HasShowColorBarPercentages || value.ShowColorBarPercentages,
            value.HasShowColorBarFractions && value.ShowColorBarFractions,
            value.HasHighSaturationLimit ? value.HighSaturationLimit : 0.5,
            value.HasLowSaturationLimit ? value.LowSaturationLimit : -0.5,
            value.HasHighTolerance ? value.HighTolerance : 0.03,
            value.HasLowTolerance ? value.LowTolerance : -0.03);
    }
}
