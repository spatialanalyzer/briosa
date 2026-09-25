namespace Briosa.Worker.Control;

public sealed record WorkerProjectionOptionsValue(
    string ProjectionType,
    bool IgnoreEdgeProjections,
    bool OverrideTargetOffsets,
    double OverrideTargetOffsetsValue,
    bool AddExtraMaterialThickness,
    double ExtraMaterialThicknessValue) : WorkerMpValue;
