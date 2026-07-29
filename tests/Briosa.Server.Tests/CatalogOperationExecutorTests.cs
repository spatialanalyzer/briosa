using Briosa.Core.V1Alpha1;
using Briosa.Server.Generated.Sa.V2026_1_0529_7.V1Alpha1;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Briosa.Server.Tests;

public sealed class CatalogOperationExecutorTests
{
    [Fact]
    public async Task ResultMappingFailureEmitsOnlyValueFreeFailureAudit()
    {
        const string sensitiveValue = @"C:\Customers\Secret\geometry.xit";
        var sink = new CapturingLogger();
        var executor = new CatalogOperationExecutor(
            new SuccessfulExecutor(sensitiveValue),
            new OperationAuditLogger(sink),
            TimeProvider.System);
        var operation = WorkingDirectoryOperation();

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            executor.ExecuteAsync<object, object>(
                new object(),
                operation,
                _ => new WorkerMpCommand(
                    operation.OperationId,
                    operation.MpStep,
                    [],
                    [new("Directory", WorkerMpValueKind.Text, "GetStringArg")]),
                [new("directory", "Directory", WorkerMpValueKind.Text)],
                _ => throw new InvalidOperationException(sensitiveValue),
                CancellationToken.None));

        Assert.Equal(StatusCode.DataLoss, exception.StatusCode);
        Assert.Equal("result-mapping-failed", GrpcOperationOutcomeMapper.GetDiagnosticCode(exception));
        Assert.DoesNotContain(sink.Entries, entry => entry.EventId == 2004);
        Assert.Single(sink.Entries, entry => entry.EventId == 2005);
        Assert.DoesNotContain(sensitiveValue, sink.AllText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GeneratedValidationFailureNeverEnqueuesAWorkerCommand()
    {
        var worker = new NeverCalledExecutor();
        var executor = new CatalogOperationExecutor(
            worker,
            new OperationAuditLogger(new CapturingLogger()),
            TimeProvider.System);
        var operation = WorkingDirectoryOperation();

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            executor.ExecuteAsync<object, object>(
                new object(),
                operation,
                _ => throw new ArgumentException("invalid request"),
                [],
                _ => new object(),
                CancellationToken.None));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        Assert.Equal(OperationFailureKind.Validation, ReadError(exception).Kind);
        Assert.False(worker.Called);
    }

    private static OperationError ReadError(RpcException exception) =>
        OperationError.Parser.ParseFrom(Assert.Single(exception.Trailers).ValueBytes);

    private static Briosa.Server.Security.CatalogOperationDescriptor WorkingDirectoryOperation() =>
        TargetCatalogMetadata.Operations.Single(operation =>
            operation.OperationId == "file_operations.get_working_directory");

    private sealed class SuccessfulExecutor(string value) : IWorkerCommandExecutor
    {
        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkerExecutionOutcome(
                WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed,
                new WorkerMpExecutionResult(
                    ExecuteStepReturned: true,
                    MpResultRetrieved: true,
                    MpSucceeded: true,
                    MpResultCode: 2,
                    DurationMilliseconds: 1,
                    OutputValues:
                    [
                        new WorkerMpOutputValue(
                            "Directory",
                            WorkerMpValueKind.Text,
                            Retrieved: true,
                            StringValue: value)
                    ],
                    DiagnosticCode: "completed"),
                Connection: null,
                DiagnosticCode: "completed",
                Generation: 1));
    }

    private sealed class NeverCalledExecutor : IWorkerCommandExecutor
    {
        public bool Called { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Called = true;
            throw new InvalidOperationException("The worker must not be called.");
        }
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
