using Briosa.Server.Generated.Sa.V2026_1_0529_7.V1Alpha1;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Briosa.Server.Tests;

public sealed class OperationAuditLoggerTests
{
    private const string SensitivePath = @"C:\Customers\Secret\geometry.xit";

    [Fact]
    public void CompletionEventsContainCorrelationAndOutcomesButNeverReturnedValues()
    {
        var sink = new CapturingLogger();
        var audit = new OperationAuditLogger(sink);
        var correlationId = Guid.NewGuid();
        var operation = Assert.Single(TargetCatalogMetadata.Operations);
        var outcome = new WorkerExecutionOutcome(
            WorkerExecutionStatus.Completed,
            new WorkerMpExecutionResult(
                ExecuteStepReturned: true,
                MpSucceeded: true,
                MpResultCode: 0,
                DurationMilliseconds: 12,
                OutputValues:
                [
                    new WorkerMpOutputValue(
                        "Directory",
                        WorkerMpValueKind.Text,
                        Retrieved: true,
                        StringValue: SensitivePath)
                ],
                DiagnosticCode: null),
            Connection: null,
            DiagnosticCode: "completed",
            Generation: 4,
            correlationId);

        audit.RequestStarted(correlationId, operation, "local-unauthenticated");
        audit.OperationCompleted(
            correlationId,
            operation.OperationId,
            outcome.Generation,
            requestDurationMilliseconds: 18,
            OperationAuditSummary.Create(outcome));

        var start = Assert.Single(sink.Entries, entry => entry.EventId == 2001);
        var completed = Assert.Single(sink.Entries, entry => entry.EventId == 2004);
        Assert.Contains(correlationId.ToString(), start.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("local-unauthenticated", start.Message, StringComparison.Ordinal);
        Assert.Contains(operation.FullyQualifiedMethod, start.Message, StringComparison.Ordinal);
        Assert.Contains("succeeded", completed.Message, StringComparison.Ordinal);
        Assert.Contains("retrieved", completed.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(SensitivePath, sink.AllText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DebugAndTraceLoggingDoNotEnableArgumentOrResultLogging()
    {
        var sink = new CapturingLogger();
        var audit = new OperationAuditLogger(sink);
        var outcome = new WorkerExecutionOutcome(
            WorkerExecutionStatus.Completed,
            new WorkerMpExecutionResult(
                ExecuteStepReturned: true,
                MpSucceeded: false,
                MpResultCode: 42,
                DurationMilliseconds: 9,
                OutputValues:
                [
                    new WorkerMpOutputValue(
                        "Directory",
                        WorkerMpValueKind.Text,
                        Retrieved: false,
                        StringValue: SensitivePath)
                ],
                DiagnosticCode: "mp-command-failed"),
            Connection: null,
            DiagnosticCode: "mp-command-failed",
            Generation: 2);

        audit.OperationFailed(
            Guid.NewGuid(),
            "file_operations.get_working_directory",
            outcome.Generation,
            requestDurationMilliseconds: 10,
            OperationAuditSummary.Create(outcome),
            StatusCode.FailedPrecondition,
            outcome.DiagnosticCode);

        Assert.Contains(sink.Entries, entry => entry.EventId == 2005);
        Assert.DoesNotContain(SensitivePath, sink.AllText, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CapturingLogger : ILogger<OperationAuditLogger>
    {
        private readonly List<LogEntry> _entries = [];

        public IReadOnlyList<LogEntry> Entries => _entries;

        public string AllText => string.Join(Environment.NewLine, _entries.Select(entry => entry.Message));

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            _entries.Add(new LogEntry(eventId.Id, formatter(state, exception)));
    }

    private sealed record LogEntry(int EventId, string Message);
}
