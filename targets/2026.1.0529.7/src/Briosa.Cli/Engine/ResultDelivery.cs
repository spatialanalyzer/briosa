using System.Diagnostics.CodeAnalysis;
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

    /// <summary>The warning given whenever the delivery state is <c>lost</c>.</summary>
    public const string ResultLostMessage =
        "The result is not in this envelope, and no file this call left holds it in full.";

    private readonly IResultFileSystem _files;
    private readonly string _spillBase;
    private readonly IReadOnlyList<string> _spillComponents;
    private readonly TimeProvider _time;
    private readonly Func<byte[], byte[]> _sha256;

    /// <summary>Tests may replace the SHA-256 computation, to make it fail.</summary>
    public ResultDelivery(IResultFileSystem files, string spillBase, IReadOnlyList<string> spillComponents, TimeProvider time,
        Func<byte[], byte[]>? sha256 = null)
    {
        _files = files;
        _spillBase = spillBase;
        _spillComponents = spillComponents;
        _time = time;
        _sha256 = sha256 ?? SHA256.HashData;
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
    /// handle. Returns true when it could not be removed and stays on disk;
    /// <paramref name="bytes"/> is then its measured size, or null when that could not be
    /// read. A file that received a result has already been closed, and is reported by
    /// <see cref="Deliver"/>.
    /// </summary>
    public static bool Release(ResultFile? output, out long? bytes)
    {
        bytes = null;
        return output is not null && !output.IsClosed && !output.RemoveIfEmpty(out bytes);
    }

    /// <summary>
    /// The reference to an unused <c>--output</c> reservation that could not be removed:
    /// empty, and labeled so that no one takes it for a result.
    /// </summary>
    public static JsonObject ReservationLeft(string path, long? bytes) => new()
    {
        ["path"] = path,
        ["bytes"] = bytes,
        ["sha256"] = null,
        ["reason"] = "reservation_not_removed",
        ["verification"] = "incomplete",
        ["other_files"] = new JsonArray()
    };

    /// <summary>
    /// Writes the result into <paramref name="data"/> or a file. Returns false when the
    /// result could not be delivered where it had to go. <paramref name="completionEstablished"/>
    /// says whether the call's own evidence shows that the operation completed; no
    /// message here claims a completion that evidence did not establish.
    /// </summary>
    /// <remarks>
    /// Everything that can fail without touching a file runs first: formatting the
    /// result, summarizing it, and computing its SHA-256. A formatting failure is
    /// reported before any file exists; a SHA-256 that cannot be computed only leaves
    /// that field empty. Once a file exists, the writes and the spill catch every
    /// failure, and the report is built in memory from one record per file this call
    /// wrote: what reading it back showed, whether a later step reported an error, and
    /// whether it is still on disk. Nothing else is consulted, so every combination of
    /// failures is reported by the same rules; DeliveryFaultMatrixTests checks the
    /// delivery invariants on the operations page across those combinations.
    /// </remarks>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "A result that cannot be formatted is reported as such, before any file exists.")]
    public bool Deliver(IMessage result, string operationId, ResultFile? output, int maximumInlineBytes,
        bool completionEstablished, BridgeEnvelope envelope, JsonObject data)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(data);
        byte[] bytes;
        JsonNode? inline = null;
        JsonObject? summary = null;
        try
        {
            var json = ProtocolJson.Format(result);
            bytes = Encoding.UTF8.GetBytes(json);
            if (output is null && bytes.Length <= maximumInlineBytes)
            {
                inline = JsonNode.Parse(json);
            }
            else
            {
                summary = Summarize(result);
            }
        }
        catch (Exception)
        {
            data["result_delivery"] = "lost";
            data["result_delivery_error"] = "result-format-failed";
            envelope.Warn("result-lost", ResultLostMessage);
            return false;
        }

        data["result_message"] = result.Descriptor.FullName;
        data["result_json_bytes"] = bytes.Length;
        data["inline_cap_bytes"] = maximumInlineBytes;
        if (inline is not null)
        {
            data["result_delivery"] = "inline";
            data["result"] = inline;
            return true;
        }

        data["result"] = null;
        data["result_summary"] = summary;
        var sha256 = Sha256(bytes);
        var requested = output is null ? null : Write(output, bytes, requested: true, "output_requested");
        // A requested file that read back as the whole result is not copied; any other
        // outcome of it is backed by a spill.
        var spill = requested is { Content: FileContent.Matched or FileContent.MatchedUnconfirmed }
            ? null
            : Spill(bytes, operationId, output is null ? "inline_cap_exceeded" : "output_write_failed");
        return Report(bytes.Length, sha256, requested, spill, completionEstablished, envelope, data);
    }

    // The result's SHA-256 in lowercase hex, or null when it cannot be computed.
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "A SHA-256 that cannot be computed leaves that field empty; it never loses a file.")]
    private string? Sha256(byte[] bytes)
    {
        try
        {
            return Convert.ToHexStringLower(_sha256(bytes));
        }
        catch (Exception)
        {
            return null;
        }
    }

    // What a file this call wrote holds, as far as the CLI can tell.
    private enum FileContent
    {
        // Read back through its handle as exactly the result; the write, the flush to
        // disk, and the close reported no error.
        Matched,

        // Read back as exactly the result, but the write, the flush to disk, or the
        // close reported an error.
        MatchedUnconfirmed,

        // Could not be read back: it may hold the whole result.
        NotReadBack,

        // Read back shorter or different, or never written: known not to hold the result.
        Incomplete
    }

    // A file this call wrote, or created. OnDisk is false only for an incomplete file
    // that was removed through its handle; a file that may hold the result is never
    // removed.
    private sealed record WrittenFile(string Path, bool Requested, FileContent Content, bool OnDisk, long? BytesOnDisk,
        string Reason);

    private enum SpillFailure
    {
        None,

        // The volume keeps no permissions, so nothing was created.
        Refused,

        // No spill file could be created, or one was created and removed when it failed
        // its checks: nothing is left.
        NotCreated,

        // A spill file was created but failed its checks before anything was written to
        // it, and could not be removed: File names it.
        NotHandedOver
    }

    private sealed record SpillAttempt(SpillFailure Failure, WrittenFile? File);

    // Writes the whole result, reads it back through the file's own handle and compares
    // every byte, asks for it to be flushed to the storage device, and closes it. Content
    // known to be incomplete is removed through the handle, on either destination: this
    // call created the file. Anything else is kept, because it may be the only copy.
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Whatever a step throws, the file is still reported for what it holds.")]
    private static WrittenFile Write(ResultFile file, byte[] bytes, bool requested, string reason)
    {
        var reported = false;
        try
        {
            file.Write(bytes);
        }
        catch (Exception)
        {
            reported = true;
        }

        // The read-back's own measurement is the incomplete file's size: no one else can
        // write to it while this call holds it.
        var content = file.ReadBack(bytes, out var length);
        if (content == ResultContent.Incomplete)
        {
            var removed = file.Remove();
            return new WrittenFile(file.Path, requested, FileContent.Incomplete, !removed, length, reason);
        }

        try
        {
            file.FlushToDisk();
        }
        catch (Exception)
        {
            reported = true;
        }

        reported |= !file.TryClose();
        var state = content == ResultContent.Unknown ? FileContent.NotReadBack
            : reported ? FileContent.MatchedUnconfirmed
            : FileContent.Matched;
        return new WrittenFile(file.Path, requested, state, OnDisk: true, BytesOnDisk: null, reason);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Whatever stops the spill file from being created, the result is still reported.")]
    private SpillAttempt Spill(byte[] bytes, string operationId, string reason)
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
            return new SpillAttempt(SpillFailure.Refused, null);
        }
        catch (ResultFileLeftException left)
        {
            // Created, but neither handed over nor removed: nothing was written to it.
            return new SpillAttempt(SpillFailure.NotHandedOver,
                new WrittenFile(left.Path, Requested: false, FileContent.Incomplete, OnDisk: true, BytesOnDisk: 0, reason));
        }
        catch (Exception)
        {
            return new SpillAttempt(SpillFailure.NotCreated, null);
        }

        return new SpillAttempt(SpillFailure.None, Write(file, bytes, requested: false, reason));
    }

    // Everything the envelope says about the delivery, derived from the files alone, in
    // memory: nothing here calls out, so nothing here can fail and drop a file. The files
    // are named first.
    private static bool Report(int length, string? sha256, WrittenFile? requested, SpillAttempt? spill,
        bool completionEstablished, BridgeEnvelope envelope, JsonObject data)
    {
        // Every file this call left on disk, best copy first; on a tie, the --output file.
        var left = new[] { requested, spill?.File }.OfType<WrittenFile>().Where(file => file.OnDisk)
            .OrderBy(file => file.Content).ToList();
        var primary = left.FirstOrDefault();
        if (primary is null)
        {
            data["result_file"] = null;
        }
        else
        {
            var reference = Reference(primary, length, sha256);
            reference["other_files"] = new JsonArray([.. left.Skip(1).Select(file => (JsonNode)Reference(file, length, sha256))]);
            data["result_file"] = reference;
        }

        data["result_delivery"] = primary?.Content switch
        {
            FileContent.Matched => "file",
            FileContent.MatchedUnconfirmed => "file_unconfirmed",
            FileContent.NotReadBack => "file_unverified",
            _ => "lost"
        };

        // Delivered as asked: to the --output file when there is one, otherwise to the spill.
        var delivered = (requested ?? spill?.File)?.Content == FileContent.Matched;
        data["result_delivery_error"] = delivered ? null : Error(requested, spill);

        var subject = completionEstablished
            ? "The operation completed, but its result"
            : "The call's outcome is not established, and the result that came back";
        if (requested is not null)
        {
            Warn(envelope, requested, subject, spilled: false, sha256);
        }

        switch (spill?.Failure)
        {
            case SpillFailure.Refused:
                envelope.Warn("spill-refused", subject + " could not be spilled. " + SpillRefusedMessage);
                break;
            case SpillFailure.NotCreated:
                envelope.Warn("spill-write-failed", subject + " could not be written to the spill directory: no " +
                    "spill file could be created and checked, and none is left there.");
                break;
            case SpillFailure.NotHandedOver:
                envelope.Warn("spill-write-failed", subject + " could not be written to the spill directory: a " +
                    "new spill file failed its checks before anything was written to it, and could not be removed.");
                WarnLeft(envelope);
                break;
            case SpillFailure.None:
                Warn(envelope, spill.File!, subject, spilled: requested is null, sha256);
                break;
        }

        if (primary is null or { Content: FileContent.Incomplete })
        {
            envelope.Warn("result-lost", ResultLostMessage);
        }

        // Whatever the state: it says only that the SHA-256 is missing.
        if (sha256 is null)
        {
            envelope.Warn("result-hash-unavailable", "The result's SHA-256 could not be computed, so data.result_file " +
                "gives no SHA-256 for any file.");
        }

        return delivered;
    }

    // Each message says only what the file's record establishes.
    private static void Warn(BridgeEnvelope envelope, WrittenFile file, string subject, bool spilled, string? sha256)
    {
        var where = file.Requested ? "the --output file" : "the spill directory";
        var backup = file.Requested ? " The CLI also tried the spill directory." : string.Empty;
        switch (file.Content)
        {
            case FileContent.Matched when spilled:
                envelope.Warn("result-spilled",
                    "The result is larger than the inline cap, so it was written in full to data.result_file and only " +
                    "a summary is shown. Do not run the call again to fetch it.");
                break;
            case FileContent.MatchedUnconfirmed:
                envelope.Warn("result-file-unconfirmed", subject + " was written in full to " + where + " and read " +
                    "back intact, but the write, the flush to disk, or the close reported an error, so the CLI cannot " +
                    "confirm that the file stays intact. The file is kept; see data.result_file." + CheckBeforeUse(sha256));
                break;
            case FileContent.NotReadBack:
                envelope.Warn("result-file-unverified", subject + " was sent to " + where + ", but the file could not " +
                    "be read back, so whether it holds the whole result is not known. The file is kept; see " +
                    "data.result_file." + CheckBeforeUse(sha256) + backup);
                break;
            case FileContent.Incomplete:
                envelope.Warn(file.Requested ? "output-write-failed" : "spill-write-failed", subject + " did not read " +
                    "back intact from " + where + ": the file is shorter than the result or differs from it." +
                    (file.OnDisk ? string.Empty : " The incomplete file was removed.") + backup);
                if (file.OnDisk)
                {
                    WarnLeft(envelope);
                }

                break;
        }
    }

    private static void WarnLeft(BridgeEnvelope envelope) =>
        envelope.Warn("incomplete-file-left", "An incomplete result file could not be removed; it is listed in " +
            "data.result_file or its other_files with the verification incomplete and the reason " +
            "incomplete_not_removed. It does not hold the whole result; delete it.");

    // What to do before using a file whose content is not confirmed: check it against the
    // size and SHA-256 when there is one; without a SHA-256, its size alone cannot verify it.
    private static string CheckBeforeUse(string? sha256) => sha256 is not null
        ? " Before using it, check that its size and SHA-256 equal the bytes and sha256 given for it in " +
          "data.result_file."
        : " Its SHA-256 could not be computed, so its size alone cannot verify it; do not use it without another " +
          "way to confirm its content.";

    // The first failure: the --output file's when there is one, otherwise the spill's.
    private static string Error(WrittenFile? requested, SpillAttempt? spill) =>
        requested is not null
            ? requested.Content switch
            {
                FileContent.MatchedUnconfirmed => "output-finalize-failed",
                FileContent.NotReadBack => "output-verify-failed",
                _ => "output-write-failed"
            }
            : spill?.Failure switch
            {
                SpillFailure.Refused => SpillPermissionsUnsupported,
                SpillFailure.None => spill.File!.Content switch
                {
                    FileContent.MatchedUnconfirmed => "spill-finalize-failed",
                    FileContent.NotReadBack => "spill-verify-failed",
                    _ => "spill-write-failed"
                },
                _ => "spill-write-failed"
            };

    // A file that may hold the result carries the result's size and SHA-256, to check it
    // against, or a null SHA-256 when it could not be computed; one known to be
    // incomplete carries its size on disk and no SHA-256.
    private static JsonObject Reference(WrittenFile file, int length, string? sha256) => file.Content == FileContent.Incomplete
        ? new JsonObject
        {
            ["path"] = file.Path,
            ["bytes"] = file.BytesOnDisk,
            ["sha256"] = null,
            ["reason"] = "incomplete_not_removed",
            ["verification"] = "incomplete"
        }
        : new JsonObject
        {
            ["path"] = file.Path,
            ["bytes"] = length,
            ["sha256"] = sha256,
            ["reason"] = file.Reason,
            ["verification"] = file.Content switch
            {
                FileContent.Matched => "matched",
                FileContent.MatchedUnconfirmed => "matched_unconfirmed",
                _ => "not_read_back"
            }
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
