namespace Briosa.Worker.Control;

public sealed record WorkerToleranceVectorOptionsValue(
    WorkerToleranceLimit HighX,
    WorkerToleranceLimit HighY,
    WorkerToleranceLimit HighZ,
    WorkerToleranceLimit HighMagnitude,
    WorkerToleranceLimit LowX,
    WorkerToleranceLimit LowY,
    WorkerToleranceLimit LowZ,
    WorkerToleranceLimit LowMagnitude);
