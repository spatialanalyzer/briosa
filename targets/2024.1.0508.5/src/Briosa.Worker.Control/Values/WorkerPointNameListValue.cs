namespace Briosa.Worker.Control;

public sealed record WorkerPointNameListValue(
    IReadOnlyList<WorkerPointNameValue> Values);
