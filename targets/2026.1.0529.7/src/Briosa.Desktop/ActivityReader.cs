using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Briosa.Desktop;

public sealed record ActivityEntry(DateTimeOffset Time, string Level, string Category, string Message,
    string Correlation, string Operation, string Details);

public sealed class ActivityReader
{
    private readonly Dictionary<string, ActivityEntry> _entries = new(StringComparer.Ordinal);
    public IReadOnlyList<ActivityEntry> Entries => _entries.Values.OrderBy(entry => entry.Time).ToArray();
    public string Status { get; private set; } = "No activity observed yet.";

    public void ReadRecent(string? directory, string instance)
    {
        if (directory is null || !DesktopProtocol.IsInstance(instance))
        { Status = "File diagnostics are unavailable for this server."; return; }
        try
        {
            var files = Directory.EnumerateFiles(directory, "briosa-" + instance + "-*.jsonl")
                .OrderByDescending(path => path, StringComparer.Ordinal).Take(3).ToArray();
            foreach (var path in files)
            {
                using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var offset = Math.Max(0, file.Length - 256 * 1024);
                file.Seek(offset, SeekOrigin.Begin);
                var buffer = new byte[checked((int)Math.Min(256 * 1024, file.Length - offset))];
                var count = file.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
                var content = Encoding.UTF8.GetString(buffer, 0, count);
                var lines = content.Split('\n');
                // The first tail fragment and the last incomplete write are not records.
                for (int i = offset > 0 ? 1 : 0; i < lines.Length - 1; i++)
                {
                    if (lines[i].Length > 16 * 1024) continue;
                    var entry = Parse(lines[i], instance);
                    if (entry is not null) _entries[Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(lines[i])))] = entry;
                }
            }
            foreach (var key in _entries.OrderByDescending(pair => pair.Value.Time).Skip(2000).Select(pair => pair.Key).ToArray())
                _entries.Remove(key);
            Status = files.Length == 0 ? "No log file is available yet." : "Recent activity · best-effort diagnostics; records may be missing.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        { Status = "Activity files are temporarily unavailable. Server status is independent."; }
    }

    public static ActivityEntry? Parse(string json, string instance)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (json.Length > 16 * 1024) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var properties = root.GetProperty("Properties");
            if (properties.GetProperty("SchemaVersion").GetInt32() != 1 ||
                properties.GetProperty("ServerInstanceId").GetString() != instance ||
                properties.GetProperty("SpatialAnalyzerTarget").GetString() != DesktopProtocol.Target) return null;
            var name = root.GetProperty("MessageTemplate").GetString();
            var (category, message) = name switch
            {
                "ControlPlaneReady" => ("Server", "Server started."),
                "WorkerTransition" => ("SDK", "SDK worker state changed."),
                "ExecutionDispatched" => ("Execution", "Command dispatched."),
                "ExecutionResolved" => ("Execution", "Command execution observation recorded."),
                "LifecycleRejected" => ("SDK", "Lifecycle action rejected."),
                "ApplicationTransition" => ("SpatialAnalyzer", "Application state changed."),
                "LogSinkDegraded" => ("Logging", "Diagnostic delivery is degraded; records may be missing."),
                "PolicyLoaded" => ("Policy", "Operation policy loaded."),
                "RequestStarted" => ("Request", "Request started."),
                "PolicyAllowed" => ("Policy", "Operation admitted by policy."),
                "PolicyRejected" => ("Policy", "Operation denied by policy."),
                "RpcCompleted" => ("Request", "RPC completed. Check the execution observation for the MP outcome."),
                "RpcFailed" => ("Request", "RPC failed. This alone does not establish command completion."),
                "FrameworkEvent" => ("Host", "Host framework diagnostic recorded."),
                "ReadinessNotReady" => ("Readiness", "Commands are not ready. Review SDK connection and version evidence on Overview."),
                _ => ("", "")
            };
            if (message.Length == 0) return null;
            var level = root.GetProperty("Level").GetString();
            if (level is not ("Verbose" or "Debug" or "Information" or "Warning" or "Error" or "Fatal")) return null;
            string Value(string key) => properties.TryGetProperty(key, out var value) ? value.ToString() : "";
            var details = new List<string>();
            foreach (var key in new[] { "WorkerState", "ConnectionState", "ExecutionReadinessState", "ExecutionDisposition", "MpOutcome", "OutputRetrievalOutcome", "DiagnosticCode" })
            {
                var value = Value(key);
                if (value.Length > 0 && SafeText.Code(value) != "unavailable") details.Add(key + ": " + value);
            }
            foreach (var key in new[] { "Generation", "RestartCount", "MpResultCode", "RequestDurationMilliseconds", "SdkDurationMilliseconds", "DroppedRecords" })
                if (double.TryParse(Value(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number))
                    details.Add(key + ": " + number.ToString("G", CultureInfo.InvariantCulture));
            var correlation = Value("CorrelationId");
            if (!Guid.TryParse(correlation, out _)) correlation = "";
            var operation = Value("OperationId");
            if (operation.Length > 128 || !operation.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_')) operation = "";
            return new(root.GetProperty("Timestamp").GetDateTimeOffset().ToLocalTime(), level, category, message,
                correlation, operation, string.Join(" · ", details));
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException) { return null; }
    }
}
