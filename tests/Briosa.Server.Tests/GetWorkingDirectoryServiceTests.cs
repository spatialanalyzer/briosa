using Briosa.Core.V1Alpha1;
using Briosa.Server.Services;
using Briosa.Server.Services.Sa.V2026_1_0529_7.V1Alpha1;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using TargetProtocol = global::Briosa.Sa.V2026_1_0529_7.V1Alpha1;

namespace Briosa.Server.Tests;

public sealed class GetWorkingDirectoryServiceTests
{
    [Fact]
    [OperationTest("file_operations.get_working_directory")]
    public async Task GeneratedClientRetrievesDirectoryThroughCatalogBinding()
    {
        var executor = new RecordingExecutor(CompletedExecution(
            new WorkerMpOutputValue(
                "Directory",
                WorkerMpValueKind.Text,
                Retrieved: true,
                StringValue: @"C:\Measurements")));
        var client = CreateClient(executor);

        var result = await client.GetWorkingDirectoryAsync(
            new TargetProtocol.GetWorkingDirectoryRequest());

        Assert.True(result.HasDirectory);
        Assert.Equal(@"C:\Measurements", result.Directory);
        Assert.NotNull(result.Execution);
        Assert.Equal(MpExecutionState.Succeeded, result.Execution.State);
        Assert.True(result.Execution.HasMpResultCode);
        Assert.Equal(0, result.Execution.MpResultCode);
        Assert.Equal(
            OutputRetrievalState.Retrieved,
            Assert.Single(result.Execution.OutputRetrievals).State);
        Assert.NotNull(executor.Command);
        Assert.Equal(
            "file_operations.get_working_directory",
            executor.Command.OperationId);
        Assert.Equal("Get Working Directory", executor.Command.StepName);
        Assert.Empty(executor.Command.InputArguments);
        var output = Assert.Single(executor.Command.OutputArguments);
        Assert.Equal("Directory", output.Name);
        Assert.Equal(WorkerMpValueKind.Text, output.Kind);
        Assert.Equal("GetStringArg", output.SdkBinding);
    }

    [Fact]
    public async Task MpFailureIsDistinctAndCarriesResultCode()
    {
        var executor = new RecordingExecutor(new WorkerExecutionOutcome(
            WorkerExecutionStatus.Completed,
            new WorkerMpExecutionResult(
                ExecuteStepReturned: true,
                MpSucceeded: false,
                MpResultCode: 42,
                DurationMilliseconds: 7,
                OutputValues: [],
                "mp-command-failed"),
            Connection: null,
            "mp-command-failed",
            Generation: 3));
        var client = CreateClient(executor);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetWorkingDirectoryAsync(
                new TargetProtocol.GetWorkingDirectoryRequest()));

