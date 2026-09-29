using System.Text.Json.Serialization;

namespace Briosa.Worker.Control;

public sealed record WorkerExecutionResponse
{
    [JsonConstructor]
    public WorkerExecutionResponse(
        WorkerExecutionResponseStatus Status,
        WorkerMpExecutionResult? Execution,
        WorkerConnectionSnapshot Connection,
        string? DiagnosticCode)
    {
        if (!Enum.IsDefined(Status) || Connection is null ||
            (Status == WorkerExecutionResponseStatus.Completed) != (Execution is not null))
        {
            throw new ArgumentException("The worker execution response is contradictory.");
        }

        this.Status = Status;
        this.Execution = Execution;
        this.Connection = Connection;
        this.DiagnosticCode = DiagnosticCode;
    }

    public WorkerExecutionResponseStatus Status { get; }
    public WorkerMpExecutionResult? Execution { get; }
    public WorkerConnectionSnapshot Connection { get; }
    public string? DiagnosticCode { get; }
}
