using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;
using Briosa.Server.Operations.RobotOperations;
using Briosa.Server.Operations.Variables;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

// The public inbound limit and the private worker response budget are separate
// limits. These tests send synthetic results through the production private
// channel and the public gRPC endpoint. Every boundary follows the named
// constants, so raising WorkerControlProtocol.MaximumResponseBytes is a one-line
// change and these tests move with it.
public sealed class WorkerResponseBudgetTests(ITestOutputHelper output)
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

    // Every operation that returns double lists, through its real mapper, with a
    // private frame at the response budget in the smallest legal envelope
    // (MinimalEnvelope). The first double list holds as many zeros, the densest
    // JSON, as the frame allows; later lists are empty. That is one tested
    // distribution per operation, not each operation's maximum. Its public
    // response must stay within the published clients' 4 MiB receive limit.
    public static TheoryData<string> DoubleListOperations() => new()
    {
        GetNamedDoubleListVariableOperation.Descriptor.OperationId,
        GetRobotPoseForAFrameOperation.Descriptor.OperationId,
        GetCalibrationApplianceDataOperation.Descriptor.OperationId,
        GetCalibrationApplianceNodeDataOperation.Descriptor.OperationId,
        GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.Descriptor.OperationId,
        GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixOperation.Descriptor.OperationId,
        CalculateTcpFixtureUncertaintiesOperation.Descriptor.OperationId
    };

    [Theory]
    [MemberData(nameof(DoubleListOperations))]
    public async Task EveryDoubleListResponseAtTheBudgetFitsTheClientsDefaultReceiveLimit(string operationId)
    {
        var (frameBytes, responseBytes) = await DensestResponse(operationId).ConfigureAwait(true);
        WriteMargin(operationId, frameBytes, responseBytes);

        Assert.InRange(frameBytes, WorkerControlProtocol.MaximumResponseBytes - 1,
            WorkerControlProtocol.MaximumResponseBytes);
        Assert.InRange(responseBytes, 3 * WorkerControlProtocol.MaximumResponseBytes,
            FirstPartyClientDefaultReceiveBytes);
    }

    // A distribution with every list populated: the six covariance rows, each just
    // over a power of two in length, in a 1,048,575-byte frame. Each populated row
    // adds its own protobuf field header, so this response is larger than the
    // first-list distribution's for the same operation.
    [Fact]
    public async Task ASixRowCovarianceResponseAtTheBudgetFitsTheClientsDefaultReceiveLimit()
    {
        int[] rows = [262_145, 131_073, 65_537, 32_769, 16_385, 15_914];
        var (frameBytes, responseBytes) = await Densest<Api.GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldRequest,
            Api.GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldResult>(
            GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.Descriptor,
            GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.CreateCommand,
            GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.OutputContracts,
            GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.CreateResult,
            command => MinimalEnvelope([.. command.OutputArguments.Select((argument, index) =>
                (WorkerMpOutputValue)new WorkerRetrievedOutput(argument.Name, argument.Kind,
                    new WorkerDoubleArrayValue(new double[rows[index]])))])).ConfigureAwait(true);
        WriteMargin("six-row covariance", frameBytes, responseBytes);

        Assert.Equal(WorkerControlProtocol.MaximumResponseBytes - 1, frameBytes);
        Assert.InRange(responseBytes, 3 * WorkerControlProtocol.MaximumResponseBytes,
            FirstPartyClientDefaultReceiveBytes);
    }

    private void WriteMargin(string name, int frameBytes, int responseBytes) =>
        output.WriteLine(
            $"{name}: frame {frameBytes} bytes, response {responseBytes} bytes, " +
            $"{FirstPartyClientDefaultReceiveBytes - responseBytes} bytes under the client limit");

    private static Task<(int FrameBytes, int ResponseBytes)> DensestResponse(string operationId) => operationId switch
    {
        _ when operationId == GetNamedDoubleListVariableOperation.Descriptor.OperationId => Densest<Api.GetNamedDoubleListVariableRequest, Api.GetNamedDoubleListVariableResult>(
            GetNamedDoubleListVariableOperation.Descriptor, GetNamedDoubleListVariableOperation.CreateCommand,
            GetNamedDoubleListVariableOperation.OutputContracts, GetNamedDoubleListVariableOperation.CreateResult),
        _ when operationId == GetRobotPoseForAFrameOperation.Descriptor.OperationId => Densest<Api.GetRobotPoseForAFrameRequest, Api.GetRobotPoseForAFrameResult>(
            GetRobotPoseForAFrameOperation.Descriptor, GetRobotPoseForAFrameOperation.CreateCommand,
            GetRobotPoseForAFrameOperation.OutputContracts, GetRobotPoseForAFrameOperation.CreateResult),
        _ when operationId == GetCalibrationApplianceDataOperation.Descriptor.OperationId => Densest<Api.GetCalibrationApplianceDataRequest, Api.GetCalibrationApplianceDataResult>(
            GetCalibrationApplianceDataOperation.Descriptor, GetCalibrationApplianceDataOperation.CreateCommand,
            GetCalibrationApplianceDataOperation.OutputContracts, GetCalibrationApplianceDataOperation.CreateResult),
        _ when operationId == GetCalibrationApplianceNodeDataOperation.Descriptor.OperationId => Densest<Api.GetCalibrationApplianceNodeDataRequest, Api.GetCalibrationApplianceNodeDataResult>(
            GetCalibrationApplianceNodeDataOperation.Descriptor, GetCalibrationApplianceNodeDataOperation.CreateCommand,
            GetCalibrationApplianceNodeDataOperation.OutputContracts, GetCalibrationApplianceNodeDataOperation.CreateResult),
        _ when operationId == GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.Descriptor.OperationId => Densest<Api.GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldRequest, Api.GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldResult>(
            GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.Descriptor,
            GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.CreateCommand,
            GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.OutputContracts,
            GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.CreateResult),
        _ when operationId == GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixOperation.Descriptor.OperationId => Densest<Api.GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixRequest, Api.GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixResult>(
            GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixOperation.Descriptor,
            GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixOperation.CreateCommand,
            GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixOperation.OutputContracts,
            GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixOperation.CreateResult),
        _ when operationId == CalculateTcpFixtureUncertaintiesOperation.Descriptor.OperationId => Densest<Api.CalculateTcpFixtureUncertaintiesRequest, Api.CalculateTcpFixtureUncertaintiesResult>(
            CalculateTcpFixtureUncertaintiesOperation.Descriptor, CalculateTcpFixtureUncertaintiesOperation.CreateCommand,
            CalculateTcpFixtureUncertaintiesOperation.OutputContracts, CalculateTcpFixtureUncertaintiesOperation.CreateResult),
        _ => throw new ArgumentOutOfRangeException(nameof(operationId))
    };

    private static async Task<(int FrameBytes, int ResponseBytes)> Densest<TRequest, TResult>(
        OperationDescriptor descriptor,
        Func<TRequest, WorkerMpCommand> createCommand,
        IReadOnlyList<OperationOutputContract> outputContracts,
        Func<SuccessfulOperationExecution, TResult> createResult,
        Func<WorkerMpCommand, WorkerExecutionResponse>? respond = null)
        where TRequest : class, IMessage<TRequest>, new()
        where TResult : class, IMessage
    {
        var worker = new PrivateChannelWorker(respond ?? DensestDoubleLists);
        var executor = new OperationExecutor(worker,
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance), TimeProvider.System);
        var request = OperationConditionalOptionTests.RequestPopulator.Create<TRequest>();
        // The populated transform is not a 4x4 matrix; this operation requires one.
        if (request is Api.CalculateTcpFixtureUncertaintiesRequest tcp)
        {
            tcp.TcpInWorking = new Api.Transform();
            tcp.TcpInWorking.Values.AddRange(new double[16]);
        }

        var result = await executor.ExecuteAsync(
            request, descriptor, createCommand, outputContracts, createResult, CancellationToken.None).ConfigureAwait(true);
        Assert.False(worker.Rejected);
        return (worker.DeliveredFrameBytes, result.CalculateSize());
    }

    // The first double list gets as many zeros as the frame allows; every other
    // output gets the smallest value of its kind.
    private static WorkerExecutionResponse DensestDoubleLists(WorkerMpCommand command)
    {
        WorkerExecutionResponse With(int count)
        {
            var outputs = new List<WorkerMpOutputValue>();
            var remaining = count;
            foreach (var output in command.OutputArguments)
            {
                WorkerMpValue value = output.Kind switch
                {
                    WorkerMpValueKind.DoubleArray => new WorkerDoubleArrayValue(new double[remaining]),
                    WorkerMpValueKind.Logical => new WorkerBooleanValue(true),
                    WorkerMpValueKind.FloatingPoint => new WorkerDoubleValue(0),
                    WorkerMpValueKind.Transform => new WorkerTransformValue(new double[16]),
                    WorkerMpValueKind.EditText => new WorkerStringListValue([]),
                    _ => throw new InvalidOperationException("No densest value for this output kind.")
                };
                if (output.Kind == WorkerMpValueKind.DoubleArray) remaining = 0;
                outputs.Add(new WorkerRetrievedOutput(output.Name, output.Kind, value));
            }

            return MinimalEnvelope(outputs);
        }

        int lower = 0, upper = WorkerControlProtocol.MaximumResponseBytes;
        while (lower < upper)
        {
            var middle = (lower + upper + 1) / 2;
            if (TryEncodedLength(With(middle)) is not null)
                lower = middle;
            else
                upper = middle - 1;
        }

        return With(lower);
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

    private static WorkerExecutionResponse Completed(WorkerMpOutputValue output) => Completed([output]);

    // The smallest execution result the channel accepts around these outputs.
    // Every other field is fixed by the protocol or the operation (version, kind,
    // discriminators, output names and kinds, the Connected and ExecutionReady
    // values), has a fixed width (the correlation ID), or is at its minimum: the
    // timestamp at its minimum width, with no fractional seconds; optional fields
    // omitted (the connection status code, runtime identity and diagnostic codes);
    // the required connection diagnostic code at the one character the channel
    // allows; and every number at one digit.
    private static WorkerExecutionResponse MinimalEnvelope(IReadOnlyList<WorkerMpOutputValue> outputs) =>
        new(
            WorkerExecutionResponseStatus.Completed,
            new WorkerMpResultAvailable(2, 0, outputs, null),
            new WorkerConnectionSnapshot(
                WorkerConnectionState.Connected,
                WorkerExecutionReadinessState.ExecutionReady,
                StatusCode: null,
                Attempt: 1,
                MaximumAttempts: 1,
                DiagnosticCode: "c",
                default),
            DiagnosticCode: null);

    private static WorkerExecutionResponse Completed(IReadOnlyList<WorkerMpOutputValue> outputs) =>
        new(
            WorkerExecutionResponseStatus.Completed,
            new WorkerMpResultAvailable(2, 5, outputs, null),
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

    private static int EncodedLength(WorkerExecutionResponse response) =>
        TryEncodedLength(response) ?? throw new InvalidOperationException("The frame is over the response budget.");

    private static int? TryEncodedLength(WorkerExecutionResponse response)
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        try
        {
            channel.Send(WorkerControlMessage.ExecutionResult(CorrelationId, response));
        }
        catch (WorkerMessageRejectedException)
        {
            return null;
        }

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
