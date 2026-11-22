using System.ComponentModel;
using System.Diagnostics;
using Grpc.Core;

namespace Briosa.Server.Services;

// Records lifecycle RPC arrival and every exit with its elapsed time, so a log tells
// a call that never reached the server from one that is still running in it.
internal sealed partial class LifecycleAuditLogger(ILogger<LifecycleAuditLogger> logger)
{
    private readonly ILogger<LifecycleAuditLogger> _logger = logger;

    // Measured from process creation, so it also covers runtime and host start-up.
    // A client's own connect delay is the time from here to its first lifecycle RPC.
    public void EndpointListening()
    {
        if (!_logger.IsEnabled(LogLevel.Information)) return;
        try
        {
            using var self = Process.GetCurrentProcess();
            var elapsed = Math.Max(0L, (long)(DateTime.Now - self.StartTime).TotalMilliseconds);
            LogEndpointListening(elapsed);
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            // Diagnostics only: an unreadable start time never affects the host.
        }
    }

    public void Started(string rpc) => LogStarted(rpc);

    public void Completed(string rpc, TimeProvider timeProvider, long startTimestamp)
    {
        var elapsed = ElapsedMilliseconds(timeProvider, startTimestamp);
        LogCompleted(rpc, StatusCode.OK, elapsed);
    }

    public void Rejected(string rpc, StatusCode grpcStatus, string diagnosticCode,
        TimeProvider timeProvider, long startTimestamp)
    {
        var elapsed = ElapsedMilliseconds(timeProvider, startTimestamp);
        LogRejected(rpc, grpcStatus, diagnosticCode, elapsed);
    }

    // Neither a response nor a typed lifecycle error. The exception is not logged.
    public void Faulted(string rpc, TimeProvider timeProvider, long startTimestamp)
    {
        var elapsed = ElapsedMilliseconds(timeProvider, startTimestamp);
        LogFaulted(rpc, elapsed);
    }

    private static long ElapsedMilliseconds(TimeProvider timeProvider, long startTimestamp) =>
        (long)timeProvider.GetElapsedTime(startTimestamp).TotalMilliseconds;

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Briosa public endpoint is listening {ElapsedMilliseconds} ms after process start.")]
    private partial void LogEndpointListening(long elapsedMilliseconds);

    [LoggerMessage(EventId = 1302, Level = LogLevel.Warning,
        Message = "Lifecycle RPC {Rpc} rejected with gRPC status {GrpcStatus}, diagnostic {DiagnosticCode}, after {ElapsedMilliseconds} ms.")]
    private partial void LogRejected(string rpc, StatusCode grpcStatus, string diagnosticCode,
        long elapsedMilliseconds);

    [LoggerMessage(EventId = 1303, Level = LogLevel.Information,
        Message = "Lifecycle RPC {Rpc} started.")]
    private partial void LogStarted(string rpc);

    [LoggerMessage(EventId = 1304, Level = LogLevel.Information,
        Message = "Lifecycle RPC {Rpc} completed with gRPC status {GrpcStatus} after {ElapsedMilliseconds} ms.")]
    private partial void LogCompleted(string rpc, StatusCode grpcStatus, long elapsedMilliseconds);

    [LoggerMessage(EventId = 1305, Level = LogLevel.Warning,
        Message = "Lifecycle RPC {Rpc} ended without a typed outcome after {ElapsedMilliseconds} ms.")]
    private partial void LogFaulted(string rpc, long elapsedMilliseconds);
}
