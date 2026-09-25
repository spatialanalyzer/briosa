namespace Briosa.Worker.Control;

public sealed record WorkerFitConstraintScalarOptionsValue(
    WorkerToleranceLimit High,
    WorkerToleranceLimit Low);
