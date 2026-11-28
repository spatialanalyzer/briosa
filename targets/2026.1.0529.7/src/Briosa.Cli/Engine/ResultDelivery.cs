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

    /// <summary>The spill directory's volume does not keep file permissions, so nothing was written there.</summary>
    public const string SpillPermissionsUnsupported = "spill-permissions-unsupported";

    private const string SpillRefusedMessage =
        "The spill directory is on a volume that does not keep file permissions, so the CLI did not write the " +
        "result there, where other users could read it.";

    private readonly IResultFileSystem _files;
    private readonly string _spillBase;
    private readonly IReadOnlyList<string> _spillComponents;
    private readonly TimeProvider _time;

    public ResultDelivery(IResultFileSystem files, string spillBase, IReadOnlyList<string> spillComponents, TimeProvider time)
    {
        _files = files;
        _spillBase = spillBase;
        _spillComponents = spillComponents;
        _time = time;
    }

    /// <summary>
    /// Creates the requested output file before anything is sent, so that a path that
    /// cannot be written is refused while nothing has run. The file must not exist yet.
    /// </summary>
    public ResultFile? Reserve(string path, out string? rejectionCode)
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
            return _files.CreateNew(fullPath);
        }
        catch (IOException exception)
        {
            rejectionCode = (exception.HResult & 0xFFFF) switch
            {
                NativeMethods.ErrorFileExists or NativeMethods.ErrorAlreadyExists => "output-exists",
                NativeMethods.ErrorAccessDenied when Directory.Exists(fullPath) => "output-exists",
                NativeMethods.ErrorPathNotFound or NativeMethods.ErrorFileNotFound => "output-directory-missing",
                _ => "output-not-writable"
            };
            return null;
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or NotSupportedException or
            ArgumentException)
        {
            rejectionCode = "output-not-writable";
            return null;
        }
    }

    /// <summary>
    /// Removes a reserved output file that never received a result, through its own
    /// handle. A file written in full has already been closed and stays.
    /// </summary>
    public static void Release(ResultFile? output)
    {
        if (output is null || output.IsClosed)
        {
            return;
        }

        try
        {
            output.DiscardIfEmpty();
        }
        catch (IOException)
        {
            // An empty reservation left behind holds no data.
        }
    }

    /// <summary>
    /// Writes the result into <paramref name="data"/> or a file. Returns false when the
    /// result could not be written where it had to go. <paramref name="completionEstablished"/>
    /// says whether the call's own evidence shows that the operation completed; no
    /// message here claims a completion that evidence did not establish.
    /// </summary>
    public bool Deliver(IMessage result, string operationId, ResultFile? output, int maximumInlineBytes,
        bool completionEstablished, BridgeEnvelope envelope, JsonObject data)
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
            return true;
        }

        data["result"] = null;
        data["result_summary"] = Summarize(result);
        var subject = completionEstablished
            ? "The operation completed, but its result"
            : "The call's outcome is not established, and the result that came back";
        if (output is not null)
        {
            var written = Write(output, bytes, removeIncomplete: false);
            if (written.State is WriteState.Complete or WriteState.Unconfirmed)
            {
                // The whole result was read back from the file the caller named.
                data["result_file"] = FileReference(output.Path, bytes, "output_requested");
                if (written.State == WriteState.Complete)
                {
                    data["result_delivery"] = "file";
                    return true;
                }

                data["result_delivery"] = "file_unconfirmed";
                data["result_delivery_error"] = "output-finalize-failed";
                envelope.Warn("result-file-unconfirmed", subject + " was written in full to the --output file and " +
                    "read back intact, but a later step (the write, flush, or close) reported an error, so the CLI " +
                    "cannot confirm that the file stays intact. The file is kept; see data.result_file.");
                return false;
            }

            // The --output file is kept as far as it got, as documented; the result goes
            // to the spill directory instead.
            envelope.Warn("output-write-failed", subject + " could not be written in full to the --output file, " +
                "which may now be incomplete. The CLI tried the spill directory instead; see data.result_file.");
            var fallback = Spill(bytes, operationId, "output_write_failed");
            ReportSpill(fallback, subject, envelope, data, primaryError: "output-write-failed");
            return false;
        }

        var spilled = Spill(bytes, operationId, "inline_cap_exceeded");
        if (spilled.State == WriteState.Complete)
        {
            data["result_delivery"] = "file";
            data["result_file"] = spilled.Reference;
            envelope.Warn("result-spilled",
                "The result is larger than the inline cap, so it was written in full to data.result_file and only " +
                "a summary is shown. Do not run the call again to fetch it.");
            return true;
        }

        ReportSpill(spilled, subject, envelope, data, primaryError: null);
        return false;
    }

    // What a write of the result left on disk.
    private enum WriteState
    {
        // Written, read back intact, flushed, and closed without an error.
        Complete,

        // Read back intact, but the write, the flush, or the close reported an error.
        Unconfirmed,

        // Could not be read back: what the file holds is not known.
        Unverified,

        // Read back shorter or different: known not to hold the result.
        Incomplete,

        // Refused before any file was created, because the volume keeps no permissions.
        Refused,

        // No file could be created.
        NotCreated
    }

    private sealed record SpillReport(WriteState State, JsonObject? Reference, string? Leftover, long? LeftoverBytes);

    private sealed record WriteReport(WriteState State, bool Removed, long? Length);

    // Writes the whole result, reads it back through the file's own handle, then flushes
    // and closes it. Content known to be incomplete is removed through the handle when
    // asked; anything else is kept, because it may be the only copy of the result.
    private static WriteReport Write(ResultFile file, byte[] bytes, bool removeIncomplete)
    {
        var reported = false;
        try
        {
            file.Stream.Write(bytes);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            reported = true;
        }

        var content = file.ReadBack(bytes);
        if (content == ResultContent.Incomplete)
        {
            var length = file.Length();
            var removed = false;
            if (removeIncomplete)
            {
                try
                {
                    file.Discard();
                    removed = true;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    // The removal may still have happened when the close failed after it.
                    removed = !File.Exists(file.Path);
                }
            }

            Close(file);
            return new WriteReport(WriteState.Incomplete, removed, length);
        }

        if (!reported)
        {
            try
            {
                file.Stream.Flush();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                reported = true;
            }
        }

        reported |= !Close(file);
        return content == ResultContent.Unknown
            ? new WriteReport(WriteState.Unverified, false, null)
            : new WriteReport(reported ? WriteState.Unconfirmed : WriteState.Complete, false, bytes.Length);
    }

    // True when the file closed without an error.
    private static bool Close(ResultFile file)
    {
        try
        {
            file.Dispose();
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }

    private SpillReport Spill(byte[] bytes, string operationId, string reason)
    {
        ResultFile file;
        try
        {
            // The name holds only the time, the validated operation ID, and random
            // characters, never text that came back from the server.
            var name = string.Create(CultureInfo.InvariantCulture,
                $"{_time.GetUtcNow():yyyyMMdd'T'HHmmssfff'Z'}-{operationId.Replace('.', '-')}-{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(4))}.json");
            file = _files.CreatePrivate(_spillBase, _spillComponents, name);
        }
        catch (SpillPermissionsUnsupportedException)
        {
            return new SpillReport(WriteState.Refused, null, null, null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            NotSupportedException or ArgumentException or InvalidOperationException)
        {
            return new SpillReport(WriteState.NotCreated, null, null, null);
        }

        // A spill file known to be incomplete is removed, so no partial file can pass
        // for a result; one the CLI could not remove is reported.
        var written = Write(file, bytes, removeIncomplete: true);
        return written.State == WriteState.Incomplete
            ? new SpillReport(WriteState.Incomplete, null, written.Removed ? null : file.Path, written.Length)
            : new SpillReport(written.State, FileReference(file.Path, bytes, reason), null, null);
    }

    // Everything but a complete spill: each state says what is on disk, and nothing
    // claims a copy that the CLI cannot vouch for.
    private static void ReportSpill(SpillReport spill, string subject, BridgeEnvelope envelope, JsonObject data,
        string? primaryError)
    {
        data["result_file"] = spill.Reference;
        switch (spill.State)
        {
            case WriteState.Complete:
                data["result_delivery"] = "file";
                break;
            case WriteState.Unconfirmed:
                data["result_delivery"] = "file_unconfirmed";
                envelope.Warn("result-file-unconfirmed", subject + " was written in full to the spill directory and " +
                    "read back intact, but a later step (the write, flush, or close) reported an error, so the CLI " +
                    "cannot confirm that the file stays intact. The file is kept; see data.result_file.");
                break;
            case WriteState.Unverified:
                data["result_delivery"] = "file_unverified";
                envelope.Warn("result-file-unverified", subject + " was written to the spill directory, but could " +
                    "not be read back, so whether the file holds the whole result is not known. The file is kept; see " +
                    "data.result_file.");
                break;
            case WriteState.Refused:
                data["result_delivery"] = "lost";
                envelope.Warn("spill-refused", subject + " could not be spilled. " + SpillRefusedMessage +
                    " The result was discarded: it is not in this envelope or in any file.");
                break;
            default:
                data["result_delivery"] = "lost";
                envelope.Warn("output-write-failed", subject + " could not be written in full to the spill directory.");
                if (spill.Leftover is { } leftover)
                {
                    // The incomplete file is named so that no one mistakes it for a result.
                    data["result_file"] = new JsonObject
                    {
                        ["path"] = leftover,
                        ["bytes"] = spill.LeftoverBytes,
                        ["sha256"] = null,
                        ["reason"] = "incomplete_not_removed"
                    };
                    envelope.Warn("incomplete-file-left", "An incomplete spill file could not be removed; see " +
                        "data.result_file. It does not hold the whole result; delete it.");
                }

                break;
        }

        data["result_delivery_error"] = primaryError ?? spill.State switch
        {
            WriteState.Unconfirmed => "spill-finalize-failed",
            WriteState.Unverified => "spill-verify-failed",
            WriteState.Refused => SpillPermissionsUnsupported,
            _ => "spill-write-failed"
        };
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
