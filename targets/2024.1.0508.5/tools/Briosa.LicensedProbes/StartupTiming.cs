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
/// (1302 to 1305). It connects to nothing and adds no timing of its own. Only records of this
/// exact SpatialAnalyzer target are read, every JSON value's kind is checked, and only event
/// numbers, closed-vocabulary phases, RPC names and statuses, listed diagnostic codes, integers
/// and timestamps are kept. A line that is not such a record, including one whose known field
/// has the wrong JSON kind, is counted, never thrown on. An attempt is labelled with the
/// measured condition only when the log proves it was the server's first start. The log names
/// no request, so lifecycle requests are followed as outstanding until their exit: an attempt
/// that overlapped any other request has neither its RPC timing nor its phases reported.
/// </summary>
internal static class StartupTiming
{
    public const string Option = "--startup-timing";

    public const string StartRpc = "StartSpatialAnalyzerSdk";
    public const string ConnectRpc = "ConnectToSpatialAnalyzer";

    // Correlation of an attempt with its phases and its Connect.
    public const string Unambiguous = "unambiguous";
    public const string OverlappingRequests = "overlapping-requests";
    public const string SeveralGenerations = "several-generations";
    public const string NoExitRecorded = "no-exit-recorded";

    // The RPC timing of an attempt that overlapped another request: which exit was its own is unknown.
    public const string WithheldExit = "withheld-overlapping";

    private static readonly string[] StringFields = ["Phase", "Rpc", "GrpcStatus", "DiagnosticCode"];
    private static readonly string[] IntegerFields = ["ElapsedMilliseconds", "Generation"];

    private static readonly HashSet<string> Phases = new(StringComparer.Ordinal)
    {
        "process-created", "control-pipe-connected", "ready-received", "startup-bound-expired", "start-failed"
    };

    // Every state-changing lifecycle RPC, so that a request overlapping an attempt is seen.
    private static readonly HashSet<string> Rpcs = new(StringComparer.Ordinal)
    {
        StartRpc, ConnectRpc, "ReconnectToSpatialAnalyzer", "StopSpatialAnalyzerSdk", "RecoverSpatialAnalyzerSdk",
        "LaunchSpatialAnalyzer", "CloseOwnedSpatialAnalyzer"
    };

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

    internal enum LineKind
    {
        Event,
        Unreadable,
        OtherTarget
    }

    /// <summary>One log record the analysis uses.</summary>
    internal sealed record LogEvent(DateTimeOffset Timestamp, int EventId, string Instance, IReadOnlyDictionary<string, string> Fields);

    internal sealed record PhaseTiming(string Phase, long ElapsedMilliseconds);

    internal sealed record RpcTiming(string Exit, string? GrpcStatus, string? DiagnosticCode, long? ElapsedMilliseconds);

    internal sealed record StartAttempt(int Ordinal, string Label, string Correlation, int? Generation, RpcTiming Start, IReadOnlyList<PhaseTiming> Phases, RpcTiming? Connect);

    internal sealed record Report(
        StartupCondition Condition,
        TimeSpan ConfiguredStartupTimeout,
        long? EndpointListeningMilliseconds,
        IReadOnlyList<StartAttempt> Attempts,
        int UnreadableLines,
        string? HarnessRevision)
    {
        /// <summary>A start under a startup bound other than the product default is a changed configuration, not a stock start.</summary>
        public bool StockConfiguration => ConfiguredStartupTimeout == WorkerProcessOptions.DefaultStartupTimeout;

        public string ConfigurationLabel => StockConfiguration
            ? "stock"
            : string.Create(CultureInfo.InvariantCulture,
                $"changed configuration: worker startup bound {ConfiguredStartupTimeout.TotalSeconds:0} s, not a stock start");
    }

