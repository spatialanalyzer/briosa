namespace Briosa.Worker.Sdk;

internal sealed record SdkColorizationOptionsValue(
    SdkColorRangeMethodValue ColorRangeMethod,
    SdkBaseColorTypeValue BaseHighColor,
    SdkBaseMidColorTypeValue BaseMidColor,
    SdkBaseColorTypeValue BaseLowColor,
    bool DrawTubes,
    bool DrawArrowheads,
    bool IndicateValues,
    double VectorMagnification,
    int VectorWidth,
    bool DrawBlotches,
    double BlotchSize,
    bool ShowOutOfToleranceOnly,
    bool ShowColorBarInView,
    bool ShowColorBarPercentages,
    bool ShowColorBarFractions,
    double HighSaturationLimit,
    double LowSaturationLimit,
    double HighTolerance,
    double LowTolerance);
