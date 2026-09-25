using System.Text.Json.Serialization;

namespace Briosa.Worker.Control;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "outcome")]
[JsonDerivedType(typeof(WorkerArgumentsRejected), "arguments-rejected")]
[JsonDerivedType(typeof(WorkerExecuteRejected), "execute-rejected")]
[JsonDerivedType(typeof(WorkerMpResultUnavailable), "result-unavailable")]
[JsonDerivedType(typeof(WorkerMpResultAvailable), "result-available")]
[JsonDerivedType(typeof(WorkerMpOutputsUnavailable), "outputs-unavailable")]
public abstract record WorkerMpExecutionResult
{
    protected WorkerMpExecutionResult(long durationMilliseconds, string? diagnosticCode)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(durationMilliseconds);
        DurationMilliseconds = durationMilliseconds;
        DiagnosticCode = diagnosticCode;
    }

    public long DurationMilliseconds { get; }
    public string? DiagnosticCode { get; }

    // These projections preserve the independent evidence consumed by audit and
    // public outcomes. They cannot be assigned contradictory values or arrive on
    // the wire independently from the concrete outcome.
    [JsonIgnore]
    public bool ExecuteStepReturned => this is WorkerMpResultUnavailable or WorkerMpResultAvailable or WorkerMpOutputsUnavailable;

    [JsonIgnore]
    public bool MpResultRetrieved => this is WorkerMpResultAvailable or WorkerMpOutputsUnavailable;

    [JsonIgnore]
    public bool MpSucceeded => this is WorkerMpResultAvailable { ResultCode: 2 } or WorkerMpOutputsUnavailable;

    [JsonIgnore]
    public int? MpResultCode => this switch { WorkerMpResultAvailable result => result.ResultCode, WorkerMpOutputsUnavailable => 2, _ => null };

    [JsonIgnore]
    public IReadOnlyList<WorkerMpOutputValue> OutputValues =>
        this is WorkerMpResultAvailable result ? result.Outputs : [];

    // Compatibility adapter for retained fake-worker evidence fixtures. The real SDK
    // constructs explicit outcome alternatives directly.
    // Reject contradictions rather than dropping evidence to make it fit.
    public static WorkerMpExecutionResult FromEvidence(
        bool executeStepReturned,
        bool mpResultRetrieved,
        bool mpSucceeded,
        int? mpResultCode,
        long durationMilliseconds,
        IReadOnlyList<WorkerMpOutputValue> outputValues,
        string? diagnosticCode)
    {
        ArgumentNullException.ThrowIfNull(outputValues);
        if (!executeStepReturned && mpResultRetrieved ||
            mpResultRetrieved != mpResultCode.HasValue ||
            mpSucceeded != (mpResultRetrieved && mpResultCode == 2) ||
            !mpSucceeded && outputValues.Count != 0)
        {
            throw new ArgumentException("The SDK execution evidence is contradictory.");
        }

        if (!executeStepReturned)
        {
            return diagnosticCode == "sdk-argument-rejected"
                ? new WorkerArgumentsRejected(durationMilliseconds, diagnosticCode)
                : new WorkerExecuteRejected(durationMilliseconds, diagnosticCode);
        }

        return mpResultRetrieved
            ? new WorkerMpResultAvailable(mpResultCode!.Value, durationMilliseconds, outputValues, diagnosticCode)
            : new WorkerMpResultUnavailable(durationMilliseconds, diagnosticCode);
    }
}
