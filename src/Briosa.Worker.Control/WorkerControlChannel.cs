using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Briosa.Worker.Control;

public sealed class WorkerControlChannel(Stream stream, bool leaveOpen = false) : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter<WorkerControlMessageKind>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<WorkerConnectionState>(JsonNamingPolicy.CamelCase)
        }
    };

    private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    private readonly bool _leaveOpen = leaveOpen;
    private int _disposeState;

    public void Send(WorkerControlMessage message)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        var payload = Serialize(message);
        Span<byte> header = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        _stream.Write(header);
        _stream.Write(payload);
        _stream.Flush();
    }

    public WorkerControlMessage Receive()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        Span<byte> header = stackalloc byte[sizeof(int)];
        _stream.ReadExactly(header);
        var payload = new byte[ReadLength(header)];
        _stream.ReadExactly(payload);
        return Deserialize(payload);
    }

    public async ValueTask SendAsync(
        WorkerControlMessage message,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        var payload = Serialize(message);
        var header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        await _stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await _stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<WorkerControlMessage> ReceiveAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        var header = new byte[sizeof(int)];
        await _stream.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(false);
        var payload = new byte[ReadLength(header)];
        await _stream.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);
        return Deserialize(payload);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) == 0 && !_leaveOpen)
        {
            _stream.Dispose();
        }
    }

    private static byte[] Serialize(WorkerControlMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Validate(message);
        var payload = JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
        if (payload.Length > WorkerControlProtocol.MaximumMessageBytes)
        {
            throw new InvalidDataException("The worker control message exceeds the size limit.");
        }

        return payload;
    }

    private static WorkerControlMessage Deserialize(ReadOnlySpan<byte> payload)
    {
        var message = JsonSerializer.Deserialize<WorkerControlMessage>(payload, SerializerOptions)
            ?? throw new InvalidDataException("The worker control message was empty.");
        Validate(message);
        return message;
    }

    private static int ReadLength(ReadOnlySpan<byte> header)
    {
        var length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length is <= 0 or > WorkerControlProtocol.MaximumMessageBytes)
        {
            throw new InvalidDataException("The worker control message length is invalid.");
        }

        return length;
    }

    private static void Validate(WorkerControlMessage message)
    {
        if (message.ProtocolVersion != WorkerControlProtocol.CurrentVersion)
        {
            throw new InvalidDataException(
                $"Unsupported worker control protocol version ''{message.ProtocolVersion}''.");
        }

        if (message.Kind == WorkerControlMessageKind.None)
        {
            throw new InvalidDataException("The worker control message kind is invalid.");
        }

        if (message.Kind == WorkerControlMessageKind.Ready &&
            (message.ProcessId is not > 0 || message.Connection is null))
        {
            throw new InvalidDataException(
                "A worker ready message requires a process identifier and connection snapshot.");
        }
    }
}
