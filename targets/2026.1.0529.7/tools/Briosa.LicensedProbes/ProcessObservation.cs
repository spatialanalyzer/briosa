using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Briosa.LicensedProbes;

/// <summary>Waits between process snapshots. Tests replace it so that no test sleeps.</summary>
internal interface IPause
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

internal sealed class SystemPause : IPause
{
    public static SystemPause Instance { get; } = new();

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) => Task.Delay(delay, cancellationToken);
}

/// <summary>
/// The read-only process observation for session 4's preflight and postflight (#402):
/// <c>--observe-processes</c>. It only reads the process list, every 500 ms, until a complete
/// snapshot shows no SDK, worker or server process, or the window ends. It connects to nothing,
/// opens no process except to read a tracked one's start time and session with a query-only
/// handle, and has no code path that stops, terminates or signals anything. Its records hold
/// labels, roles, parent roles, start times, offsets and counts; never a process id, path or
/// argument.
/// </summary>
internal static class ProcessObservation
{
    public const string Option = "--observe-processes";

    // 2: every process offset is bounded by the snapshot that saw it (snapshot start or end).
    public const int SchemaVersion = 2;

    public static TimeSpan PollInterval { get; } = TimeSpan.FromMilliseconds(500);

    public const string Usage = """
        Usage (read-only; connects to nothing and acts on no process):
          Briosa.LicensedProbes --observe-processes --output-directory <empty dir>
              [--observe-seconds <0-1800>, default 120; 0 takes one snapshot] [--harness-revision <sha>]
        """;

    internal sealed record Options(string OutputDirectory, TimeSpan Window, string? HarnessRevision);

    internal sealed record Record(
        DateTimeOffset StartedAt,
        DateTimeOffset CompletedAt,
        TimeSpan Window,
        string Result,
        IReadOnlyList<TrackedProcess> Processes,
        int CompleteSnapshots,
        int IncompleteSnapshots,
        int FailedSnapshots,
        SnapshotStatus? FinalSnapshot,
        string? HarnessRevision)
    {
        /// <summary>Whether the residual list is known: the observation ended with a complete snapshot.</summary>
        public bool ResidualKnown => FinalSnapshot == SnapshotStatus.Complete;

        /// <summary>SDK, worker and server processes alive at the end.</summary>
        public IReadOnlyList<TrackedProcess> Residual => [.. Processes.Where(static process => process.Alive &&
            process.Role is ProcessRoles.Sdk or ProcessRoles.Worker or ProcessRoles.Server)];

        public int SpatialAnalyzerAlive => Processes.Count(static process => process.Alive && process.Role == ProcessRoles.SpatialAnalyzer);
    }

    public static bool IsObservation(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return arguments.Contains(Option, StringComparer.Ordinal);
    }

    public static Options Parse(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var flags = 0;
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            if (string.Equals(argument, Option, StringComparison.Ordinal))
            {
                if (++flags > 1)
                    throw new ProbeUsageException($"Option '{Option}' is repeated.");
                continue;
            }

            if (argument is not ("--output-directory" or "--observe-seconds" or "--harness-revision"))
                throw new ProbeUsageException("An unknown option was supplied.");
            if (index + 1 >= arguments.Count)
                throw new ProbeUsageException($"Option '{argument}' needs a value.");
            if (!values.TryAdd(argument, arguments[++index]))
                throw new ProbeUsageException($"Option '{argument}' is repeated.");
        }

