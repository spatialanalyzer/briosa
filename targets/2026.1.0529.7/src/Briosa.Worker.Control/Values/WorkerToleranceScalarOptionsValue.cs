namespace Briosa.Worker.Control;

public sealed record WorkerToleranceScalarOptionsValue(
    WorkerToleranceLimit High,
    WorkerToleranceLimit Low);
