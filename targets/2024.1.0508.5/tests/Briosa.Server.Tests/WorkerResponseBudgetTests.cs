using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Operations.Variables;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

// The public inbound limit and the private worker response budget are separate
// limits. These tests send synthetic results through the production private
// channel and the public gRPC endpoint. Every boundary follows the named
// constants, so raising WorkerControlProtocol.MaximumResponseBytes is a one-line
// change and these tests move with it.
public sealed class WorkerResponseBudgetTests
{
    // Server 0.10.0 accepted public requests and delivered private frames up to 64 KiB.
    private const int Server0100LimitBytes = 64 * 1024;

    // The default receive limit of grpc-dotnet (Grpc.Net.Client), grpc-js and
    // grpcio. The published 0.5.0 clients create their channels without options,
    // so they cannot receive a larger response message.
    private const int FirstPartyClientDefaultReceiveBytes = 4 * 1024 * 1024;

    [Fact]
    public void PublicInboundLimitKeepsItsServer0100Value()
    {
        // A literal on purpose: changing a worker budget must not move it.
        Assert.Equal(Server0100LimitBytes, PublicGrpcHosting.MaximumReceiveMessageBytes);

        var services = new ServiceCollection();
        services.AddBriosaGrpc();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<GrpcServiceOptions>>().Value;
        Assert.Equal(Server0100LimitBytes, options.MaxReceiveMessageSize);
        Assert.Null(options.MaxSendMessageSize);
    }

