using System.Buffers;

namespace Briosa.Worker.Control;

/// <summary>
/// Collects one encoded frame in pooled chunks and stops the encoder as soon as
/// the frame is over its budget. The frame length it reports is exact for every
/// frame that fits. An oversized message is encoded at most one write past the
/// budget, so for a 1 MiB budget the buffer holds at most 25 chunks: up to 24
/// for the frame, whose tails can stay unused when a write does not fit, and one
/// discard chunk.
/// </summary>
/// <remarks>
/// While <see cref="WorkerControlStringConverter"/> writes a string, the JSON
/// writer may hold rented escaping scratch. Throwing then would leave that scratch
/// unreturned, so an over-budget request during a string write gets discard memory
/// instead, and the converter rejects the frame once the write has returned.
/// </remarks>
internal sealed class BoundedFrameBuffer : IBufferWriter<byte>, IDisposable
{
    /// <summary>
    /// The pooled chunk size. Large enough that the JSON writer's growth requests
    /// (4 KiB by default) rarely leave an unused tail.
    /// </summary>
    internal const int ChunkBytes = 64 * 1024;

    /// <summary>
    /// The largest single request the encoder may make: one chunk. Long strings
    /// are written in segments (<see cref="WorkerControlStringConverter"/>) whose
    /// worst case asks for under 19 KiB, so no write Briosa makes needs more; a
    /// larger request fails closed instead of allocating it.
    /// </summary>
    internal const int MaximumSizeHint = ChunkBytes;

    [ThreadStatic]
    private static BoundedFrameBuffer? t_current;

    private readonly int _maximumBytes;
    private readonly List<(byte[] Buffer, int Count)> _filled = [];
    private byte[]? _current;
    private int _currentCount;
    private byte[]? _discard;
    private bool _writingDiscard;
    private int _stringWrites;
    private bool _disposed;

    public BoundedFrameBuffer(int maximumBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        _maximumBytes = maximumBytes;
    }

    /// <summary>The frame this thread is encoding, if any.</summary>
    internal static BoundedFrameBuffer? Current => t_current;

    /// <summary>The bytes written so far.</summary>
    public int Length { get; private set; }

    /// <summary>Whether the frame is over its budget.</summary>
    public bool Exceeded => Length > _maximumBytes;

    /// <summary>Makes this frame <see cref="Current"/> until the scope is disposed.</summary>
    internal EncodingScope Encoding() => new(this);

    /// <summary>Marks a string write in progress; see the remarks.</summary>
    internal void BeginStringWrite() => _stringWrites++;

    internal void EndStringWrite() => _stringWrites--;

    public void Advance(int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var provided = _writingDiscard ? _discard!.Length : _current is null ? 0 : _current.Length - _currentCount;
        if (count > provided)
        {
            throw new InvalidOperationException("The frame buffer was advanced past the memory it provided.");
        }

        if (!_writingDiscard)
        {
            _currentCount += count;
        }

        Length += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        Reserve(sizeHint);
        return _writingDiscard ? _discard.AsMemory() : _current.AsMemory(_currentCount);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        Reserve(sizeHint);
        return _writingDiscard ? _discard.AsSpan() : _current.AsSpan(_currentCount);
    }

    public void WriteTo(Stream stream)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        foreach (var (buffer, count) in _filled)
        {
            stream.Write(buffer, 0, count);
        }

        if (_current is not null && _currentCount > 0)
        {
            stream.Write(_current, 0, _currentCount);
        }
    }

    public async ValueTask WriteToAsync(Stream stream, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        foreach (var (buffer, count) in _filled)
        {
            await stream.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
        }

        if (_current is not null && _currentCount > 0)
        {
            await stream.WriteAsync(_current.AsMemory(0, _currentCount), cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var (buffer, _) in _filled)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        _filled.Clear();
        if (_current is not null)
        {
            ArrayPool<byte>.Shared.Return(_current);
            _current = null;
        }

        if (_discard is not null)
        {
            ArrayPool<byte>.Shared.Return(_discard);
            _discard = null;
        }
    }

    private void Reserve(int sizeHint)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (sizeHint > MaximumSizeHint)
        {
            throw new InvalidDataException("The worker control message cannot be encoded within its size limit.");
        }

        sizeHint = Math.Max(sizeHint, 1);
        if (Exceeded)
        {
            // Outside a string write, stop the encoder now; the frame is rejected
            // either way. During one, let the writer finish into discard memory.
            if (_stringWrites == 0)
            {
                throw new InvalidDataException("The worker control message exceeds the size limit.");
            }

            _discard ??= ArrayPool<byte>.Shared.Rent(ChunkBytes);
            _writingDiscard = true;
            return;
        }

        if (_current is not null && _current.Length - _currentCount >= sizeHint)
        {
            return;
        }

        if (_current is not null)
        {
            _filled.Add((_current, _currentCount));
            _current = null;
        }

        _current = ArrayPool<byte>.Shared.Rent(Math.Max(ChunkBytes, sizeHint));
        _currentCount = 0;
    }

    internal readonly struct EncodingScope : IDisposable
    {
        private readonly BoundedFrameBuffer? _previous;

        internal EncodingScope(BoundedFrameBuffer frame)
        {
            _previous = t_current;
            t_current = frame;
        }

        public void Dispose() => t_current = _previous;
    }
}
