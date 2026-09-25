namespace Briosa.Worker.Control;

public sealed record WorkerPointDeltaReportOptionsValue(
    int CoordinateSystem,
    string DetailsFormat,
    bool ShowPointA,
    bool ShowPointB,
    bool ShowDelta,
    bool ShowMagnitude,
    bool ShowComponent1,
    bool ShowComponent2,
    bool ShowComponent3,
    bool SortPointNames,
    bool ShowToleranceFields,
    bool ColorizeInToleranceFields);
