namespace Briosa.Worker.Control;

public enum WorkerControlMessageKind
{
    None = 0,
    Ready,
    Ping,
    Pong,
    Stop,
    Stopped,
    Execute,
    ExecutionResult,
    Connect,
    ConnectionResult,
    VerifyExecution,
    ExecutionVerificationResult
}
