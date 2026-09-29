using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Sinks.File;

namespace Briosa.Server.Services;

/// <summary>
/// Invoked only by the async sink. Serializes retention/quota decisions between
/// local processes without putting filesystem work on request or SDK threads.
/// </summary>
internal sealed class BriosaRetainedLogSink(
    BriosaLoggingOptions options, string instanceId, BriosaLogHealth health) : ILogEventSink, IDisposable
{
    internal const int MaximumRecordBytes = 16 * 1024;
    private Logger? _logger;
    private readonly RotationObserver _rotation = new();

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The acquired ownership stream is disposed by the using block after the catch.")]
    public void Emit(LogEvent logEvent)
    {
        Directory.CreateDirectory(options.Directory);
        // No waiting for another process. Contention is a best-effort log drop.
        FileStream ownership;
        try
        {
            ownership = new FileStream(Path.Combine(options.Directory, ".retention.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            health.Discarded();
            return;
        }
        using (ownership)
        {
            if (!PruneAndReserve())
            {
                // A one-file budget can fill before Serilog gets another write
                // to trigger rotation. Close our file so retention may reclaim
                // it; never close or delete another instance's active file.
                _logger?.Dispose();
                _logger = null;
                if (!PruneAndReserve())
                {
                    health.Discarded();
                    return;
                }
            }
            _rotation.OpenedFile = false;
            _logger ??= new LoggerConfiguration().MinimumLevel.Verbose()
                .WriteTo.Fallible(sinks => sinks.File(
                    new BoundedJsonFormatter(),
                    Path.Combine(options.Directory, $"briosa-{instanceId}-.jsonl"),
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: options.MaxFileSizeMiB * 1024L * 1024 - MaximumRecordBytes,
                    retainedFileCountLimit: options.RetainedFileCount,
                    retainedFileTimeLimit: TimeSpan.FromDays(options.MaxAgeDays),
                    hooks: _rotation,
                    // Flush to the OS cache before releasing the quota lock so
                    // other instances see current length. No per-event durable fsync.
                    buffered: false), health)
                .CreateLogger();
            _logger.Write(logEvent);
            // The pre-write scan reserved a complete bounded record under the
            // shared lock. Only opening/rotating a file can change the file count
            // or release an old active file that now needs to be reclaimed.
            if (_rotation.OpenedFile) PruneAndReserve();
        }
    }

    private bool PruneAndReserve()
    {
        var files = new DirectoryInfo(options.Directory).EnumerateFiles("briosa-*.jsonl")
            .Where(file => IsLogFile(file.Name) && (file.Attributes & FileAttributes.ReparsePoint) == 0)
            .OrderBy(file => file.LastWriteTimeUtc).ToArray();
        var total = files.Sum(file => file.Length);
        var remaining = files.Length;
        var cutoff = DateTime.UtcNow.AddDays(-options.MaxAgeDays);
        var budget = options.MaxTotalSizeMiB * 1024L * 1024;
        foreach (var file in files)
        {
            if (file.LastWriteTimeUtc >= cutoff &&
                total + MaximumRecordBytes <= budget &&
                remaining < options.RetainedFileCount) continue;
            try
            {
                // DeleteOnClose + exclusive open cannot delete an active Serilog
                // file. Closed files from earlier server instances are eligible.
                using var expired = new FileStream(file.FullName, FileMode.Open,
                    FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
                total -= file.Length;
                remaining--;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { health.Failed(); }
        }
        // Active instances may occupy every retained-file slot. Refuse a new
        // instance rather than exceeding the shared directory count/size bound.
        var alreadyOpen = _logger is not null;
        return total + MaximumRecordBytes <= budget &&
            (alreadyOpen || remaining < options.RetainedFileCount);
    }

    internal static bool IsLogFile(string name) =>
        name.Length > 48 && name.StartsWith("briosa-", StringComparison.Ordinal) &&
        Guid.TryParseExact(name.AsSpan(7, 32), "N", out _) && name[39] == '-' &&
        name.EndsWith(".jsonl", StringComparison.Ordinal);

    public void Dispose() => _logger?.Dispose();

    private sealed class RotationObserver : FileLifecycleHooks
    {
        public bool OpenedFile { get; set; }

        public override Stream OnFileOpened(string path, Stream underlyingStream, Encoding encoding)
        {
            OpenedFile = true;
            return underlyingStream;
        }
    }

    private sealed class BoundedJsonFormatter : ITextFormatter
    {
        private readonly JsonFormatter _formatter = new(renderMessage: false);
        public void Format(LogEvent logEvent, TextWriter output)
        {
            using var buffer = new StringWriter(CultureInfo.InvariantCulture);
            var utcEvent = new LogEvent(logEvent.Timestamp.ToUniversalTime(), logEvent.Level,
                null, logEvent.MessageTemplate,
                logEvent.Properties.Select(property => new LogEventProperty(property.Key, property.Value)));
            _formatter.Format(utcEvent, buffer);
            var record = buffer.ToString();
            if (Encoding.UTF8.GetByteCount(record) > MaximumRecordBytes)
            {
                output.WriteLine("{\"Level\":\"Warning\",\"MessageTemplate\":\"LogRecordTooLarge\",\"Properties\":{\"SchemaVersion\":1}}");
                return;
            }
            output.Write(record);
        }
    }
}
