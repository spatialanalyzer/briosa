namespace Briosa.Worker.Control;

public enum WorkerConnectionFailure
{
    None,
    ActivationFailed,
    NotStarted,
    ConnectFailed,
    ProcessExited,
    LivenessUnavailable
}