        var output = values.GetValueOrDefault("--output-directory") ?? throw new ProbeUsageException("--output-directory is required.");
        var window = TimeSpan.FromSeconds(120);
        if (values.TryGetValue("--observe-seconds", out var text))
        {
            window = int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) && seconds is >= 0 and <= 1800
                ? TimeSpan.FromSeconds(seconds)
                : throw new ProbeUsageException("--observe-seconds must be between 0 and 1800.");
        }

        var revision = values.GetValueOrDefault("--harness-revision");
        if (revision is not null && (revision.Length != 40 || !revision.All(char.IsAsciiHexDigitLower)))
            throw new ProbeUsageException("--harness-revision must be a 40-character lowercase commit SHA.");
        return new Options(output, window, revision);
    }

    /// <summary>Snapshots until a complete one shows no SDK, worker or server process, or the window ends.</summary>
    public static async Task<Record> ObserveAsync(Options options, IProcessTable table, IPause pause, TimeProvider clock, int harnessProcessId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(pause);
        ArgumentNullException.ThrowIfNull(clock);
        var tracker = new ProcessTracker(harnessProcessId);
        var started = clock.GetUtcNow();
        long Offset() => (long)(clock.GetUtcNow() - started).TotalMilliseconds;
        var window = (long)options.Window.TotalMilliseconds;
        string result;
        while (true)
        {
            // A snapshot takes time: the offsets before and after bound what it saw.
            var snapshotStarted = Offset();
            var snapshot = table.Snapshot();
            tracker.Observe(snapshot, snapshotStarted, Offset());
            if (tracker.NoneConfirmed(ProcessRoles.Sdk, ProcessRoles.Worker, ProcessRoles.Server))
            {
                result = "settled";
                break;
            }

            if (Offset() >= window)
            {
                result = "window-elapsed";
                break;
            }

            try
            {
                await pause.DelayAsync(PollInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The record is still written; what was seen so far stands.
                result = "cancelled";
                break;
            }
        }

        return new Record(started, clock.GetUtcNow(), options.Window, result, tracker.Processes, tracker.CompleteSnapshots,
            tracker.IncompleteSnapshots, tracker.FailedSnapshots, tracker.LastStatus, options.HarnessRevision);
    }

    public static async Task<int> RunAsync(IReadOnlyList<string> arguments, TextWriter output, TextWriter error, IProcessTable table, IPause pause,
        TimeProvider clock, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        Options options;
        try
        {
            options = Parse(arguments);
            if (File.Exists(options.OutputDirectory) ||
                (Directory.Exists(options.OutputDirectory) && Directory.EnumerateFileSystemEntries(options.OutputDirectory).Any()))
                throw new ProbeUsageException("--output-directory must be a new or empty folder; records are never overwritten.");
        }
        catch (ProbeUsageException exception)
        {
            await error.WriteLineAsync(exception.Message).ConfigureAwait(false);
            await error.WriteLineAsync(Usage).ConfigureAwait(false);
            return 2;
        }

        var record = await ObserveAsync(options, table, pause, clock, Environment.ProcessId, cancellationToken).ConfigureAwait(false);
        var stem = Path.Combine(options.OutputDirectory, $"process-observation-{ProbeTarget.SpatialAnalyzerTarget}");
        if (!await RecordFiles.TryWriteNewAsync(error, (stem + ".json", ToJson(record)), (stem + ".md", ToMarkdown(record))).ConfigureAwait(false))
        {
            return 2;
        }

        await output.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
            $"Process observation: {record.Result}; final snapshot {SnapshotName(record.FinalSnapshot)}; " +
            $"spatial_analyzer_alive={record.SpatialAnalyzerAlive}; residual={ResidualText(record)}.")).ConfigureAwait(false);
        await output.WriteLineAsync($"Records: {Path.GetFileName(stem)}.json, {Path.GetFileName(stem)}.md").ConfigureAwait(false);
        return record.ResidualKnown && record.Residual.Count == 0 ? 0 : 1;
    }

    public static string ToJson(Record record)
    {
        ArgumentNullException.ThrowIfNull(record);
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteNumber("schema_version", SchemaVersion);
            json.WriteString("issue", ProbeTarget.Session4Issue);
            json.WriteString("spatial_analyzer_target", ProbeTarget.SpatialAnalyzerTarget);
            json.WriteString("started_at", record.StartedAt.ToString("O", CultureInfo.InvariantCulture));
            json.WriteString("completed_at", record.CompletedAt.ToString("O", CultureInfo.InvariantCulture));
            json.WriteNumber("observe_window_seconds", (long)record.Window.TotalSeconds);
            json.WriteNumber("poll_interval_ms", (long)PollInterval.TotalMilliseconds);
            json.WriteString("result", record.Result);
            if (record.HarnessRevision is { } revision)
                json.WriteString("harness_revision", revision);
            else
                json.WriteNull("harness_revision");
            json.WriteStartObject("snapshots");
            json.WriteNumber("complete", record.CompleteSnapshots);
            json.WriteNumber("incomplete", record.IncompleteSnapshots);
            json.WriteNumber("failed", record.FailedSnapshots);
            json.WriteString("final", SnapshotName(record.FinalSnapshot));
            json.WriteEndObject();
            json.WriteStartArray("processes");
            foreach (var process in record.Processes)
            {
                json.WriteStartObject();
                json.WriteString("label", process.Label);
                json.WriteString("role", process.Role);
                json.WriteString("parent_role", process.ParentRole);
                if (process.StartedAt is { } at)
                    json.WriteString("started_at", at.ToString("O", CultureInfo.InvariantCulture));
                else
                    json.WriteNull("started_at");
                json.WriteBoolean("present_at_start", process.PresentAtStart);

                // No exit time is claimed. The process existed by the end of the first snapshot
                // that listed it, was still running at some moment after the start of the last
                // one, and had exited by the end of the first complete snapshot without it.
                json.WriteNumber("first_seen_snapshot_end_ms", process.FirstSeenMilliseconds);
                json.WriteNumber("last_seen_snapshot_start_ms", process.LastSeenMilliseconds);
                if (process.FirstAbsentMilliseconds is { } absent)
                    json.WriteNumber("first_absent_snapshot_end_ms", absent);
                else
                    json.WriteNull("first_absent_snapshot_end_ms");
                json.WriteNumber("reappearances", process.Reappearances);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteNumber("spatial_analyzer_alive", record.SpatialAnalyzerAlive);
            json.WriteBoolean("residual_known", record.ResidualKnown);
            json.WriteStartArray("residual");
            foreach (var process in record.Residual)
            {
                json.WriteStringValue(process.Label);
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray()) + "\n";
    }

    public static string ToMarkdown(Record record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture,
            $"# SA {ProbeTarget.SpatialAnalyzerTarget} process observation ({ProbeTarget.Session4Issue}) — {record.StartedAt.UtcDateTime:yyyy-MM-dd}\n\n");
        text.Append(CultureInfo.InvariantCulture,
            $"Read-only: the list of processes only, every {PollInterval.TotalMilliseconds:0} ms for up to {record.Window.TotalSeconds:0} s. Result: `{record.Result}`. ")
            .Append(CultureInfo.InvariantCulture,
                $"Snapshots: {record.CompleteSnapshots} complete, {record.IncompleteSnapshots} incomplete, {record.FailedSnapshots} failed; the last was {SnapshotName(record.FinalSnapshot)}.\n\n");
        text.Append("| Process | Role | Parent | Present at start | First listed by (snapshot end, ms) | Last listed after (snapshot start, ms) | First absent by (snapshot end, ms) |\n")
            .Append("| --- | --- | --- | --- | --- | --- | --- |\n");
        foreach (var process in record.Processes)
        {
            var absent = process.FirstAbsentMilliseconds?.ToString(CultureInfo.InvariantCulture) ??
                (record.ResidualKnown ? "alive at end" : "unknown: final census incomplete; see last seen");
            text.Append(CultureInfo.InvariantCulture,
                $"| `{process.Label}` | {process.Role} | {process.ParentRole} | {(process.PresentAtStart ? "yes" : "no")} | {process.FirstSeenMilliseconds} | {process.LastSeenMilliseconds} | {absent} |\n");
        }

        text.Append("\nA snapshot takes time, so each offset is that of a snapshot's start or end. A process was still running at some ")
            .Append("moment after the start of the last snapshot that listed it, and had exited no later than the end of the first ")
            .Append("complete snapshot without it; no exit time is measured. Only a complete snapshot can show a process absent.\n");
        text.Append(!record.ResidualKnown
            ? "\nResidual: unknown. The observation ended without a complete snapshot, so postflight cannot pass on this record.\n"
            : record.Residual.Count == 0
                ? "\nResidual: none. No SDK, worker or server process was alive at the end.\n"
                : $"\nResidual: {string.Join(", ", record.Residual.Select(static process => $"`{process.Label}`"))} alive at the end. " +
                  "Each is unresolved; the session never stops one.\n");
        text.Append("\nThese are observations of one bounded run, not vendor guarantees.\n");
        return text.ToString();
    }

    private static string ResidualText(Record record) =>
        !record.ResidualKnown ? "unknown (the last snapshot was not complete)" :
        record.Residual.Count == 0 ? "none" : string.Join(",", record.Residual.Select(static process => process.Label));

    private static string SnapshotName(SnapshotStatus? status) => status switch
    {
        SnapshotStatus.Complete => "complete",
        SnapshotStatus.Incomplete => "incomplete",
        SnapshotStatus.Failed => "failed",
        _ => "none"
    };
}

/// <summary>
/// Writes a command's records into its output folder, never over an existing file. A folder or
/// file that cannot be written ends the command with a fixed message that names no path.
/// </summary>
internal static class RecordFiles
{
    public const string WriteFailure = "The records could not be written to --output-directory; nothing was overwritten.";

    public static async Task<bool> TryWriteNewAsync(TextWriter error, params (string Path, string Content)[] files)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(files);
        try
        {
            foreach (var (path, content) in files)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                await using (stream.ConfigureAwait(false))
                {
                    // Written on no token: a cancellation never loses a record that is already known.
                    await stream.WriteAsync(new UTF8Encoding(false).GetBytes(content), CancellationToken.None).ConfigureAwait(false);
                }
            }

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            await error.WriteLineAsync(WriteFailure).ConfigureAwait(false);
            return false;
        }
    }
}
