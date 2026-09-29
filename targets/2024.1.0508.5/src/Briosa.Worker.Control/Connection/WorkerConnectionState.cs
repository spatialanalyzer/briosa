namespace Briosa.Worker.Control;

public enum WorkerConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Faulted,
    Stopping
}
