using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

/// <summary>Defers request mapping until policy and admission have accepted the operation.</summary>
internal sealed record WorkerCommandSubmission(
    string OperationId,
    Func<WorkerMpCommand> CreateCommand,
    int RetainedBytes = 1024 * 1024);
