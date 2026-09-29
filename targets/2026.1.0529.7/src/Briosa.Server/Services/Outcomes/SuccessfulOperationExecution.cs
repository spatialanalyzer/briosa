using Briosa.Worker.Control;

namespace Briosa.Server.Services;

internal sealed record SuccessfulOperationExecution(
    WorkerMpExecutionResult Execution,
    global::Briosa.MpExecutionDetails Details);
