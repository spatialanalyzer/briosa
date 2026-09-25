namespace Briosa.Worker.Control;

public sealed record WorkerFitConstraintScalarOptionsValue(
    WorkerScalarToleranceLimit High,
    WorkerScalarToleranceLimit Low);
