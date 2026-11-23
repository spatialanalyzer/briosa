namespace Briosa.Worker.Control;

public static class WorkerControlProtocol
{
    // 26: the response budget is 1 MiB. A host and worker that disagree on the
    // version fail at Ready, before any command runs.
    public const int CurrentVersion = 26;

    /// <summary>
    /// The private request budget: the largest encoded frame the host may send
    /// the worker (<see cref="WorkerControlMessageKind.Ping"/>,
    /// <see cref="WorkerControlMessageKind.Stop"/>,
    /// <see cref="WorkerControlMessageKind.Execute"/>,
    /// <see cref="WorkerControlMessageKind.Connect"/> and
    /// <see cref="WorkerControlMessageKind.VerifyExecution"/>). It is
    /// independent of the public gRPC receive limit and of
    /// <see cref="MaximumResponseBytes"/>.
    /// </summary>
    public const int MaximumRequestBytes = 64 * 1024;

    /// <summary>
    /// The private response budget: the largest encoded frame the worker may
    /// send the host, including every execution result. A result that does not
    /// fit is replaced by the bounded fallback in
    /// <see cref="WorkerExecutionDelivery"/>. Changing it moves neither
    /// <see cref="MaximumRequestBytes"/> nor the public gRPC receive limit.
    /// </summary>
    public const int MaximumResponseBytes = 1024 * 1024;

    /// <summary>
    /// The largest frame either direction may carry. A receiver that does not
    /// name its peer's direction bounds a frame by this value before it can read
    /// the frame's kind.
    /// </summary>
    public const int MaximumFrameBytes =
        MaximumRequestBytes > MaximumResponseBytes ? MaximumRequestBytes : MaximumResponseBytes;

    /// <summary>Whether <paramref name="kind"/> travels from the host to the worker.</summary>
    public static bool IsRequest(WorkerControlMessageKind kind) =>
        kind is WorkerControlMessageKind.Ping or
            WorkerControlMessageKind.Stop or
            WorkerControlMessageKind.Execute or
            WorkerControlMessageKind.Connect or
            WorkerControlMessageKind.VerifyExecution;

    /// <summary>The budget that bounds one encoded frame of <paramref name="kind"/>.</summary>
    public static int MaximumBytesFor(WorkerControlMessageKind kind) =>
        IsRequest(kind) ? MaximumRequestBytes : MaximumResponseBytes;
}