    /// <summary>Parses one JSON log line, never throwing.</summary>
    internal static (LineKind Kind, LogEvent? Event) ParseLine(string line)
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
                !eventId.TryGetProperty("Id", out var id) || id.ValueKind != JsonValueKind.Number || !id.TryGetInt32(out var number) ||
                !properties.TryGetProperty("ServerInstanceId", out var instance) || instance.ValueKind != JsonValueKind.String ||
                instance.GetString() is not { Length: 32 } instanceId || !instanceId.All(char.IsAsciiHexDigitLower) ||
                !properties.TryGetProperty("SpatialAnalyzerTarget", out var target) || target.ValueKind != JsonValueKind.String)
            {
                return (LineKind.Unreadable, null);
            }

            if (!string.Equals(target.GetString(), ProbeTarget.SpatialAnalyzerTarget, StringComparison.Ordinal))
            {
                return (LineKind.OtherTarget, null);
            }

            // A known field of the wrong kind makes the whole line unusable: it is counted, and
            // never read as an event with that field missing.
            var fields = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var name in StringFields)
            {
                if (!properties.TryGetProperty(name, out var value))
                {
                    continue;
                }

                if (value.ValueKind != JsonValueKind.String)
                {
                    return (LineKind.Unreadable, null);
                }

                if (Accept(name, value.GetString()!) is { } accepted)
                {
                    fields[name] = accepted;
                }
            }

            foreach (var name in IntegerFields)
            {
                if (!properties.TryGetProperty(name, out var value))
                {
                    continue;
                }

                if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var integer))
                {
                    return (LineKind.Unreadable, null);
                }

                fields[name] = integer.ToString(CultureInfo.InvariantCulture);
            }

            return (LineKind.Event, new LogEvent(at.ToUniversalTime(), number, instanceId, fields));
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException or ArgumentException)
        {
            return (LineKind.Unreadable, null);
        }
    }

    private static string? Accept(string name, string text) => name switch
    {
        "Phase" => Phases.Contains(text) ? text : null,
        "Rpc" => Rpcs.Contains(text) ? text : null,
        "GrpcStatus" => text.All(char.IsAsciiLetter) && Enum.TryParse<Grpc.Core.StatusCode>(text, ignoreCase: false, out _) ? text : null,
        "DiagnosticCode" => ExternalValues.DiagnosticCode(text),
        _ => null
    };

    /// <summary>One lifecycle request as the log shows it: its arrival, the exit paired with it, and whether it overlapped another.</summary>
    private sealed class Request(LogEvent arrival, string? rpc)
    {
        public LogEvent Arrival { get; } = arrival;

        // Null when the arrival named no listed RPC: such a request never exits, so it overlaps
        // everything after it.
        public string? Rpc { get; } = rpc;

        public LogEvent? Exit { get; set; }

        public bool Overlapped { get; set; }
    }

    // Walks the log in order, keeping every request outstanding from its arrival to the first
    // later exit of the same RPC. A request that arrives while another is outstanding overlaps
    // it, and both are marked: the log cannot say which exit and which phases are whose.
    private static List<Request> Requests(IReadOnlyList<LogEvent> ordered)
    {
        var requests = new List<Request>();
        var outstanding = new List<Request>();
        foreach (var item in ordered)
        {
            if (item.EventId == 1303)
            {
                var request = new Request(item, Field(item, "Rpc"));
                if (outstanding.Count > 0)
                {
                    request.Overlapped = true;
                    outstanding.ForEach(static other => other.Overlapped = true);
                }

                outstanding.Add(request);
                requests.Add(request);
            }
            else if (item.EventId is 1302 or 1304 or 1305 && Field(item, "Rpc") is { } rpc &&
                outstanding.Find(request => request.Rpc == rpc) is { } open)
            {
                open.Exit = item;
                outstanding.Remove(open);
            }
        }

        return requests;
    }

    /// <summary>
    /// Builds the report from one server instance's events. Each StartSpatialAnalyzerSdk is an
    /// attempt, in order. Its RPC timing and phases are reported only when no other lifecycle
    /// request was outstanding at any time while it ran, and its phases only when they name one
    /// worker generation; its Connect is the next ConnectToSpatialAnalyzer, likewise only when
    /// nothing overlapped it.
    /// </summary>
    internal static Report Analyze(StartupCondition condition, TimeSpan configuredStartupTimeout, IReadOnlyList<LogEvent> events,
        int unreadableLines, string? harnessRevision)
    {
        ArgumentNullException.ThrowIfNull(events);
        var ordered = events.OrderBy(static item => item.Timestamp).ToList();
        var listeningEvent = ordered.FirstOrDefault(static item => item.EventId == 1001);
        long? listening = listeningEvent is not null && Long(listeningEvent, "ElapsedMilliseconds") is { } ms ? ms : null;

        var requests = Requests(ordered);
        var starts = requests.Where(static request => request.Rpc == StartRpc).ToList();
        var attempts = new List<StartAttempt>();
        for (var index = 0; index < starts.Count; index++)
        {
            var request = starts[index];
            var arrival = request.Arrival;
            var next = index + 1 < starts.Count ? starts[index + 1].Arrival.Timestamp : DateTimeOffset.MaxValue;
            var exit = request.Overlapped ? null : request.Exit;
            var phases = exit is null
                ? []
                : ordered
                    .Where(item => item.EventId == 1203 && item.Timestamp >= arrival.Timestamp && item.Timestamp <= exit.Timestamp && Field(item, "Phase") is not null)
                    .Select(static item => (Phase: Field(item, "Phase")!, Elapsed: Long(item, "ElapsedMilliseconds"), Generation: Long(item, "Generation")))
                    .Where(static item => item.Elapsed is >= 0)
                    .ToList();
            var generations = phases.Select(static item => item.Generation).Distinct().ToList();
            var correlation = request.Overlapped ? OverlappingRequests :
                exit is null ? NoExitRecorded :
                generations.Count > 1 || generations.Contains(null) ? SeveralGenerations :
                Unambiguous;
            int? generation = correlation == Unambiguous && generations.Count == 1 ? (int?)generations[0] : null;

            // The measured condition needs proof that this is the server's very first start: the
            // server's own listening record before it, nothing lifecycle-related earlier, and the
            // first worker generation.
            var first = index == 0 && correlation == Unambiguous && listeningEvent is not null &&
                listeningEvent.Timestamp <= arrival.Timestamp && generation == 1 &&
                !ordered.Any(item => item.Timestamp < arrival.Timestamp && item.EventId is 1203 or 1303 or 1304 or 1302 or 1305);
            var label = index == 0
                ? first ? ConditionName(condition) : "first-attempt-unproven"
                : correlation == Unambiguous ? "repeat" : "ambiguous";

            RpcTiming? connect = null;
            if (exit is not null &&
                requests.Find(item => item.Rpc == ConnectRpc && item.Arrival.Timestamp >= exit.Timestamp && item.Arrival.Timestamp < next) is
                    { Overlapped: false, Exit: { } connectExit })
            {
                connect = Timing(connectExit);
            }

            var start = request.Overlapped ? new RpcTiming(WithheldExit, null, null, null) :
                exit is null ? new RpcTiming(NoExitRecorded, null, null, null) : Timing(exit);
            attempts.Add(new StartAttempt(index + 1, label, correlation, generation, start,
                correlation == Unambiguous ? [.. phases.Select(static item => new PhaseTiming(item.Phase, item.Elapsed!.Value))] : [],
                connect));
        }

        return new Report(condition, configuredStartupTimeout, listening, attempts, unreadableLines, harnessRevision);
    }

    private static RpcTiming Timing(LogEvent exit) => new(
        exit.EventId switch { 1304 => "completed", 1302 => "rejected", _ => "faulted" },
        Field(exit, "GrpcStatus"), Field(exit, "DiagnosticCode"),
        Long(exit, "ElapsedMilliseconds") is { } elapsed && elapsed >= 0 ? elapsed : null);

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
            json.WriteNumber("schema_version", 2);
            json.WriteString("issue", ProbeTarget.Session4Issue);
            json.WriteString("spatial_analyzer_target", ProbeTarget.SpatialAnalyzerTarget);
            json.WriteString("condition", ConditionName(report.Condition));
            json.WriteString("configuration", report.ConfigurationLabel);
            json.WriteBoolean("stock_configuration", report.StockConfiguration);
            json.WriteNumber("configured_worker_startup_timeout_ms", (long)report.ConfiguredStartupTimeout.TotalMilliseconds);
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
                json.WriteString("label", attempt.Label);
                json.WriteString("correlation", attempt.Correlation);
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
        text.Append(CultureInfo.InvariantCulture, $"**Configuration: {report.ConfigurationLabel}.** ")
            .Append(CultureInfo.InvariantCulture,
                $"The product's default worker startup bound is {(long)WorkerProcessOptions.DefaultStartupTimeout.TotalMilliseconds} ms.\n\n")
            .Append(CultureInfo.InvariantCulture,
                $"From the server's own log events (EndpointListening, WorkerStartPhase, lifecycle RPC records). Endpoint listening: {Ms(report.EndpointListeningMilliseconds)} after process start. ")
            .Append(CultureInfo.InvariantCulture, $"Lines that were not usable records of this target: {report.UnreadableLines}.\n\n");
        text.Append("| Attempt | Label | Correlation | Start RPC | Start ms | process-created | control-pipe-connected | ready-received | Connect RPC | Connect ms |\n")
            .Append("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |\n");
        foreach (var attempt in report.Attempts)
        {
            string PhaseMs(string phase) =>
                attempt.Phases.FirstOrDefault(item => item.Phase == phase) is { } found ? found.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) : "-";
            var failed = attempt.Phases.FirstOrDefault(static item => item.Phase is "startup-bound-expired" or "start-failed");
            text.Append(CultureInfo.InvariantCulture,
                $"| {attempt.Ordinal} | {attempt.Label} | {attempt.Correlation} | {RpcText(attempt.Start)} | {Ms(attempt.Start.ElapsedMilliseconds)} | ")
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
            .Append("An attempt whose label is not the condition did not prove it was the server's first start, an attempt that is not ")
            .Append("`unambiguous` has no phases attributed to it, and an attempt that overlapped another request has no RPC timing either. ")
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
              --startup-timeout-seconds <the server's Briosa:Worker:StartupTimeout, 1-300>
              --server-log-directory <the server's Briosa:Logging:File:Directory> --output-directory <empty dir>
              [--harness-revision <sha>]
        """;

    public static async Task<int> RunAsync(IReadOnlyList<string> arguments, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        StartupCondition condition;
        TimeSpan startupTimeout;
        string logDirectory;
        string outputDirectory;
        string? revision;
        List<LogEvent> events = [];
        var unreadable = 0;
        var otherTarget = 0;
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

                if (argument is not ("--condition" or "--server-log-directory" or "--output-directory" or "--harness-revision" or "--startup-timeout-seconds"))
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
            startupTimeout = values.GetValueOrDefault("--startup-timeout-seconds") is { } seconds &&
                int.TryParse(seconds, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value is >= 1 and <= 300
                    ? TimeSpan.FromSeconds(value)
                    : throw new ProbeUsageException("--startup-timeout-seconds must give the server's worker startup bound, 1 to 300.");
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
                    if (line.Length == 0)
                    {
                        continue;
                    }

                    var (kind, item) = ParseLine(line);
                    switch (kind)
                    {
                        case LineKind.Event:
                            events.Add(item!);
                            break;
                        case LineKind.OtherTarget:
                            otherTarget++;
                            break;
                        default:
                            unreadable++;
                            break;
                    }
                }
            }

            if (otherTarget > 0)
                throw new ProbeUsageException(string.Create(CultureInfo.InvariantCulture,
                    $"The log folder holds {otherTarget} records of another SpatialAnalyzer target; measure each target with its own harness and its own log folder."));
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

        var report = Analyze(condition, startupTimeout, events, unreadable, revision);
        Directory.CreateDirectory(outputDirectory);
        var stem = Path.Combine(outputDirectory, $"startup-timing-{ConditionName(condition)}-{ProbeTarget.SpatialAnalyzerTarget}");
        foreach (var (path, content) in new[] { (stem + ".json", ToJson(report)), (stem + ".md", ToMarkdown(report)) })
        {
            var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            await using (stream.ConfigureAwait(false))
            {
                await stream.WriteAsync(new UTF8Encoding(false).GetBytes(content), CancellationToken.None).ConfigureAwait(false);
            }
        }

        var firstStart = report.Attempts.Count > 0 ? report.Attempts[0] : null;
        await output.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
            $"Startup timing {ConditionName(condition)} ({report.ConfigurationLabel}): {report.Attempts.Count} start attempts; " +
            $"first {firstStart?.Label ?? "none"}, {Ms(firstStart?.Start.ElapsedMilliseconds)} ({firstStart?.Start.Exit ?? "none"}).")).ConfigureAwait(false);
        await output.WriteLineAsync($"Records: {Path.GetFileName(stem)}.json, {Path.GetFileName(stem)}.md").ConfigureAwait(false);
        return report.Attempts.Count > 0 ? 0 : 1;
    }
}