    // gRPC rejects an over-limit request before the service runs, so the
    // worker sees nothing. Today's transport-level rejection is unchanged.
    [Fact]
    public async Task ThePublicEndpointAcceptsARequestAtItsInboundLimitAndRejectsOneByteMore()
    {
        var worker = new RecordingWorker(_ =>
            [new WorkerRetrievedOutput("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1.25))]);
        var host = await GrpcTestHost.StartAsync<VariablesService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.Variables.VariablesClient(host.Channel);

        var atLimit = await client.GetDoubleVariableAsync(RequestOfSize(Server0100LimitBytes));
        Assert.Equal(1.25, atLimit.Value);
        Assert.Single(worker.Commands);

        var overLimit = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetDoubleVariableAsync(RequestOfSize(Server0100LimitBytes + 1))).ConfigureAwait(true);
        Assert.Equal(StatusCode.ResourceExhausted, overLimit.StatusCode);
        Assert.Single(worker.Commands);
    }

    // Frames just under, at and just over both the Server 0.10.0 limit and the
    // current response budget, split by whether they fit the budget.
    private static readonly int[] ResultFrameSizes =
    [
        .. new[]
        {
            Server0100LimitBytes - 1, Server0100LimitBytes, Server0100LimitBytes + 1,
            WorkerControlProtocol.MaximumResponseBytes - 1,
            WorkerControlProtocol.MaximumResponseBytes,
            WorkerControlProtocol.MaximumResponseBytes + 1
        }.Distinct().Order()
    ];

    public static TheoryData<int> DeliverableResultFrameSizes() =>
        Sizes(ResultFrameSizes.Where(size => size <= WorkerControlProtocol.MaximumResponseBytes));

    public static TheoryData<int> UndeliverableResultFrameSizes() =>
        Sizes(ResultFrameSizes.Where(size => size > WorkerControlProtocol.MaximumResponseBytes));

    [Theory]
    [MemberData(nameof(DeliverableResultFrameSizes))]
    public async Task AResultWithinTheResponseBudgetReachesTheGeneratedClientWhole(int frameBytes)
    {
        var worker = new PrivateChannelWorker(command => TextResult(command, frameBytes));
        var host = await GrpcTestHost.StartAsync<FileOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.FileOperations.FileOperationsClient(host.Channel);

        var result = await client.GetWorkingDirectoryAsync(new Api.GetWorkingDirectoryRequest());

        Assert.Equal(frameBytes, worker.DeliveredFrameBytes);
        Assert.Equal(worker.DeliveredText!.Length, result.Directory.Length);
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(2, result.Execution.MpResultCode);
    }

    // A result beyond the budget still completes as today: DataLoss, Completed,
    // DoNotReplay, with the MP result preserved and no output values.
    [Theory]
    [MemberData(nameof(UndeliverableResultFrameSizes))]
    public async Task AResultBeyondTheResponseBudgetIsCompletedDataLossWithItsMpResult(int frameBytes)
    {
        var worker = new PrivateChannelWorker(command => TextResult(command, frameBytes));
        var host = await GrpcTestHost.StartAsync<FileOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.FileOperations.FileOperationsClient(host.Channel);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetWorkingDirectoryAsync(new Api.GetWorkingDirectoryRequest())).ConfigureAwait(true);

        Assert.True(worker.Rejected);
        Assert.Equal(StatusCode.DataLoss, exception.StatusCode);
        var error = Error(exception);
        Assert.Equal(Api.OperationFailureKind.OutputRetrievalFailure, error.Kind);
        Assert.Equal(WorkerExecutionDelivery.OutputEncodingRejected, error.DiagnosticCode);
        Assert.Equal(Api.ExecutionDisposition.Completed, error.ExecutionDisposition);
        Assert.Equal(Api.ReplayGuidance.DoNotReplay, error.ReplayGuidance);
        Assert.Equal(Api.MpExecutionState.Succeeded, error.MpExecution.State);
        Assert.Equal(2, error.MpExecution.MpResultCode);
        Assert.Equal(Api.OutputRetrievalState.Failed, Assert.Single(error.MpExecution.OutputRetrievals).State);
    }

    // The densest result: a double list of zeros encodes each value in two
    // bytes of JSON but eight bytes of protobuf, so the public response is
    // about four times the private frame. At the response budget it must still
    // fit the clients' default receive limit, or the caller would get a
    // transport error for an MP command that ran.
    [Fact]
    public async Task TheDensestResultAtTheResponseBudgetFitsTheClientsDefaultReceiveLimit()
    {
        var worker = new PrivateChannelWorker(DensestDoubleListResult);
        var host = await GrpcTestHost.StartAsync<VariablesService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.Variables.VariablesClient(host.Channel);

        var result = await client.GetNamedDoubleListVariableAsync(
            new Api.GetNamedDoubleListVariableRequest { Name = "budget" });

        Assert.False(worker.Rejected);
        Assert.InRange(worker.DeliveredFrameBytes, WorkerControlProtocol.MaximumResponseBytes - 1,
            WorkerControlProtocol.MaximumResponseBytes);
        Assert.Equal(worker.DeliveredDoubles, result.DoubleListVariable.Count);
        Assert.All(result.DoubleListVariable, value => Assert.Equal(0, value));
        Assert.InRange(result.CalculateSize(), 3 * WorkerControlProtocol.MaximumResponseBytes,
            FirstPartyClientDefaultReceiveBytes);
    }

    private static Api.GetDoubleVariableRequest RequestOfSize(int bytes)
    {
        var request = new Api.GetDoubleVariableRequest { Name = string.Empty };
        request.Name = new string('n', bytes - request.CalculateSize());
        while (request.CalculateSize() > bytes)
        {
            // The length prefix grew; shorten the name by what it added.
            request.Name = request.Name[..^(request.CalculateSize() - bytes)];
        }

        Assert.Equal(bytes, request.CalculateSize());
        return request;
    }

    // A Text result for the command's single output whose ExecutionResult frame
    // encodes to exactly frameBytes; each padding 'x' adds one byte.
    private static WorkerExecutionResponse TextResult(WorkerMpCommand command, int frameBytes)
    {
        var name = Assert.Single(command.OutputArguments).Name;
        var empty = EncodedLength(TextResponse(name, string.Empty));
        return TextResponse(name, new string('x', frameBytes - empty));
    }

    private static WorkerExecutionResponse TextResponse(string name, string text) =>
        Completed(new WorkerRetrievedOutput(name, WorkerMpValueKind.Text, new WorkerTextValue(text)));

    // The longest list of zeros whose frame fits the response budget. A list of
    // n zeros adds 2n - 1 bytes to the frame of an empty list.
    private static WorkerExecutionResponse DensestDoubleListResult(WorkerMpCommand command)
    {
        var name = Assert.Single(command.OutputArguments).Name;
        var empty = EncodedLength(DoubleListResponse(name, 0));
        return DoubleListResponse(name, (WorkerControlProtocol.MaximumResponseBytes - empty + 1) / 2);
    }

    private static WorkerExecutionResponse DoubleListResponse(string name, int count) =>
        Completed(new WorkerRetrievedOutput(name, WorkerMpValueKind.DoubleArray,
            new WorkerDoubleArrayValue(new double[count])));

    private static WorkerExecutionResponse Completed(WorkerMpOutputValue output) =>
        new(
            WorkerExecutionResponseStatus.Completed,
            new WorkerMpResultAvailable(2, 5, [output], null),
            new WorkerConnectionSnapshot(
                WorkerConnectionState.Connected,
                WorkerExecutionReadinessState.ExecutionReady,
                StatusCode: 0,
                Attempt: 1,
                MaximumAttempts: 1,
                DiagnosticCode: "sdk-connected",
                DateTimeOffset.UnixEpoch),
            DiagnosticCode: null);

    private static readonly Guid CorrelationId = new("6c1f9a3e-2d4b-4e8f-a1c7-5b9d0e2f4a6c");

    private static int EncodedLength(WorkerExecutionResponse response)
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        channel.Send(WorkerControlMessage.ExecutionResult(CorrelationId, response));
        return checked((int)stream.Length - sizeof(int));
    }

