using System.Reflection;
using Briosa.Worker.Control;

namespace Briosa.LicensedProbes;

/// <summary>
/// The size of one response as the harness received it (licensed session 4, #402). It
/// holds byte counts and one flag, never a value. In the worker phase the harness is the
/// worker's host, so it counts the bytes of the private frame it reads; in the public phase
/// it measures the protobuf message the generated client received.
/// </summary>
/// <param name="WorkerFrameBytes">
/// Payload bytes of the private worker frame the harness received, without the 4-byte length
/// header. This is the quantity the worker response budget bounds. When
/// <paramref name="BudgetExceeded"/> is true it is the size of the worker's bounded
/// replacement, not of the result that did not fit. Null in the public phase.
/// </param>
/// <param name="PublicMessageBytes">
/// Serialized size of the protobuf response message the generated client received. Null in
/// the worker phase and when the RPC returned an error instead of a message.
/// </param>
/// <param name="BudgetExceeded">
/// The worker could not encode the result within its response budget, so the outputs were
/// replaced by the existing completed outputs-unavailable outcome (<c>worker-output-encoding-rejected</c>).
/// The result's own size is then unknown; it is larger than the budget.
/// </param>
internal sealed record ResponseSize(int? WorkerFrameBytes, int? PublicMessageBytes, bool BudgetExceeded)
{
    // The 4-byte little-endian length prefix of every private frame.
    public const int FrameHeaderBytes = sizeof(int);

    // grpc-dotnet's default MaxReceiveMessageSize, which the published clients keep.
    public const int PublishedClientReceiveLimitBytes = 4 * 1024 * 1024;

    /// <summary>The value-free code the worker reports when a result did not fit its response budget.</summary>
    public const string BudgetExceededDiagnosticCode = WorkerExecutionDelivery.OutputEncodingRejected;

    /// <summary>The protobuf message is larger than the published clients can receive.</summary>
    public bool? ExceedsPublishedClientLimit => PublicMessageBytes is { } bytes ? bytes > PublishedClientReceiveLimitBytes : null;

    public static ResponseSize FromWorkerFrame(int frameBytes, string? diagnosticCode) =>
        new(frameBytes, null, string.Equals(diagnosticCode, BudgetExceededDiagnosticCode, StringComparison.Ordinal));

    /// <summary>
    /// The size of one received execution result. The budget was exceeded when the result is
    /// the outputs-unavailable replacement, or when an SDK call fault arrived with an output
    /// withheld because it did not fit.
    /// </summary>
    public static ResponseSize FromWorkerResponse(int frameBytes, WorkerExecutionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var execution = response.Execution;
        var exceeded =
            string.Equals(execution?.DiagnosticCode ?? response.DiagnosticCode, BudgetExceededDiagnosticCode, StringComparison.Ordinal) ||
            (execution?.OutputValues.Any(static output =>
                output is WorkerUnavailableOutput { Reason: WorkerUnavailableOutputReason.EncodingRejected }) ?? false);
        return new(frameBytes, null, exceeded);
    }

    public static ResponseSize FromPublicMessage(int messageBytes) => new(null, messageBytes, false);

    public static ResponseSize PublicBudgetExceeded { get; } = new(null, null, true);
}

/// <summary>
/// The private worker response budget of the build the harness runs with. The harness and
/// the worker always come from the same commit, so this is the budget the worker enforces.
/// It is read by name so that the harness works unchanged at either budget: the single 64 KiB
/// frame limit (<c>MaximumMessageBytes</c>), or the separate 1 MiB response budget
/// (<c>MaximumResponseBytes</c>) proposed by #411, whichever is merged first.
/// </summary>
internal static class WorkerResponseBudget
{
    private static readonly Lazy<int> Value = new(Resolve);

    public static int Bytes => Value.Value;

    internal static int Resolve()
    {
        foreach (var name in new[] { "MaximumResponseBytes", "MaximumMessageBytes" })
        {
            if (typeof(WorkerControlProtocol).GetField(name, BindingFlags.Public | BindingFlags.Static) is { IsLiteral: true } field &&
                field.GetRawConstantValue() is int bytes && bytes > 0)
            {
                return bytes;
            }
        }

        throw new InvalidOperationException("The worker control protocol declares no response budget.");
    }
}

/// <summary>
/// Counts the bytes read through it and changes nothing else. The worker phase reads the
/// private channel through it, so the size of each received frame is known exactly. It sits
/// only in the harness, never in the server or the worker.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed",
    Justification = "The inner stream belongs to its owner, which disposes it; the counter never closes it.")]
internal sealed class CountingStream(Stream inner) : Stream
{
    private readonly Stream _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private long _bytesRead;

    public long BytesRead => Interlocked.Read(ref _bytesRead);

    public override bool CanRead => _inner.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => _inner.CanWrite;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => Count(_inner.Read(buffer, offset, count));

    public override int Read(Span<byte> buffer) => Count(_inner.Read(buffer));

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        Count(await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false));

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        Count(await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false));

    public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

    public override void Write(ReadOnlySpan<byte> buffer) => _inner.Write(buffer);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        _inner.WriteAsync(buffer, offset, count, cancellationToken);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
        _inner.WriteAsync(buffer, cancellationToken);

    public override void Flush() => _inner.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    // Dispose is not overridden: the inner stream belongs to its owner, and disposing the
    // counter never closes it.
    private int Count(int read)
    {
        if (read > 0)
        {
            Interlocked.Add(ref _bytesRead, read);
        }

        return read;
    }
}
