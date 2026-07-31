using global::Briosa;
using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class GetIThCollectionNameServiceTests
{
    [Fact]
    [OperationTest("analysis_operations.get_i_th_collection_name")]
    public async Task GeneratedClientRetrievesCollectionNameThroughHandwrittenBinding()
    {
        var executor = new RecordingExecutor(CompletedExecution(
            new WorkerMpOutputValue(
                "Resultant Name",
                WorkerMpValueKind.CollectionName,
                Retrieved: true,
                StringValue: "Collection 1")));
        var client = CreateClient(executor);

        var result = await client.GetIThCollectionNameAsync(
            new Api.GetIThCollectionNameRequest
            {
                CollectionIndex = 0
            });

        Assert.True(result.HasResultantName);
        Assert.Equal("Collection 1", result.ResultantName);
        Assert.NotNull(result.Execution);
        Assert.Equal(MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(
            OutputRetrievalState.Retrieved,
            Assert.Single(result.Execution.OutputRetrievals).State);

        Assert.NotNull(executor.Command);
        Assert.Equal(
            "analysis_operations.get_i_th_collection_name",
            executor.Command.OperationId);
        Assert.Equal("Get i-th Collection Name", executor.Command.StepName);
        var input = Assert.Single(executor.Command.InputArguments);
        Assert.Equal("Collection Index", input.Name);
        Assert.Equal(WorkerMpValueKind.WholeNumber, input.Kind);
        Assert.Equal(0, input.IntegerValue);
        Assert.Equal("SetIntegerArg", input.SdkBinding);
        var output = Assert.Single(executor.Command.OutputArguments);
        Assert.Equal("Resultant Name", output.Name);
        Assert.Equal(WorkerMpValueKind.CollectionName, output.Kind);
        Assert.Equal("GetCollectionNameArg", output.SdkBinding);
    }

    [Fact]
    public async Task OmittedCollectionIndexFailsBeforeWorkerAdmission()
    {
        var executor = new RecordingExecutor(CompletedExecution(
            new WorkerMpOutputValue(
                "Resultant Name",
                WorkerMpValueKind.CollectionName,
                Retrieved: true,
                StringValue: "unused")));
        var client = CreateClient(executor);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetIThCollectionNameAsync(
                new Api.GetIThCollectionNameRequest()));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        Assert.Equal(
            "request-validation-failed",
            ErrorDetail(exception).DiagnosticCode);
        Assert.Null(executor.Command);
    }

    [Fact]
    public async Task MpFailureIsDistinctAndCarriesResultCode()
    {
        var executor = new RecordingExecutor(new WorkerExecutionOutcome(
            WorkerExecutionStatus.Completed,
            WorkerExecutionDisposition.Completed,
            new WorkerMpExecutionResult(
                ExecuteStepReturned: true,
                MpResultRetrieved: true,
                MpSucceeded: false,
                MpResultCode: 3,
                DurationMilliseconds: 7,
                OutputValues: [],
                "mp-command-failed"),
            Connection: null,
            "mp-command-failed",
            Generation: 3));
        var client = CreateClient(executor);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetIThCollectionNameAsync(Request()));

        Assert.Equal(StatusCode.FailedPrecondition, exception.StatusCode);
        var error = ErrorDetail(exception);
        Assert.Equal("mp-command-failed", error.DiagnosticCode);
        Assert.True(error.MpExecution.HasMpResultCode);
        Assert.Equal(3, error.MpExecution.MpResultCode);
    }

    [Fact]
    public async Task GetterFailureNeverReturnsAnEmptyCollectionName()
    {
        var executor = new RecordingExecutor(CompletedExecution(
            new WorkerMpOutputValue(
                "Resultant Name",
                WorkerMpValueKind.CollectionName,
                Retrieved: false),
            diagnosticCode: "sdk-output-retrieval-failed"));
        var client = CreateClient(executor);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetIThCollectionNameAsync(Request()));

        Assert.Equal(StatusCode.DataLoss, exception.StatusCode);
        Assert.Equal(
            "sdk-output-retrieval-failed",
            ErrorDetail(exception).DiagnosticCode);
    }

    [Fact]
    public async Task CallerDeadlineRemainsDistinctFromWorkerWatchdog()
    {
        var executor = new RecordingExecutor(new WorkerExecutionOutcome(
            WorkerExecutionStatus.ClientCancelled,
            WorkerExecutionDisposition.StartedOutcomeUnknown,
            Execution: null,
            Connection: null,
            "client-wait-cancelled",
            Generation: 4));
        var client = CreateClient(executor);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetIThCollectionNameAsync(
                Request(),
                deadline: DateTime.UtcNow.AddSeconds(-1)));

        Assert.Equal(StatusCode.DeadlineExceeded, exception.StatusCode);
        Assert.Equal(
            OperationFailureKind.CallerDeadlineExceeded,
            ErrorDetail(exception).Kind);
    }

    [Fact]
    public async Task CallerCancellationRemainsDistinctFromDeadline()
    {
        var executor = new RecordingExecutor(new WorkerExecutionOutcome(
            WorkerExecutionStatus.ClientCancelled,
            WorkerExecutionDisposition.NotStarted,
            Execution: null,
            Connection: null,
            "client-wait-cancelled",
            Generation: 4));
        var client = CreateClient(executor);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetIThCollectionNameAsync(
                Request(),
                cancellationToken: cancellation.Token));

        Assert.Equal(StatusCode.Cancelled, exception.StatusCode);
        Assert.Equal(
            OperationFailureKind.CallerCancelled,
            ErrorDetail(exception).Kind);
    }

    private static Api.GetIThCollectionNameRequest Request() =>
        new() { CollectionIndex = 0 };

    private static Api.AnalysisOperations.AnalysisOperationsClient CreateClient(
        IWorkerCommandExecutor executor)
    {
        var service = new AnalysisOperationsService(new OperationExecutor(
            executor,
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance),
            TimeProvider.System));
        return new Api.AnalysisOperations.AnalysisOperationsClient(
            new ServiceCallInvoker(service));
    }

    private static WorkerExecutionOutcome CompletedExecution(
        WorkerMpOutputValue output,
        string? diagnosticCode = null) =>
        new(
            WorkerExecutionStatus.Completed,
            WorkerExecutionDisposition.Completed,
            new WorkerMpExecutionResult(
                ExecuteStepReturned: true,
                MpResultRetrieved: true,
                MpSucceeded: true,
                MpResultCode: 2,
                DurationMilliseconds: 5,
                OutputValues: [output],
                diagnosticCode),
            Connection: null,
            diagnosticCode ?? "completed",
            Generation: 2);

    private static OperationError ErrorDetail(RpcException exception)
    {
        var trailer = Assert.Single(exception.Trailers);
        Assert.Equal(GrpcOperationOutcomeMapper.ErrorTrailerName, trailer.Key);
        return OperationError.Parser.ParseFrom(trailer.ValueBytes);
    }

    private sealed class RecordingExecutor(WorkerExecutionOutcome outcome) :
        IWorkerCommandExecutor
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

    private sealed class ServiceCallInvoker(AnalysisOperationsService service) : CallInvoker
    {
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options,
            TRequest request)
        {
            var response = Invoke<TRequest, TResponse>(
                method,
                request,
                options.Deadline,
                options.CancellationToken);
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
                "/briosa.AnalysisOperations/GetIThCollectionName",
                method.FullName);
            Assert.IsType<Api.GetIThCollectionNameRequest>(request);
            var response = await service.ExecuteGetIThCollectionName(
                    (Api.GetIThCollectionNameRequest)(object)request,
                    cancellationToken,
                    deadline)
                .ConfigureAwait(false);
            return (TResponse)(object)response;
        }
    }
}
