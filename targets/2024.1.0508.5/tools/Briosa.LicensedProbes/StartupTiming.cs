using System.Globalization;
using System.Text;
using System.Text.Json;
using Briosa.Server.Workers;

namespace Briosa.LicensedProbes;

/// <summary>Which start a cold-start measurement is (#371, #402). The runbook defines how the operator establishes each.</summary>
internal enum StartupCondition
{
    // The first StartSpatialAnalyzerSdk on a package extracted into a new, never-used folder.
    FreshInstall,

    // The first StartSpatialAnalyzerSdk after the machine restarted.
    AfterReboot,

    // A start on a server and package that have already started the SDK.
    Warm
}

/// <summary>
/// Reads the server's own JSON log, offline, for the startup timings it already records
/// (#412): EndpointListening (1001), WorkerStartPhase (1203), and the lifecycle RPC records
/// (1303 to 1305). It connects to nothing and adds no timing of its own. Only event names,
/// closed-vocabulary phases and statuses, stable diagnostic codes, integers and timestamps are
/// read; any other value is ignored.
/// </summary>
internal static class StartupTiming
{
    public const string Option = "--startup-timing";

    public const string StartRpc = "StartSpatialAnalyzerSdk";
    public const string ConnectRpc = "ConnectToSpatialAnalyzer";

    private static readonly HashSet<string> Phases = new(StringComparer.Ordinal)
    {
        "process-created", "control-pipe-connected", "ready-received", "startup-bound-expired", "start-failed"
    };

    private static readonly HashSet<string> Rpcs = new(StringComparer.Ordinal) { StartRpc, ConnectRpc };

    public static bool IsStartupTiming(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return arguments.Contains(Option, StringComparer.Ordinal);
    }

    public static string ConditionName(StartupCondition condition) => condition switch
    {
        StartupCondition.FreshInstall => "fresh-install",
        StartupCondition.AfterReboot => "after-reboot",
        _ => "warm"
    };

    /// <summary>One log record the analysis uses.</summary>
    internal sealed record LogEvent(DateTimeOffset Timestamp, int EventId, string Instance, IReadOnlyDictionary<string, string> Fields);

    internal sealed record PhaseTiming(string Phase, long ElapsedMilliseconds);

    internal sealed record RpcTiming(string Exit, string? GrpcStatus, string? DiagnosticCode, long? ElapsedMilliseconds);

    internal sealed record StartAttempt(int Ordinal, int? Generation, RpcTiming Start, IReadOnlyList<PhaseTiming> Phases, RpcTiming? Connect);

    internal sealed record Report(
        StartupCondition Condition,
        long? EndpointListeningMilliseconds,
        IReadOnlyList<StartAttempt> Attempts,
        int UnreadableLines,
        string? HarnessRevision);

