using System.Buffers.Binary;
using System.Net.Http;

namespace Briosa.Cli.Engine;

/// <summary>
/// Makes a second submission of a request impossible below the gRPC client. The
/// gRPC client has no retry policy, but <see cref="SocketsHttpHandler"/> resends a
/// request on its own when the server refuses the HTTP/2 stream or closes the
/// connection before processing it. Every logical request the channel starts
/// permits exactly one new HTTP/2 stream; the connection's outgoing bytes are read
/// frame by frame, and a HEADERS frame that would open a stream beyond the permits
/// is never written. The write fails instead, the connection is torn down, and the
/// call fails without the server receiving a second request.
/// </summary>
internal sealed class SubmissionGuard
{
    private int _permitted;
    private int _opened;
    private int _blocked;

    /// <summary>A stream was refused because its request had already opened one.</summary>
    public bool ResendBlocked => Volatile.Read(ref _blocked) > 0;

    public void Permit() => Interlocked.Increment(ref _permitted);

    public bool TryOpenStream()
    {
        if (Volatile.Read(ref _opened) >= Volatile.Read(ref _permitted))
        {
            Interlocked.Increment(ref _blocked);
            return false;
        }

        Interlocked.Increment(ref _opened);
        return true;
    }
}

/// <summary>Grants one stream to each request the gRPC channel sends.</summary>
internal sealed class SubmissionHandler(SubmissionGuard guard, HttpMessageHandler inner) : DelegatingHandler(inner)
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        guard.Permit();
        return base.SendAsync(request, cancellationToken);
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        guard.Permit();
        return base.Send(request, cancellationToken);
    }
}

/// <summary>
/// The connection stream, reading the client's outgoing HTTP/2 frames. Only the
/// frame headers are inspected: a HEADERS frame (type 1) on a stream identifier
/// higher than any before opens a new stream, and needs a permit.
/// </summary>
internal sealed class SubmissionGuardStream(Stream inner, SubmissionGuard guard) : Stream
{
    private const int PrefaceLength = 24;
    private const int FrameHeaderLength = 9;
    private const byte HeadersFrame = 0x1;

    private readonly byte[] _header = new byte[FrameHeaderLength];
    private int _prefaceRemaining = PrefaceLength;
    private int _headerFilled;
    private long _payloadRemaining;
    private uint _highestStream;
    private bool _poisoned;

    public override bool CanRead => inner.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => inner.CanWrite;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => inner.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

    public override int Read(Span<byte> buffer) => inner.Read(buffer);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        inner.ReadAsync(buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        inner.ReadAsync(buffer, cancellationToken);

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        var allowed = Inspect(buffer);
        inner.Write(buffer[..allowed]);
        ThrowIfBlocked(allowed, buffer.Length);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var allowed = Inspect(buffer.Span);
        await inner.WriteAsync(buffer[..allowed], cancellationToken).ConfigureAwait(false);
        ThrowIfBlocked(allowed, buffer.Length);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            inner.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await inner.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private void ThrowIfBlocked(int allowed, int length)
    {
        if (allowed < length || _poisoned)
        {
            _poisoned = true;
            throw new IOException("A second submission of the same request was blocked.");
        }
    }

    // Returns how many leading bytes may be written: all of them, or the bytes before
    // the first frame header that would open an unpermitted stream.
    private int Inspect(ReadOnlySpan<byte> buffer)
    {
        if (_poisoned)
        {
            return 0;
        }

        var index = 0;
        while (index < buffer.Length)
        {
            if (_prefaceRemaining > 0)
            {
                var take = Math.Min(_prefaceRemaining, buffer.Length - index);
                _prefaceRemaining -= take;
                index += take;
                continue;
            }

            if (_payloadRemaining > 0)
            {
                var take = (int)Math.Min(_payloadRemaining, buffer.Length - index);
                _payloadRemaining -= take;
                index += take;
                continue;
            }

            var headerStart = index - _headerFilled;
            var copy = Math.Min(FrameHeaderLength - _headerFilled, buffer.Length - index);
            buffer.Slice(index, copy).CopyTo(_header.AsSpan(_headerFilled));
            _headerFilled += copy;
            index += copy;
            if (_headerFilled < FrameHeaderLength)
            {
                continue;
            }

            _headerFilled = 0;
            _payloadRemaining = (_header[0] << 16) | (_header[1] << 8) | _header[2];
            var stream = BinaryPrimitives.ReadUInt32BigEndian(_header.AsSpan(5)) & 0x7FFF_FFFF;
            if (_header[3] == HeadersFrame && stream > _highestStream)
            {
                if (!guard.TryOpenStream())
                {
                    _poisoned = true;
                    return Math.Max(0, headerStart);
                }

                _highestStream = stream;
            }
        }

        return buffer.Length;
    }
}