    private static TheoryData<int> Sizes(IEnumerable<int> sizes)
    {
        var data = new TheoryData<int>();
        foreach (var size in sizes) data.Add(size);
        return data;
    }

    private static Api.OperationError Error(RpcException exception)
    {
        var entry = Assert.Single(
            exception.Trailers,
            item => item.Key == GrpcOperationOutcomeMapper.ErrorTrailerName);
        return Api.OperationError.Parser.ParseFrom(entry.ValueBytes);
    }

    // Sends the scripted result as the production worker host does, including
    // the bounded replacement for a frame the channel rejects, and receives it
    // through the host endpoint's own response budget.
    private sealed class PrivateChannelWorker(Func<WorkerMpCommand, WorkerExecutionResponse> respond)
        : IWorkerCommandExecutor
    {
        public int DeliveredFrameBytes { get; private set; }
        public string? DeliveredText { get; private set; }
        public int DeliveredDoubles { get; private set; }
        public bool Rejected { get; private set; }

        public async Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            var response = respond(command);
            using var stream = new MemoryStream();
            using (var worker = new WorkerControlChannel(stream, leaveOpen: true,
                       maximumReceiveBytes: WorkerControlProtocol.MaximumRequestBytes))
            {
                try
                {
                    await worker.SendAsync(WorkerControlMessage.ExecutionResult(CorrelationId, response),
                        cancellationToken).ConfigureAwait(false);
                }
                catch (WorkerMessageRejectedException)
                {
                    Assert.Equal(0, stream.Length);
                    Rejected = true;
                    await worker.SendAsync(WorkerExecutionDelivery.Undeliverable(CorrelationId, response),
                        cancellationToken).ConfigureAwait(false);
                }
            }

            DeliveredFrameBytes = checked((int)stream.Length - sizeof(int));
            stream.Position = 0;
            using var host = new WorkerControlChannel(stream, leaveOpen: true,
                maximumReceiveBytes: WorkerControlProtocol.MaximumResponseBytes);
            var delivered = (await host.ReceiveAsync(cancellationToken).ConfigureAwait(false)).ExecutionResponse!;
            if (delivered.Execution!.OutputValues is [WorkerRetrievedOutput { Value: var value }])
            {
                DeliveredText = (value as WorkerTextValue)?.Value;
                DeliveredDoubles = (value as WorkerDoubleArrayValue)?.Values.Count ?? 0;
            }

            return WorkerExecutionOutcome.FromWorkerResponse(delivered, generation: 1, CorrelationId);
        }
    }
}
