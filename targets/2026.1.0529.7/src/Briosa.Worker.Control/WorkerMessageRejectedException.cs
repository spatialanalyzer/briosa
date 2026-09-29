namespace Briosa.Worker.Control;

/// <summary>
/// The local message failed preparation before any frame bytes were written.
/// This proves the peer did not observe this request; it is not a pipe failure.
/// </summary>
public sealed class WorkerMessageRejectedException : IOException
{
    public WorkerMessageRejectedException() : base("The worker message could not be encoded.") { }

    public WorkerMessageRejectedException(string message) : base(message) { }

    public WorkerMessageRejectedException(string message, Exception innerException)
        : base(message, innerException) { }
}
