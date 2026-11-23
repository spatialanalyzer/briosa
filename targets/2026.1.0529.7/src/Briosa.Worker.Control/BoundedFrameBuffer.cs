using System.Buffers;

namespace Briosa.Worker.Control;

/// <summary>
/// Collects one encoded frame in pooled chunks and stops the encoder as soon as
/// the frame is over its budget. Encoding an oversized message therefore holds
/// at most the budget plus one chunk, whatever the message contains, and the
/// frame length it reports is exact for every frame that fits.
/// </summary>
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

    private readonly int _maximumBytes;
    private readonly List<(byte[] Buffer, int Count)> _filled = [];
    private byte[]? _current;
    private int _currentCount;
    private bool _disposed;

    public BoundedFrameBuffer(int maximumBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        _maximumBytes = maximumBytes;
    }

    /// <summary>The bytes written so far.</summary>
    public int Length { get; private set; }

    /// <summary>Whether the frame is over its budget.</summary>
    public bool Exceeded => Length > _maximumBytes;

    public void Advance(int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (_current is null || count > _current.Length - _currentCount)
        {
            throw new InvalidOperationException("The frame buffer was advanced past the memory it provided.");
        }

        _currentCount += count;
        Length += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        Reserve(sizeHint);
        return _current.AsMemory(_currentCount);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        Reserve(sizeHint);
        return _current.AsSpan(_currentCount);
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
    }

    private void Reserve(int sizeHint)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // Once the frame is over its budget the encoder gets no more memory, so
        // it stops; the frame is rejected either way.
        if (Exceeded)
        {
            throw new InvalidDataException("The worker control message exceeds the size limit.");
        }

        if (sizeHint > MaximumSizeHint)
        {
            throw new InvalidDataException("The worker control message cannot be encoded within its size limit.");
        }

        sizeHint = Math.Max(sizeHint, 1);
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
}
