using System.Text.Json.Serialization;

namespace Briosa.Worker.Control;

public sealed record WorkerRetrievedOutput : WorkerMpOutputValue
{
    public WorkerRetrievedOutput(
        string name, WorkerMpValueKind kind, WorkerMpValue value, string? diagnosticCode = null)
        : base(name, kind, diagnosticCode)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!value.MatchesOutputKind(kind))
        {
            throw new ArgumentException("The output value does not match its MP kind.", nameof(value));
        }
        Value = value;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public WorkerMpValue Value { get; }
}
