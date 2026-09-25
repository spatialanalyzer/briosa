namespace Briosa.Worker.Sdk;

internal sealed record SdkProjectionOptionsValue(
    string ProjectionType,
    bool IgnoreEdgeProjections,
    bool OverrideTargetOffsets,
    double OverrideTargetOffsetsValue,
    bool AddExtraMaterialThickness,
    double ExtraMaterialThicknessValue);
