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
