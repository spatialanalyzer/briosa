namespace Briosa.Worker.Control;

public sealed record WorkerUnavailableOutput : WorkerMpOutputValue
{
    public WorkerUnavailableOutput(string name, WorkerMpValueKind kind, string? diagnosticCode = null)
        : base(name, kind, diagnosticCode) { }

}
