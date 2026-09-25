using System.Collections;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Briosa.Server.Operations;
using Microsoft.Extensions.Logging.Console;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.Async;

namespace Briosa.Server.Services;

/// <summary>Discards framework text, exceptions and scopes before either sink.</summary>
internal sealed class BriosaLogProvider : ILoggerProvider
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed",
        Justification = "Disposed once by the bounded background drain in Dispose.")]
    private readonly ILoggerFactory? _console;
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed",
        Justification = "Disposed once by the bounded background drain in Dispose.")]
    private readonly SerilogLoggerProvider? _file;
    private readonly BriosaLogHealth _health;
    private readonly int _shutdownTimeout;
    private readonly int _normalQueueLimit;
    private int _disposed;
    public string InstanceId { get; } = Guid.NewGuid().ToString("N");

    public BriosaLogProvider(BriosaLoggingOptions options, BriosaLogHealth health)
        : this(options, health, null) { }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "Serilog Async takes ownership of the contained sink and disposes it after draining.")]
    internal BriosaLogProvider(BriosaLoggingOptions options, BriosaLogHealth health, ILogEventSink? testSink)
    {
        options.Validate();
        _health = health;
        _shutdownTimeout = options.ShutdownTimeoutMilliseconds;
        _normalQueueLimit = options.QueueCapacity - Math.Max(1, options.QueueCapacity / 8);
        if (options.ConsoleEnabled)
        {
            _console = LoggerFactory.Create(builder => builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddSimpleConsole(format =>
                {
                    format.SingleLine = true;
                    format.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
                    format.UseUtcTimestamp = true;
                    format.IncludeScopes = false;
                })
                .AddConsole(console =>
                {
                    console.QueueFullMode = ConsoleLoggerQueueFullMode.DropWrite;
                    console.MaxQueueLength = options.QueueCapacity;
                }));
        }
        if (options.FileEnabled)
        {
            var sink = testSink ?? new BriosaRetainedLogSink(options, InstanceId, health);
            var logger = new LoggerConfiguration().MinimumLevel.Verbose()
                .Enrich.WithProperty("SchemaVersion", 1)
                .Enrich.WithProperty("ServerInstanceId", InstanceId)
                .Enrich.WithProperty("SpatialAnalyzerTarget", SpatialAnalyzerApi.TargetVersion)
                .WriteTo.Async(sinks => sinks.Sink(new ContainedSink(sink, health)), options.QueueCapacity,
                    blockWhenFull: false, monitor: health)
                .CreateLogger();
            _file = new SerilogLoggerProvider(logger, dispose: true);
        }
    }

    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
    {
        var category = categoryName.Length <= 256 &&
            categoryName.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '`')
                ? categoryName : "External";
        return new MetadataLogger(category, this, _file?.CreateLogger(category), _console?.CreateLogger(category));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Logging failures must not replace execution outcomes; exception text is discarded.")]
    private void Emit<TState>(string category, LogLevel level, EventId eventId, TState state,
        Exception? exception, Microsoft.Extensions.Logging.ILogger? file, Microsoft.Extensions.Logging.ILogger? console)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        try
        {
            var readinessReport = IsExpectedReadinessReport(category, eventId, state, exception);
            var safe = readinessReport ? SafeLogState.Create("Briosa.Server.Readiness", new EventId(1500), Array.Empty<KeyValuePair<string, object?>>(), InstanceId)
                : SafeLogState.Create(category, eventId, state, InstanceId);
            if (readinessReport) level = LogLevel.Information;
            try
            {
                if (file is not null && level < LogLevel.Warning && _health.Queued >= _normalQueueLimit)
                    _health.Discarded();
                else
                    file?.Log(level, safe.EventId, safe, null, static (value, _) => value.ToString());
            }
            catch (Exception) { _health.Failed(); }
            try { console?.Log(level, safe.EventId, safe, null, static (value, _) => value.ToString()); }
            catch (Exception) { _health.Failed(); }
        }
        catch (Exception) { _health.Failed(); }
    }

    private static bool IsExpectedReadinessReport<TState>(string category, EventId eventId, TState state, Exception? exception) =>
        exception is null && category == "Microsoft.Extensions.Diagnostics.HealthChecks.DefaultHealthCheckService" &&
        eventId.Id == 103 && eventId.Name == "HealthCheckEnd" && state is IEnumerable<KeyValuePair<string, object?>> values &&
        values.Any(pair => pair.Key == "HealthCheckName" && pair.Value is WorkerReadinessHealthCheck.ReadinessServiceName) &&
        values.Any(pair => pair.Key == "HealthStatus" && pair.Value is Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy);

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Shutdown is bounded best effort, including failed or stalled providers.")]
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        // Async.Dispose drains synchronously. One background disposal keeps a
        // stalled filesystem from indefinitely holding host shutdown.
        var drain = Task.Run(() =>
        {
            try { _file?.Dispose(); } catch (Exception) { _health.Failed(); }
            try { _console?.Dispose(); } catch (Exception) { _health.Failed(); }
        });
        if (!drain.Wait(_shutdownTimeout)) _health.Failed();
    }

    private sealed class MetadataLogger(string category, BriosaLogProvider owner,
        Microsoft.Extensions.Logging.ILogger? file, Microsoft.Extensions.Logging.ILogger? console)
        : Microsoft.Extensions.Logging.ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && (file is not null || console is not null);
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) =>
            owner.Emit(category, logLevel, eventId, state, exception, file, console);
    }

    private sealed class ContainedSink(ILogEventSink sink, BriosaLogHealth health) : ILogEventSink, IDisposable
    {
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "A background sink failure is recorded without exposing its exception.")]
        public void Emit(LogEvent logEvent)
        {
            try { sink.Emit(logEvent); }
            catch (Exception) { health.Failed(); }
        }
        public void Dispose() => (sink as IDisposable)?.Dispose();
    }
}
