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
        var operation = WorkingDirectoryOperation();
        var outcome = new WorkerExecutionOutcome(
            WorkerExecutionStatus.Completed,
            WorkerExecutionDisposition.Completed,
            new WorkerMpExecutionResult(
                ExecuteStepReturned: true,
                MpResultRetrieved: true,
                MpSucceeded: true,
                MpResultCode: 2,
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
        Assert.Contains("GlobalStateRead", start.Message, StringComparison.Ordinal);
        Assert.Contains("completed", completed.Message, StringComparison.Ordinal);
        Assert.Contains("succeeded", completed.Message, StringComparison.Ordinal);
        Assert.Contains("retrieved", completed.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(SensitivePath, sink.AllText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AmbiguousTransportFailureIsNotLoggedAsNotStarted()
    {
        var summary = OperationAuditSummary.Create(new WorkerExecutionOutcome(
            WorkerExecutionStatus.WorkerFailure,
            WorkerExecutionDisposition.StartedOutcomeUnknown,
            Execution: null,
            Connection: null,
            "worker-execution-control-failed",
            Generation: 5));

        Assert.Equal("started_outcome_unknown", summary.ExecutionDisposition);
        Assert.Equal("outcome_unknown", summary.MpOutcome);
        Assert.Equal("not_attempted", summary.OutputRetrievalOutcome);
    }

    [Fact]
    public void DebugAndTraceLoggingDoNotEnableArgumentOrResultLogging()
    {
        var sink = new CapturingLogger();
        var audit = new OperationAuditLogger(sink);
        var outcome = new WorkerExecutionOutcome(
            WorkerExecutionStatus.Completed,
            WorkerExecutionDisposition.Completed,
            new WorkerMpExecutionResult(
                ExecuteStepReturned: true,
                MpResultRetrieved: true,
                MpSucceeded: false,
                MpResultCode: 3,
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

    [Fact]
    public void SustainedAuditEventsRemainCorrelatedValueFreeAndTerminal()
    {
        const int requestCount = 512;
        var sink = new CapturingLogger();
        var audit = new OperationAuditLogger(sink);
        var operation = WorkingDirectoryOperation();
        var correlationIds = Enumerable.Range(0, requestCount)
            .Select(_ => Guid.NewGuid())
            .ToArray();

        foreach (var correlationId in correlationIds)
        {
            audit.RequestStarted(correlationId, operation, "local-unauthenticated");
            audit.OperationCompleted(
                correlationId,
                operation.OperationId,
                generation: 7,
                requestDurationMilliseconds: 0,
                new OperationAuditSummary(
                    "completed",
                    "succeeded",
                    "retrieved",
                    SdkDurationMilliseconds: 0,
                    MpResultCode: 2));
        }

        Assert.Equal(requestCount, sink.Entries.Count(entry => entry.EventId == 2001));
        Assert.Equal(requestCount, sink.Entries.Count(entry => entry.EventId == 2004));
        Assert.DoesNotContain(SensitivePath, sink.AllText, StringComparison.OrdinalIgnoreCase);
        foreach (var correlationId in correlationIds)
        {
            Assert.Equal(
                2,
                sink.Entries.Count(entry => entry.Message.Contains(
                    correlationId.ToString(),
                    StringComparison.OrdinalIgnoreCase)));
        }
    }

    private static CatalogOperationDescriptor WorkingDirectoryOperation() =>
        TargetCatalogMetadata.Operations.Single(operation =>
            operation.OperationId == "file_operations.get_working_directory");

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