    /// <summary>Parses one JSON log line, or returns null when it is not a usable record.</summary>
    internal static LogEvent? ParseLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("Timestamp", out var timestamp) || timestamp.ValueKind != JsonValueKind.String ||
                !DateTimeOffset.TryParse(timestamp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at) ||
                !root.TryGetProperty("Properties", out var properties) || properties.ValueKind != JsonValueKind.Object ||
                !properties.TryGetProperty("EventId", out var eventId) || eventId.ValueKind != JsonValueKind.Object ||
                !eventId.TryGetProperty("Id", out var id) || !id.TryGetInt32(out var number) ||
                !properties.TryGetProperty("ServerInstanceId", out var instance) || instance.ValueKind != JsonValueKind.String ||
                instance.GetString() is not { Length: 32 } instanceId || !instanceId.All(char.IsAsciiHexDigitLower))
            {
                return null;
            }

            var fields = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var name in new[] { "Phase", "Rpc", "GrpcStatus", "DiagnosticCode" })
            {
                if (properties.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String &&
                    value.GetString() is { } text && Accept(name, text))
                {
                    fields[name] = text;
                }
            }

            foreach (var name in new[] { "ElapsedMilliseconds", "Generation" })
            {
                if (properties.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var integer))
                {
                    fields[name] = integer.ToString(CultureInfo.InvariantCulture);
                }
            }

            return new LogEvent(at.ToUniversalTime(), number, instanceId, fields);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool Accept(string name, string text) => name switch
    {
        "Phase" => Phases.Contains(text),
        "Rpc" => Rpcs.Contains(text),
        "GrpcStatus" => Enum.TryParse<Grpc.Core.StatusCode>(text, ignoreCase: false, out _) && text.All(char.IsAsciiLetter),
        "DiagnosticCode" => SafeCode.OrNull(text) is not null,
        _ => false
    };

    /// <summary>
    /// Builds the report from one server instance's events. Each StartSpatialAnalyzerSdk is an
    /// attempt, in order; its worker start phases are those logged between its arrival and its
    /// exit, and its Connect is the next ConnectToSpatialAnalyzer exit before the next start.
    /// </summary>
    internal static Report Analyze(StartupCondition condition, IReadOnlyList<LogEvent> events, int unreadableLines, string? harnessRevision)
    {
        ArgumentNullException.ThrowIfNull(events);
        var ordered = events.OrderBy(static item => item.Timestamp).ToList();
        long? listening = ordered.FirstOrDefault(static item => item.EventId == 1001) is { } first && Long(first, "ElapsedMilliseconds") is { } ms
            ? ms
            : null;

        var attempts = new List<StartAttempt>();
        var starts = ordered.Where(static item => item.EventId == 1303 && Field(item, "Rpc") == StartRpc).ToList();
        for (var index = 0; index < starts.Count; index++)
        {
            var arrival = starts[index];
            var next = index + 1 < starts.Count ? starts[index + 1].Timestamp : DateTimeOffset.MaxValue;
            var exit = ordered.FirstOrDefault(item => item.Timestamp >= arrival.Timestamp && item.Timestamp < next &&
                item.EventId is 1302 or 1304 or 1305 && Field(item, "Rpc") == StartRpc);
            var until = exit?.Timestamp ?? next;
            var phases = ordered
                .Where(item => item.EventId == 1203 && item.Timestamp >= arrival.Timestamp && item.Timestamp <= until && Field(item, "Phase") is not null)
                .Select(static item => (Phase: Field(item, "Phase")!, Elapsed: Long(item, "ElapsedMilliseconds"), Generation: Long(item, "Generation")))
                .Where(static item => item.Elapsed is >= 0)
                .ToList();
            var connect = ordered.FirstOrDefault(item => item.Timestamp >= until && item.Timestamp < next &&
                item.EventId is 1302 or 1304 or 1305 && Field(item, "Rpc") == ConnectRpc);
            attempts.Add(new StartAttempt(
                index + 1,
                phases.Select(static item => item.Generation).FirstOrDefault(static generation => generation is not null) is { } generation ? (int)generation : null,
                Exit(exit),
                [.. phases.Select(static item => new PhaseTiming(item.Phase, item.Elapsed!.Value))],
                connect is null ? null : Exit(connect)));
        }

        return new Report(condition, listening, attempts, unreadableLines, harnessRevision);
    }

    private static RpcTiming Exit(LogEvent? exit) => exit switch
    {
        null => new RpcTiming("no-exit-recorded", null, null, null),
        _ => new RpcTiming(
            exit.EventId switch { 1304 => "completed", 1302 => "rejected", _ => "faulted" },
            Field(exit, "GrpcStatus"), Field(exit, "DiagnosticCode"),
            Long(exit, "ElapsedMilliseconds") is { } elapsed && elapsed >= 0 ? elapsed : null)
    };

    private static string? Field(LogEvent item, string name) => item.Fields.TryGetValue(name, out var value) ? value : null;

    private static long? Long(LogEvent item, string name) =>
        Field(item, name) is { } text && long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) ? value : null;

    public static string ToJson(Report report)
    {
        ArgumentNullException.ThrowIfNull(report);
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteNumber("schema_version", 1);
            json.WriteString("issue", ProbeTarget.Session4Issue);
            json.WriteString("spatial_analyzer_target", ProbeTarget.SpatialAnalyzerTarget);
            json.WriteString("condition", ConditionName(report.Condition));
            json.WriteString("first_attempt_is", ConditionName(report.Condition));
            json.WriteNumber("default_worker_startup_timeout_ms", (long)WorkerProcessOptions.DefaultStartupTimeout.TotalMilliseconds);
            if (report.EndpointListeningMilliseconds is { } listening)
                json.WriteNumber("endpoint_listening_ms", listening);
            else
                json.WriteNull("endpoint_listening_ms");
            json.WriteNumber("unreadable_lines", report.UnreadableLines);
            if (report.HarnessRevision is { } revision)
                json.WriteString("harness_revision", revision);
            else
                json.WriteNull("harness_revision");
            json.WriteStartArray("start_attempts");
            foreach (var attempt in report.Attempts)
            {
                json.WriteStartObject();
                json.WriteNumber("ordinal", attempt.Ordinal);
                json.WriteString("label", attempt.Ordinal == 1 ? ConditionName(report.Condition) : "repeat");
                if (attempt.Generation is { } generation)
                    json.WriteNumber("sdk_generation", generation);
                else
                    json.WriteNull("sdk_generation");
                WriteRpc(json, "start", attempt.Start);
                json.WriteStartObject("phases_ms");
                foreach (var phase in attempt.Phases)
                {
                    json.WriteNumber(phase.Phase, phase.ElapsedMilliseconds);
                }

                json.WriteEndObject();
                if (attempt.Connect is { } connect)
                    WriteRpc(json, "connect", connect);
                else
                    json.WriteNull("connect");
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray()) + "\n";
    }

    public static string ToMarkdown(Report report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture,
            $"# SA {ProbeTarget.SpatialAnalyzerTarget} worker start timing: {ConditionName(report.Condition)} ({ProbeTarget.Session4Issue})\n\n");
        text.Append(CultureInfo.InvariantCulture,
            $"From the server's own log events (EndpointListening, WorkerStartPhase, lifecycle RPC records). Endpoint listening: {Ms(report.EndpointListeningMilliseconds)} after process start. ")
            .Append(CultureInfo.InvariantCulture,
                $"The product's default worker startup bound is {(long)WorkerProcessOptions.DefaultStartupTimeout.TotalMilliseconds} ms. ")
            .Append(CultureInfo.InvariantCulture, $"Unreadable log lines skipped: {report.UnreadableLines}.\n\n");
        text.Append("| Attempt | Label | Start RPC | Start ms | process-created | control-pipe-connected | ready-received | Connect RPC | Connect ms |\n")
            .Append("| --- | --- | --- | --- | --- | --- | --- | --- | --- |\n");
        foreach (var attempt in report.Attempts)
        {
            string PhaseMs(string phase) =>
                attempt.Phases.FirstOrDefault(item => item.Phase == phase) is { } found ? found.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) : "-";
            var failed = attempt.Phases.FirstOrDefault(static item => item.Phase is "startup-bound-expired" or "start-failed");
            text.Append(CultureInfo.InvariantCulture,
                $"| {attempt.Ordinal} | {(attempt.Ordinal == 1 ? ConditionName(report.Condition) : "repeat")} | {RpcText(attempt.Start)} | {Ms(attempt.Start.ElapsedMilliseconds)} | ")
                .Append(CultureInfo.InvariantCulture,
                    $"{PhaseMs("process-created")} | {PhaseMs("control-pipe-connected")} | {(failed is null ? PhaseMs("ready-received") : $"{failed.Phase} at {failed.ElapsedMilliseconds}")} | ")
                .Append(CultureInfo.InvariantCulture,
                    $"{(attempt.Connect is null ? "-" : RpcText(attempt.Connect))} | {Ms(attempt.Connect?.ElapsedMilliseconds)} |\n");
        }

        if (report.Attempts.Count == 0)
        {
            text.Append("\nNo StartSpatialAnalyzerSdk was recorded in this log; nothing was measured.\n");
        }

        text.Append("\nPhase times are milliseconds since the worker start began; RPC times are from the server's acceptance to its exit record. ")
            .Append("These are observations of one bounded run on one machine, not vendor guarantees.\n");
        return text.ToString();
    }

    private static string RpcText(RpcTiming rpc) =>
        rpc.GrpcStatus is null ? rpc.Exit : $"{rpc.Exit} `{rpc.GrpcStatus}`{(rpc.DiagnosticCode is null ? string.Empty : $" `{rpc.DiagnosticCode}`")}";

    private static string Ms(long? value) => value is { } ms ? ms.ToString(CultureInfo.InvariantCulture) + " ms" : "-";

    private static void WriteRpc(Utf8JsonWriter json, string name, RpcTiming rpc)
    {
        json.WriteStartObject(name);
        json.WriteString("exit", rpc.Exit);
        if (rpc.GrpcStatus is { } status)
            json.WriteString("grpc_status", status);
        else
            json.WriteNull("grpc_status");
        if (rpc.DiagnosticCode is { } code)
            json.WriteString("diagnostic_code", code);
        else
            json.WriteNull("diagnostic_code");
        if (rpc.ElapsedMilliseconds is { } elapsed)
            json.WriteNumber("elapsed_ms", elapsed);
        else
            json.WriteNull("elapsed_ms");
        json.WriteEndObject();
    }

    public const string Usage = """
        Usage (offline; reads one server's log folder and connects to nothing):
          Briosa.LicensedProbes --startup-timing --condition fresh-install|after-reboot|warm
              --server-log-directory <the server's Briosa:Logging:File:Directory> --output-directory <empty dir>
              [--harness-revision <sha>]
        """;

    public static async Task<int> RunAsync(IReadOnlyList<string> arguments, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        StartupCondition condition;
        string logDirectory;
        string outputDirectory;
        string? revision;
        List<LogEvent> events = [];
        var unreadable = 0;
        try
        {
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

                if (argument is not ("--condition" or "--server-log-directory" or "--output-directory" or "--harness-revision"))
                    throw new ProbeUsageException("An unknown option was supplied.");
                if (index + 1 >= arguments.Count)
                    throw new ProbeUsageException($"Option '{argument}' needs a value.");
                if (!values.TryAdd(argument, arguments[++index]))
                    throw new ProbeUsageException($"Option '{argument}' is repeated.");
            }

            condition = values.GetValueOrDefault("--condition") switch
            {
                "fresh-install" => StartupCondition.FreshInstall,
                "after-reboot" => StartupCondition.AfterReboot,
                "warm" => StartupCondition.Warm,
                _ => throw new ProbeUsageException("--condition must be fresh-install, after-reboot or warm.")
            };
            logDirectory = values.GetValueOrDefault("--server-log-directory") ?? throw new ProbeUsageException("--server-log-directory is required.");
            outputDirectory = values.GetValueOrDefault("--output-directory") ?? throw new ProbeUsageException("--output-directory is required.");
            revision = values.GetValueOrDefault("--harness-revision");
            if (revision is not null && (revision.Length != 40 || !revision.All(char.IsAsciiHexDigitLower)))
                throw new ProbeUsageException("--harness-revision must be a 40-character lowercase commit SHA.");
            if (!Directory.Exists(logDirectory))
                throw new ProbeUsageException("--server-log-directory does not exist.");
            if (Directory.Exists(outputDirectory) && Directory.EnumerateFileSystemEntries(outputDirectory).Any())
                throw new ProbeUsageException("--output-directory must be new or empty; records are never overwritten.");

            foreach (var file in Directory.EnumerateFiles(logDirectory, "briosa-*.jsonl").Order(StringComparer.Ordinal))
            {
                // The server may still hold the file open; read it shared.
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
                {
                    if (ParseLine(line) is { } item)
                        events.Add(item);
                    else if (line.Length > 0)
                        unreadable++;
                }
            }

            var instances = events.Select(static item => item.Instance).Distinct(StringComparer.Ordinal).Count();
            if (instances != 1)
                throw new ProbeUsageException(string.Create(CultureInfo.InvariantCulture,
                    $"The log folder holds {instances} server instances; start each measured server with its own --Briosa:Logging:File:Directory."));
        }
        catch (ProbeUsageException exception)
        {
            await error.WriteLineAsync(exception.Message).ConfigureAwait(false);
            await error.WriteLineAsync(Usage).ConfigureAwait(false);
            return 2;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync("The server log could not be read.").ConfigureAwait(false);
            return 2;
        }

        var report = Analyze(condition, events, unreadable, revision);
        Directory.CreateDirectory(outputDirectory);
        var stem = Path.Combine(outputDirectory, $"startup-timing-{ConditionName(condition)}-{ProbeTarget.SpatialAnalyzerTarget}");
        foreach (var (path, content) in new[] { (stem + ".json", ToJson(report)), (stem + ".md", ToMarkdown(report)) })
        {
            var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            await using (stream.ConfigureAwait(false))
            {
                await stream.WriteAsync(new UTF8Encoding(false).GetBytes(content), cancellationToken).ConfigureAwait(false);
            }
        }

        var firstStart = report.Attempts.Count > 0 ? report.Attempts[0] : null;
        await output.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
            $"Startup timing {ConditionName(condition)}: {report.Attempts.Count} start attempts; first start {Ms(firstStart?.Start.ElapsedMilliseconds)} ({firstStart?.Start.Exit ?? "none"}).")).ConfigureAwait(false);
        await output.WriteLineAsync($"Records: {Path.GetFileName(stem)}.json, {Path.GetFileName(stem)}.md").ConfigureAwait(false);
        return report.Attempts.Count > 0 ? 0 : 1;
    }
}
