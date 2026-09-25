using System.Text.Json.Serialization;

namespace Briosa.Worker.Control;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "retrieval")]
[JsonDerivedType(typeof(WorkerRetrievedOutput), "retrieved")]
[JsonDerivedType(typeof(WorkerUnavailableOutput), "unavailable")]
public abstract record WorkerMpOutputValue
{
    private protected WorkerMpOutputValue(string name, WorkerMpValueKind kind, string? diagnosticCode)
    {
        Name = name;
        Kind = kind;
        DiagnosticCode = diagnosticCode;
    }

    public string Name { get; }
    public WorkerMpValueKind Kind { get; }

    // SDK getter diagnostics stay in the worker; public failures report the
    // reviewed operation/output identities, never raw SDK values or errors.
    [JsonIgnore]
    public string? DiagnosticCode { get; }

    [JsonIgnore]
    public bool Retrieved => this is WorkerRetrievedOutput;

    public WorkerMpValue? ReadValue() =>
        this is WorkerRetrievedOutput output ? output.Value : null;

    public TValue RequireValue<TValue>() where TValue : WorkerMpValue =>
        ReadValue() is TValue value
            ? value
            : throw new InvalidOperationException("The requested output value is unavailable or has a different type.");

    // SDK getters return a Boolean separately from their parsed value. Collapse
    // that evidence once, and reject contradictory evidence at this boundary.
    public static WorkerMpOutputValue FromRetrieval(
        string name, WorkerMpValueKind kind, bool retrieved, WorkerMpValue? value,
        string? diagnosticCode = null)
    {
        if (retrieved != (value is not null))
        {
            throw new ArgumentException("Output retrieval evidence is contradictory.", nameof(value));
        }

        return retrieved
            ? new WorkerRetrievedOutput(name, kind, value!, diagnosticCode)
            : new WorkerUnavailableOutput(name, kind, diagnosticCode);
    }
}
