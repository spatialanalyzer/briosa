using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Briosa.Cli.Engine;

/// <summary>
/// Delivers an MP result exactly once, without ever running the MP again. A result
/// whose JSON fits the inline cap goes into the envelope. A larger result, or any
/// result when the caller names an output file, goes to a file in full, and the
/// envelope gets a bounded summary: field names, counts, sizes, and where the data
/// is, never the data. The JSON is the formatter's text byte for byte: floating-point
/// values are neither rounded nor reformatted.
/// </summary>
internal sealed class ResultDelivery
{
    public const int DefaultMaximumInlineBytes = 64 * 1024;
    public const int MaximumInlineBytesLimit = 4 * 1024 * 1024;

    private readonly IResultFileSystem _files;
    private readonly string _spillDirectory;
    private readonly TimeProvider _time;

    public ResultDelivery(IResultFileSystem files, string spillDirectory, TimeProvider time)
    {
        _files = files;
        _spillDirectory = spillDirectory;
        _time = time;
    }

    /// <summary>
    /// Creates the requested output file before the call, so that a path that cannot
    /// be written is refused while nothing has run. The file must not exist yet.
    /// </summary>
    public ReservedOutput? Reserve(string path, out string? rejectionCode)
    {
        rejectionCode = null;
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            rejectionCode = "output-path-invalid";
            return null;
        }

        try
        {
            return new ReservedOutput(fullPath, _files.CreateNew(fullPath));
        }
        catch (IOException) when (File.Exists(fullPath) || Directory.Exists(fullPath))
        {
            rejectionCode = "output-exists";
        }
        catch (DirectoryNotFoundException)
        {
            rejectionCode = "output-directory-missing";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            NotSupportedException or ArgumentException)
        {
            rejectionCode = "output-not-writable";
        }

        return null;
    }

    /// <summary>Removes a reserved output file that never received a result.</summary>
    public void Release(ReservedOutput? output)
    {
        if (output is null)
        {
            return;
        }

        output.Dispose();
        try
        {
            _files.DeleteIfEmpty(output.Path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // An empty file left behind holds no data.
        }
    }

    /// <summary>
    /// Writes the result into <paramref name="data"/> or a file. Returns false when the
    /// operation completed but its result could not be written where it had to go.
    /// </summary>
    public bool Deliver(IMessage result, string operationId, ReservedOutput? output, int maximumInlineBytes,
        BridgeEnvelope envelope, JsonObject data)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(data);
        var json = ProtocolJson.Format(result);
        var bytes = Encoding.UTF8.GetBytes(json);
        data["result_message"] = result.Descriptor.FullName;
        data["result_json_bytes"] = bytes.Length;
        data["inline_cap_bytes"] = maximumInlineBytes;
        if (output is null && bytes.Length <= maximumInlineBytes)
        {
            data["result_delivery"] = "inline";
            data["result"] = JsonNode.Parse(json);
            data["result_file"] = null;
            data["result_summary"] = null;
            return true;
        }

        data["result"] = null;
        data["result_summary"] = Summarize(result);
        if (output is not null)
        {
            if (TryWrite(output, bytes))
            {
                data["result_delivery"] = "file";
                data["result_file"] = FileReference(output.Path, bytes, "output_requested");
                return true;
            }

            envelope.Warn("output-write-failed",
                "The operation completed, but its result could not be written to the --output file, which may " +
                "now be incomplete. The CLI tried the spill directory instead; see data.result_file.");
            data["result_delivery_error"] = "output-write-failed";
            data["result_file"] = Spill(bytes, operationId, "output_write_failed") is { } fallback ? fallback : null;
            data["result_delivery"] = data["result_file"] is null ? "lost" : "file";
            return false;
        }

        if (Spill(bytes, operationId, "inline_cap_exceeded") is { } spilled)
        {
            data["result_delivery"] = "file";
            data["result_file"] = spilled;
            envelope.Warn("result-spilled",
                "The result is larger than the inline cap, so it was written in full to data.result_file and only " +
                "a summary is shown. Do not run the call again to fetch it.");
            return true;
        }

        envelope.Warn("output-write-failed",
            "The operation completed, but its result was larger than the inline cap and could not be written to " +
            "the spill directory.");
        data["result_delivery_error"] = "spill-write-failed";
        data["result_delivery"] = "lost";
        data["result_file"] = null;
        return false;
    }

    private JsonObject? Spill(byte[] bytes, string operationId, string reason)
    {
        try
        {
            _files.EnsurePrivateDirectory(_spillDirectory);
            var name = string.Create(CultureInfo.InvariantCulture,
                $"{_time.GetUtcNow():yyyyMMdd'T'HHmmssfff'Z'}-{operationId.Replace('.', '-')}-{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(4))}.json");
            var path = Path.Combine(_spillDirectory, name);
            var output = new ReservedOutput(path, _files.CreateNew(path));
            return TryWrite(output, bytes) ? FileReference(path, bytes, reason) : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            NotSupportedException or ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }

    private static bool TryWrite(ReservedOutput output, byte[] bytes)
    {
        try
        {
            output.Stream.Write(bytes);
            if (output.Stream is FileStream file)
            {
                file.Flush(flushToDisk: true);
            }
            else
            {
                output.Stream.Flush();
            }

            output.Dispose();
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            output.Dispose();
            return false;
        }
    }

    private static JsonObject FileReference(string path, byte[] bytes, string reason) => new()
    {
        ["path"] = path,
        ["bytes"] = bytes.Length,
        ["sha256"] = Convert.ToHexStringLower(SHA256.HashData(bytes)),
        ["reason"] = reason
    };

    /// <summary>The set top-level fields of the result, with counts and no values.</summary>
    internal static JsonObject Summarize(IMessage result)
    {
        var fields = new JsonArray();
        foreach (var field in result.Descriptor.Fields.InFieldNumberOrder())
        {
            if (field.FieldNumber == ProtocolCatalog.ExecutionFieldNumber)
            {
                continue;
            }

            var entry = new JsonObject
            {
                ["name"] = field.Name,
                ["type"] = field.FieldType switch
                {
                    FieldType.Message => field.MessageType.FullName,
                    FieldType.Enum => field.EnumType.FullName,
                    _ => ProtocolJson.ScalarTypeName(field.FieldType)
                },
                ["repeated"] = field.IsRepeated
            };
            if (field.IsRepeated)
            {
                entry["count"] = ((System.Collections.IList)field.Accessor.GetValue(result)).Count;
            }
            else if (field.HasPresence)
            {
                entry["present"] = field.Accessor.HasValue(result);
            }

            fields.Add(entry);
        }

        return new JsonObject { ["fields"] = fields };
    }
}

/// <summary>An output file created before the call and written once after it.</summary>
internal sealed class ReservedOutput(string path, Stream stream) : IDisposable
{
    public string Path { get; } = path;

    public Stream Stream { get; } = stream;

    public void Dispose() => Stream.Dispose();
}
