namespace Briosa.Worker.Control;

public sealed record WorkerCloudThinningOptionsValue(
    int Mode,
    int PointIncrement,
    int MinimumNumberOfPoints,
    int MaximumNumberOfPoints) : WorkerMpValue;
