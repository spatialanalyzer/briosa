namespace Briosa.Worker.Control;

public sealed record WorkerColorizationOptionsValue(
    int ColorRangeMethod,
    int BaseHighColor,
    int BaseMidColor,
    int BaseLowColor,
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
