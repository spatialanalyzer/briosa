namespace Briosa.Worker.Control;

public sealed record WorkerMpExecutionResult(
    bool ExecuteStepReturned,
    bool MpResultRetrieved,
    bool MpSucceeded,
    int? MpResultCode,
    long DurationMilliseconds,
    IReadOnlyList<WorkerMpOutputValue> OutputValues,
    string? DiagnosticCode);