        Assert.Equal(StatusCode.FailedPrecondition, exception.StatusCode);
        Assert.Equal(
            "mp-command-failed",
            Trailer(exception, "briosa-diagnostic-code"));
        Assert.Equal("42", Trailer(exception, "briosa-mp-result-code"));
        Assert.Empty(executor.Command!.InputArguments);
    }

    [Fact]
    public async Task GetterFailureNeverReturnsAnEmptyDirectory()
    {
        var executor = new RecordingExecutor(CompletedExecution(
            new WorkerMpOutputValue(
                "Directory",
                WorkerMpValueKind.Text,
                Retrieved: false),
            diagnosticCode: "sdk-output-retrieval-failed"));
        var client = CreateClient(executor);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetWorkingDirectoryAsync(
                new TargetProtocol.GetWorkingDirectoryRequest()));

        Assert.Equal(StatusCode.DataLoss, exception.StatusCode);
        Assert.Equal(
            "sdk-output-retrieval-failed",
            Trailer(exception, "briosa-diagnostic-code"));
    }

    [Fact]
    public async Task CallerDeadlineIsDistinctFromTheWorkerWatchdog()
    {
        var executor = new RecordingExecutor(new WorkerExecutionOutcome(
            WorkerExecutionStatus.ClientCancelled,
            Execution: null,
            Connection: null,
            "client-wait-cancelled",
            Generation: 4));
        var client = CreateClient(executor);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetWorkingDirectoryAsync(
                new TargetProtocol.GetWorkingDirectoryRequest(),
                deadline: DateTime.UtcNow.AddSeconds(-1)));

        Assert.Equal(StatusCode.DeadlineExceeded, exception.StatusCode);
        Assert.Equal(
            OperationFailureKind.CallerDeadlineExceeded,
            ErrorDetail(exception).Kind);
    }

    [Fact]
    public async Task CallerCancellationIsDistinctFromADeadline()
    {
        var executor = new RecordingExecutor(new WorkerExecutionOutcome(
            WorkerExecutionStatus.ClientCancelled,
            Execution: null,
            Connection: null,
            "client-wait-cancelled",
            Generation: 4));
        var client = CreateClient(executor);

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetWorkingDirectoryAsync(
                new TargetProtocol.GetWorkingDirectoryRequest(),
                cancellationToken: cancellation.Token));

        Assert.Equal(StatusCode.Cancelled, exception.StatusCode);
        Assert.Equal(
            OperationFailureKind.CallerCancelled,
            ErrorDetail(exception).Kind);
    }

    [Fact]
    public async Task WatchdogFailureReportsWorkerUnavailableNotCallerDeadline()
    {
        var executor = new RecordingExecutor(new WorkerExecutionOutcome(
            WorkerExecutionStatus.WatchdogTimeout,
            Execution: null,
            Connection: null,
            "worker-watchdog-timeout",
            Generation: 4));
        var client = CreateClient(executor);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetWorkingDirectoryAsync(
                new TargetProtocol.GetWorkingDirectoryRequest()));

        Assert.Equal(StatusCode.Unavailable, exception.StatusCode);
        Assert.Equal(
            "worker-watchdog-timeout",
            Trailer(exception, "briosa-diagnostic-code"));
    }

    private static TargetProtocol.FileOperations.FileOperationsClient CreateClient(
        IWorkerCommandExecutor executor)
    {
        var service = new FileOperationsService(
            executor,
            NullLogger<FileOperationsService>.Instance,
            TimeProvider.System);
        return new TargetProtocol.FileOperations.FileOperationsClient(
            new ServiceCallInvoker(service));
    }

    private static WorkerExecutionOutcome CompletedExecution(
        WorkerMpOutputValue output,
        string? diagnosticCode = null) =>
        new(
            WorkerExecutionStatus.Completed,
            new WorkerMpExecutionResult(
                ExecuteStepReturned: true,
                MpSucceeded: true,
                MpResultCode: 0,
                DurationMilliseconds: 5,
                OutputValues: [output],
                diagnosticCode),
            Connection: null,
            diagnosticCode ?? "completed",
            Generation: 2);

    private static string Trailer(RpcException exception, string key) =>
        exception.Trailers.Single(entry => entry.Key == key).Value;

    private static OperationError ErrorDetail(RpcException exception) =>
        OperationError.Parser.ParseFrom(
            exception.Trailers.Single(entry =>
                entry.Key == GrpcOperationOutcomeMapper.ErrorTrailerName).ValueBytes);

    private sealed class RecordingExecutor(WorkerExecutionOutcome outcome) : IWorkerCommandExecutor
    {
        public WorkerMpCommand? Command { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Command = command;
            return Task.FromResult(outcome);
        }
    }

    private sealed class ServiceCallInvoker(FileOperationsService service) : CallInvoker
    {
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options,
            TRequest request)
        {
            var response = Invoke<TRequest, TResponse>(method, request, options.Deadline, options.CancellationToken);
            return new AsyncUnaryCall<TResponse>(
                response,
                Task.FromResult(new Metadata()),
                () => response.Status == TaskStatus.RanToCompletion
                    ? Status.DefaultSuccess
                    : new Status(StatusCode.Unknown, "The in-memory call failed."),
                static () => [],
                static () => { });
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options,
            TRequest request) =>
            throw new NotSupportedException();

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options,
            TRequest request) =>
            throw new NotSupportedException();

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options) =>
            throw new NotSupportedException();

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options) =>
            throw new NotSupportedException();

        private async Task<TResponse> Invoke<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            TRequest request,
            DateTime? deadline,
            CancellationToken cancellationToken)
            where TRequest : class
            where TResponse : class
        {
            Assert.Equal(
                "/briosa.sa.v2026_1_0529_7.v1alpha1.FileOperations/GetWorkingDirectory",
                method.FullName);
            Assert.IsType<TargetProtocol.GetWorkingDirectoryRequest>(request);
            var response = await service.ExecuteGetWorkingDirectory(
                    (TargetProtocol.GetWorkingDirectoryRequest)(object)request,
                    cancellationToken,
                    deadline)
                .ConfigureAwait(false);
            return (TResponse)(object)response;
        }
    }
}
