namespace Briosa.Worker.Control;

public sealed record WorkerToleranceScalarOptionsValue(
    WorkerScalarToleranceLimit High,
    WorkerScalarToleranceLimit Low);
