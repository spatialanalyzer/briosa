namespace Briosa.Worker.Control;

public sealed record WorkerWorldTransformValue(
    WorkerTransformValue Transform,
    double ScaleFactor) : WorkerMpValue;
