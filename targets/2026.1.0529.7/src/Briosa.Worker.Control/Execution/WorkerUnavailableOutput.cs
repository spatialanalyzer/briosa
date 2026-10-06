using System.Text.Json.Serialization;

namespace Briosa.Worker.Control;

public sealed record WorkerUnavailableOutput : WorkerMpOutputValue
{
    public WorkerUnavailableOutput(
        string name,
        WorkerMpValueKind kind,
        string? diagnosticCode = null,
        WorkerUnavailableOutputReason reason = WorkerUnavailableOutputReason.None)
        : base(name, kind, diagnosticCode)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), "The unavailable-output reason is unknown.");
        }

        Reason = reason;
    }

    // Unlike the worker-local diagnostic code, the reason is part of the private
    // protocol. The default is omitted so ordinary unavailable outputs keep their
    // existing encoding.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public WorkerUnavailableOutputReason Reason { get; }
}
