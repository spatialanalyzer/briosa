using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.LicensedProbes;

/// <summary>
/// The size of one response as the harness received it (licensed session 4, #402). It holds
/// byte counts and one flag, never a value. In the worker phase the harness is the worker's
/// host, so it counts the bytes of the private frame it reads; in the public phase it measures
/// the protobuf message the generated client received.
/// </summary>
/// <param name="ReceivedFrameBytes">
/// Payload bytes of the private worker frame the harness received, without the 4-byte length
/// header. When <paramref name="EncodingRejected"/> is true this is the size of the worker's
/// bounded replacement, never of the result it replaced. Null in the public phase.
/// </param>
/// <param name="PublicMessageBytes">
/// Serialized size of the protobuf response message the generated client received. Null in
/// the worker phase and when the RPC returned an error instead of a message.
/// </param>
/// <param name="EncodingRejected">
/// The worker could not encode at least one output, so it sent its bounded replacement: the
/// completed outputs-unavailable outcome, or an SDK call fault with that output withheld
/// (<c>worker-output-encoding-rejected</c>). The product gives that code both to a result over
/// the response budget and to a value it cannot encode at all (for example a non-finite
/// number), so the original size is unknown and is never reported as an overflow.
/// </param>
internal sealed record ResponseSize(int? ReceivedFrameBytes, int? PublicMessageBytes, bool EncodingRejected)
{
    // The 4-byte little-endian length prefix of every private frame.
    public const int FrameHeaderBytes = sizeof(int);

    // grpc-dotnet's default MaxReceiveMessageSize, which the published clients keep.
    public const int PublishedClientReceiveLimitBytes = 4 * 1024 * 1024;

    /// <summary>The value-free code the worker reports for an output it could not encode.</summary>
    public const string EncodingRejectedDiagnosticCode = WorkerExecutionDelivery.OutputEncodingRejected;

    /// <summary>The protobuf message is larger than the published clients can receive.</summary>
    public bool? ExceedsPublishedClientLimit => PublicMessageBytes is { } bytes ? bytes > PublishedClientReceiveLimitBytes : null;

    /// <summary>
    /// The size of one received execution result. Encoding was rejected when the result is the
    /// outputs-unavailable replacement, or when any output arrived withheld for its encoding.
    /// </summary>
    public static ResponseSize FromWorkerResponse(int frameBytes, WorkerExecutionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var execution = response.Execution;
        var rejected =
            string.Equals(execution?.DiagnosticCode ?? response.DiagnosticCode, EncodingRejectedDiagnosticCode, StringComparison.Ordinal) ||
            (execution?.OutputValues.Any(static output =>
                output is WorkerUnavailableOutput { Reason: WorkerUnavailableOutputReason.EncodingRejected } ||
                string.Equals(output.DiagnosticCode, EncodingRejectedDiagnosticCode, StringComparison.Ordinal)) ?? false);
        return new(frameBytes, null, rejected);
    }

    public static ResponseSize FromPublicMessage(int messageBytes) => new(null, messageBytes, false);

    /// <summary>
    /// A public error whose result, or any one of its outputs, carries the worker's encoding
    /// rejection; null when nothing was rejected for its encoding.
    /// </summary>
    public static ResponseSize? FromPublicError(Api.OperationError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        var rejected = string.Equals(error.DiagnosticCode, EncodingRejectedDiagnosticCode, StringComparison.Ordinal) ||
            (error.MpExecution?.OutputRetrievals.Any(static output =>
                string.Equals(output.DiagnosticCode, EncodingRejectedDiagnosticCode, StringComparison.Ordinal)) ?? false);
        return rejected ? new ResponseSize(null, null, EncodingRejected: true) : null;
    }
}

/// <summary>
/// The private worker response budget of the build the harness runs with. The harness and the
/// worker always come from the same commit, so this is the budget the worker enforces: 1 MiB
/// since #411, separate from the 64 KiB request budget and the public inbound limit.
/// </summary>
internal static class WorkerResponseBudget
{
    public static int Bytes => WorkerControlProtocol.MaximumResponseBytes;
}

/// <summary>
/// Counts the bytes read through it and changes nothing else. The worker phase reads the
/// private channel through it, so the size of each received frame is known exactly. It sits
/// only in the harness, never in the server or the worker.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed",
    Justification = "The inner stream belongs to its owner, which disposes it; the counter never closes it.")]
internal sealed class CountingStream(Stream inner, Action? beforeFrameWrite = null) : Stream
{
    private readonly Stream _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private long _bytesRead;

    // The control channel serializes a whole frame, then writes it (header, payload) and
    // flushes. The gate runs before the first write of each frame, after serialization, so a
    // refusal never leaves a partial frame on the pipe.
    private bool _frameOpen;

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

    public override void Write(byte[] buffer, int offset, int count)
    {
        OpenFrame();
        _inner.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        OpenFrame();
        _inner.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        OpenFrame();
        return _inner.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        OpenFrame();
        return _inner.WriteAsync(buffer, cancellationToken);
    }

    public override void Flush()
    {
        _frameOpen = false;
        _inner.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        _frameOpen = false;
        return _inner.FlushAsync(cancellationToken);
    }

    // The gate may throw to refuse the frame; nothing of it has been written then.
    private void OpenFrame()
    {
        if (!_frameOpen)
        {
            beforeFrameWrite?.Invoke();
            _frameOpen = true;
        }
    }

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
