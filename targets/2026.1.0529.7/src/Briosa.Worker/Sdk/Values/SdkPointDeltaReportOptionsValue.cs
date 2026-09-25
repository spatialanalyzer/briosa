namespace Briosa.Worker.Sdk;

internal sealed record SdkPointDeltaReportOptionsValue(
    SdkCoordinateSystemTypeValue CoordinateSystem,
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
