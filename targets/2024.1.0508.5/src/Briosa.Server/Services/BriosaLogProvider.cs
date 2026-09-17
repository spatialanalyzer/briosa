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

internal sealed class BriosaLogHealth : IAsyncLogEventSinkMonitor, ILoggingFailureListener
{
    private IAsyncLogEventSinkInspector? _inspector;
    private long _failures;
    private long _discarded;
    private long _finalDropped;
    public long Failures => Interlocked.Read(ref _failures);
    public long Dropped => Interlocked.Read(ref _discarded) +
        (_inspector?.DroppedMessagesCount ?? Interlocked.Read(ref _finalDropped));
    public int Queued => _inspector?.Count ?? 0;
    public void Failed() => Interlocked.Increment(ref _failures);
    public void Discarded() => Interlocked.Increment(ref _discarded);
    public void StartMonitoring(IAsyncLogEventSinkInspector inspector) => _inspector = inspector;
    public void StopMonitoring(IAsyncLogEventSinkInspector inspector)
    {
        Interlocked.Exchange(ref _finalDropped, inspector.DroppedMessagesCount);
        _inspector = null;
    }
    public void OnLoggingFailed(object sender, LoggingFailureKind kind, string message,
        IReadOnlyCollection<LogEvent>? events, Exception? exception) => Failed();
}

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

internal sealed class SafeLogState : IReadOnlyList<KeyValuePair<string, object?>>
{
    private static readonly SearchValues<char> HexDigits = SearchValues.Create("0123456789abcdefABCDEF");
    private readonly List<KeyValuePair<string, object?>> _values;
    private SafeLogState(EventId eventId, List<KeyValuePair<string, object?>> values)
    {
        EventId = eventId;
        _values = values;
    }
    public EventId EventId { get; }
    public int Count => _values.Count;
    public KeyValuePair<string, object?> this[int index] => _values[index];
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public override string ToString() => EventId.Name + " " + string.Join(" ", _values.Where(pair => pair.Key != "{OriginalFormat}")
        .Select(pair => $"{pair.Key}={pair.Value}"));

    public static SafeLogState Create<TState>(string category, EventId eventId, TState state, string instanceId)
    {
        var trusted = category.StartsWith("Briosa.Server.", StringComparison.Ordinal) &&
            eventId.Id is 1000 or 1201 or 1300 or 1301 or 1302 or 1400 or 1401 or 1500 or 2000 or 2001 or 2002 or 2003 or 2004 or 2005;
        var name = trusted ? eventId.Id switch
        {
            1000 => "ControlPlaneReady",
            1201 => "WorkerTransition",
            1300 => "ExecutionDispatched",
            1301 => "ExecutionResolved",
            1302 => "LifecycleRejected",
            1400 => "ApplicationTransition",
            1401 => "LogSinkDegraded",
            1500 => "ReadinessNotReady",
            2000 => "PolicyLoaded",
            2001 => "RequestStarted",
            2002 => "PolicyAllowed",
            2003 => "PolicyRejected",
            2004 => "RpcCompleted",
            2005 => "RpcFailed",
            _ => "BriosaEvent"
        } : "FrameworkEvent";
        var values = new List<KeyValuePair<string, object?>>
        {
            new("ServerInstanceId", instanceId),
            new("SpatialAnalyzerTarget", SpatialAnalyzerApi.TargetVersion),
            new("{OriginalFormat}", name)
        };
        if (trusted && state is IEnumerable<KeyValuePair<string, object?>> properties)
        {
            foreach (var (key, value) in properties)
            {
                if (AllowedProperty(key) && (key == "PolicyFingerprint" ? SafeFingerprint(value) : SafeValue(value)))
                    values.Add(new(key, value));
            }
        }
        if (Activity.Current is { } activity)
        {
            values.Add(new("TraceId", activity.TraceId.ToHexString()));
            values.Add(new("SpanId", activity.SpanId.ToHexString()));
        }
        return new SafeLogState(new EventId(eventId.Id, name), values);
    }

    private static bool SafeValue(object? value) => value is null or bool or int or long or double or Guid or Enum ||
        value is string text && text.Length <= 256 &&
        text.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.' or ',' or '/');

    private static bool SafeFingerprint(object? value) => value is string text &&
        text.Length == 71 && text.StartsWith("sha256:", StringComparison.Ordinal) &&
        !text.AsSpan(7).ContainsAnyExcept(HexDigits);

    private static bool AllowedProperty(string name) => name is
        "CorrelationId" or "ActorCategory" or "Endpoint" or "OperationId" or "Effect" or
        "ExecutionScope" or "RiskFlags" or "PolicyDecision" or "AllowCount" or "DenyCount" or
        "TargetIsolationMode" or "PolicyFingerprint" or "Generation" or "RequestDurationMilliseconds" or
        "SdkDurationMilliseconds" or "ExecutionDisposition" or "MpOutcome" or "OutputRetrievalOutcome" or
        "MpResultCode" or "MpResultRetrieved" or "GrpcStatus" or "DiagnosticCode" or "ReplaySafety" or
        "WorkerState" or "RestartCount" or "Termination" or "ConnectionState" or "ExecutionReadinessState" or
        "StatusCode" or "ActivatedSdkIdentitySource" or "ActivatedSdkIdentityMatchState" or
        "ConnectedSaIdentitySource" or "ConnectedSaIdentityMatchState" or "AdmissionMilliseconds" or
        "QueueMilliseconds" or "ExchangeMilliseconds" or "Rpc" or "ApplicationState" or "Ownership" or
        "DroppedRecords" or "SinkFailures";
}
