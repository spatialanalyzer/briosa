using System.Buffers.Binary;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text.Json;

namespace Briosa.Desktop;

public enum DesktopAction
{
    Status, StopServer, StartSdk, Connect, Reconnect, StopSdk, RecoverSdk
}

public sealed record DesktopRequest(
    string Instance, DesktopAction Action = DesktopAction.Status,
    string? Credential = null, int ExpectedSdkGeneration = 0);

// This private envelope is never written to diagnostic logs or support exports.
// It contains local routing information; public state remains standard protobuf.
public sealed record DesktopReply
{
    public int SchemaVersion { get; init; } = 1;
    public required string Instance { get; init; }
    public required string Target { get; init; }
    public required string Endpoint { get; init; }
    public string HostState { get; init; } = "Starting";
    public bool CanManage { get; init; }
    public bool Accepted { get; init; } = true;
    public string Diagnostic { get; init; } = "";
    public string LogInstance { get; init; } = "";
    public string? LogDirectory { get; init; }
    public string ServerInfoJson { get; init; } = "{}";
    public string SdkStateJson { get; init; } = "{}";
    public string ApplicationStateJson { get; init; } = "{}";
}

public static class DesktopProtocol
{
    public const string Target = "2024.1.0508.5";
    public const string InstanceVariable = "BRIOSA_DESKTOP_INSTANCE";
    public const string CredentialVariable = "BRIOSA_DESKTOP_CREDENTIAL";
    public const string ModeVariable = "Briosa__Desktop__Mode";
    public const int MaximumMessageBytes = 64 * 1024;

    public static string NewInstance() => Guid.NewGuid().ToString("N");
    public static string NewCredential() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public static bool IsInstance(string? value) => value is { Length: 32 } && Guid.TryParseExact(value, "N", out _);
    public static string PipeName(string instance) => IsInstance(instance)
        ? "briosa-desktop-" + instance : throw new ArgumentException("Invalid desktop instance.", nameof(instance));

    public static bool CredentialMatches(string? expected, string? actual)
    {
        if (expected is not { Length: 64 } || actual is not { Length: 64 }) return false;
        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.ASCII.GetBytes(expected), System.Text.Encoding.ASCII.GetBytes(actual));
    }

    public static async Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        if (bytes.Length > MaximumMessageBytes) throw new InvalidDataException("Desktop message is too large.");
        var header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, bytes.Length);
        await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<T> ReadAsync<T>(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var header = new byte[4];
        await stream.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(false);
        var length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length is < 1 or > MaximumMessageBytes) throw new InvalidDataException("Invalid desktop message size.");
        var bytes = new byte[length];
        await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(bytes) ?? throw new InvalidDataException("Invalid desktop message.");
    }

    public static async Task<DesktopReply> SendAsync(DesktopRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var pipe = new NamedPipeClientStream(".", PipeName(request.Instance),
            PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await pipe.ConnectAsync(cancellationToken).ConfigureAwait(false);
        await WriteAsync(pipe, request, cancellationToken).ConfigureAwait(false);
        var reply = await ReadAsync<DesktopReply>(pipe, cancellationToken).ConfigureAwait(false);
        if (reply.SchemaVersion != 1 || reply.Instance != request.Instance || reply.Target != Target)
            throw new InvalidDataException("Desktop instance or target mismatch.");
        return reply;
    }
}
