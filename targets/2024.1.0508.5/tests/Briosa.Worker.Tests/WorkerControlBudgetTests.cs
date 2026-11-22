using System.Buffers.Binary;
using Briosa.Worker.Control;

namespace Briosa.Worker.Tests;

// The private channel has two budgets: requests from the host to the worker,
// and responses from the worker to the host. Every boundary below follows the
// named constants, so raising the response budget is a one-line change in
// WorkerControlProtocol and these tests move with it.
public sealed class WorkerControlBudgetTests
{
    // Server 0.10.0 bounded every private frame, in both directions, at 64 KiB.
    private const int Server0100FrameBytes = 64 * 1024;
    private static readonly Guid CorrelationId = new("0d6e7f2a-0b8c-4f6a-9d3e-1a2b3c4d5e6f");

    [Fact]
    public void RequestBudgetKeepsItsServer0100Value()
    {
        // A literal on purpose: changing the response budget must not move it.
        Assert.Equal(Server0100FrameBytes, WorkerControlProtocol.MaximumRequestBytes);
    }

    [Fact]
    public void ResponseBudgetNeverFallsBelowItsServer0100Value()
    {
        // A smaller budget would turn results that Server 0.10.0 delivers into DataLoss.
        Assert.True(WorkerControlProtocol.MaximumResponseBytes >= Server0100FrameBytes);
        Assert.Equal(
            WorkerControlProtocol.MaximumFrameBytes,
            Math.Max(WorkerControlProtocol.MaximumRequestBytes, WorkerControlProtocol.MaximumResponseBytes));
    }

    [Fact]
    public void EveryKindIsBoundedByTheBudgetOfItsDirection()
    {
        WorkerControlMessageKind[] requests =
        [
            WorkerControlMessageKind.Ping,
            WorkerControlMessageKind.Stop,
            WorkerControlMessageKind.Execute,
            WorkerControlMessageKind.Connect,
            WorkerControlMessageKind.VerifyExecution
        ];
        WorkerControlMessageKind[] responses =
        [
            WorkerControlMessageKind.Ready,
            WorkerControlMessageKind.Pong,
            WorkerControlMessageKind.Stopped,
            WorkerControlMessageKind.ExecutionResult,
            WorkerControlMessageKind.ConnectionResult,
            WorkerControlMessageKind.ExecutionVerificationResult
        ];

        // A new kind must be placed in one direction before this test passes.
        Assert.Equal(
            Enum.GetValues<WorkerControlMessageKind>().Where(kind => kind != WorkerControlMessageKind.None).Order(),
            requests.Concat(responses).Order());
        Assert.All(requests, kind =>
            Assert.Equal(WorkerControlProtocol.MaximumRequestBytes, WorkerControlProtocol.MaximumBytesFor(kind)));
        Assert.All(responses, kind =>
            Assert.Equal(WorkerControlProtocol.MaximumResponseBytes, WorkerControlProtocol.MaximumBytesFor(kind)));
    }

    // Frames just under, at and just over both the Server 0.10.0 limit and the
    // current response budget, split by whether they fit the budget. Today the
    // two limits are equal; once the budget is raised, a result just over 64 KiB
    // moves to the delivered set.
    private static readonly int[] ResultFrameSizes =
    [
        .. new[]
        {
            Server0100FrameBytes - 1, Server0100FrameBytes, Server0100FrameBytes + 1,
            WorkerControlProtocol.MaximumResponseBytes - 1,
            WorkerControlProtocol.MaximumResponseBytes,
            WorkerControlProtocol.MaximumResponseBytes + 1
        }.Distinct().Order()
    ];

    public static TheoryData<int> DeliverableResultFrameSizes() =>
        Sizes(ResultFrameSizes.Where(size => size <= WorkerControlProtocol.MaximumResponseBytes));

    public static TheoryData<int> UndeliverableResultFrameSizes() =>
        Sizes(ResultFrameSizes.Where(size => size > WorkerControlProtocol.MaximumResponseBytes));

    private static TheoryData<int> Sizes(IEnumerable<int> sizes)
    {
        var data = new TheoryData<int>();
        foreach (var size in sizes) data.Add(size);
        return data;
    }

    [Theory]
    [MemberData(nameof(DeliverableResultFrameSizes))]
    public void AResultWithinTheResponseBudgetCrossesTheChannelWhole(int frameBytes)
    {
        var message = ResultFrame(frameBytes);
        using var stream = new MemoryStream();
        using (var worker = new WorkerControlChannel(stream, leaveOpen: true,
                   maximumReceiveBytes: WorkerControlProtocol.MaximumRequestBytes))
        {
            worker.Send(message);
        }

        Assert.Equal(frameBytes, BinaryPrimitives.ReadInt32LittleEndian(stream.ToArray()));
        stream.Position = 0;
        using var host = new WorkerControlChannel(stream, leaveOpen: true,
            maximumReceiveBytes: WorkerControlProtocol.MaximumResponseBytes);
        var execution = Assert.IsType<WorkerMpResultAvailable>(host.Receive().ExecutionResponse!.Execution);
        var output = Assert.IsType<WorkerRetrievedOutput>(Assert.Single(execution.OutputValues));
        Assert.Equal(Text(message).Length, Assert.IsType<WorkerTextValue>(output.Value).Value!.Length);
        Assert.Equal(2, execution.MpResultCode);
    }

    // Beyond the budget the frame is rejected before any byte is written, and
    // the bounded fallback keeps the MP result and marks the output unavailable.
    [Theory]
    [MemberData(nameof(UndeliverableResultFrameSizes))]
    public void AResultBeyondTheResponseBudgetKeepsItsMpResultWithoutItsOutputs(int frameBytes)
    {
        var message = ResultFrame(frameBytes);
        using var stream = new MemoryStream();
        using var worker = new WorkerControlChannel(stream, leaveOpen: true,
            maximumReceiveBytes: WorkerControlProtocol.MaximumRequestBytes);

        Assert.Throws<WorkerMessageRejectedException>(() => worker.Send(message));
        Assert.Equal(0, stream.Length);
        worker.Send(WorkerExecutionDelivery.Undeliverable(CorrelationId, message.ExecutionResponse!));

        stream.Position = 0;
        using var host = new WorkerControlChannel(stream, leaveOpen: true,
            maximumReceiveBytes: WorkerControlProtocol.MaximumResponseBytes);
        var response = host.Receive().ExecutionResponse!;
        Assert.Equal(WorkerExecutionResponseStatus.Completed, response.Status);
        Assert.Equal(WorkerExecutionDelivery.OutputEncodingRejected, response.DiagnosticCode);
        var unavailable = Assert.IsType<WorkerMpOutputsUnavailable>(response.Execution);
        Assert.Equal(2, unavailable.ResultCode);
        Assert.Equal(WorkerExecutionDelivery.OutputEncodingRejected, unavailable.DiagnosticCode);
    }

    [Fact]
    public void ARequestIsBoundedByTheRequestBudgetWhateverTheResponseBudget()
    {
        using var stream = new MemoryStream();
        using var host = new WorkerControlChannel(stream, leaveOpen: true,
            maximumReceiveBytes: WorkerControlProtocol.MaximumResponseBytes);
        host.Send(RequestFrame(WorkerControlProtocol.MaximumRequestBytes));
        Assert.Equal(
            WorkerControlProtocol.MaximumRequestBytes,
            BinaryPrimitives.ReadInt32LittleEndian(stream.ToArray()));
        stream.SetLength(0);

        Assert.Throws<WorkerMessageRejectedException>(() =>
            host.Send(RequestFrame(WorkerControlProtocol.MaximumRequestBytes + 1)));
        Assert.Equal(0, stream.Length);
    }

    // The worker endpoint bounds what it reads by the request budget, before it
    // allocates the payload. A larger response budget does not widen it.
    [Fact]
    public void TheWorkerEndpointRejectsAnOverBudgetFrameBeforeReadingIt()
    {
        using var stream = new MemoryStream();
        var header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, WorkerControlProtocol.MaximumRequestBytes + 1);
        stream.Write(header);
        stream.Write(new byte[16]);
        stream.Position = 0;
        using var worker = new WorkerControlChannel(stream, leaveOpen: true,
            maximumReceiveBytes: WorkerControlProtocol.MaximumRequestBytes);

        Assert.Throws<InvalidDataException>(() => worker.Receive());
        Assert.Equal(sizeof(int), stream.Position);
    }

    // A receiver that does not name its direction reads frames up to the larger
    // budget, and still rejects a request over the request budget. While the two
    // budgets are equal the frame header bound rejects it first; the check after
    // decoding is exercised only once the response budget is larger.
    [Fact]
    public void AnOverBudgetRequestIsRejectedEvenWithinTheFrameBound()
    {
        var frame = new MemoryStream();
        using (var encoder = new WorkerControlChannel(frame, leaveOpen: true))
        {
            encoder.Send(RequestFrame(WorkerControlProtocol.MaximumRequestBytes));
        }

        // Grow the padding by one byte; the result is valid JSON over the budget.
        var payload = frame.ToArray().AsSpan(sizeof(int)).ToArray();
        var padding = payload.AsSpan().IndexOf("xxxxxxxxxxxxxxxx"u8);
        Assert.True(padding > 0);
        var grown = new byte[payload.Length + 1];
        payload.AsSpan(0, padding).CopyTo(grown);
        grown[padding] = (byte)'x';
        payload.AsSpan(padding).CopyTo(grown.AsSpan(padding + 1));
        using var stream = new MemoryStream();
        var header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, grown.Length);
        stream.Write(header);
        stream.Write(grown);
        stream.Position = 0;
        using var receiver = new WorkerControlChannel(stream, leaveOpen: true);

        Assert.Throws<InvalidDataException>(() => receiver.Receive());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(WorkerControlProtocol.MaximumFrameBytes + 1)]
    public void AReceiveBoundOutsideTheFrameBoundIsRejected(int maximumReceiveBytes)
    {
        using var stream = new MemoryStream();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WorkerControlChannel(stream, maximumReceiveBytes: maximumReceiveBytes));
    }

    // One ExecutionResult frame whose encoded payload is exactly frameBytes long.
    // Each padding 'x' adds one byte, so the size follows from an empty frame.
    private static WorkerControlMessage ResultFrame(int frameBytes) =>
        Result(new string('x', frameBytes - EncodedLength(Result(string.Empty))));

    private static WorkerControlMessage RequestFrame(int frameBytes) =>
        Request(new string('x', frameBytes - EncodedLength(Request(string.Empty))));

    private static WorkerControlMessage Result(string text) =>
        WorkerControlMessage.ExecutionResult(CorrelationId, new WorkerExecutionResponse(
            WorkerExecutionResponseStatus.Completed,
            new WorkerMpResultAvailable(2, 5,
                [new WorkerRetrievedOutput("Directory", WorkerMpValueKind.Text, new WorkerTextValue(text))], null),
            Connection(),
            DiagnosticCode: null));

    private static WorkerControlMessage Request(string text) =>
        WorkerControlMessage.Execute(CorrelationId, new WorkerMpCommand(
            "budget", "Budget",
            [new WorkerMpInputArgument("Text", WorkerMpValueKind.Text, new WorkerTextValue(text))], []));

    private static string Text(WorkerControlMessage message) =>
        ((WorkerTextValue)((WorkerRetrievedOutput)message.ExecutionResponse!.Execution!.OutputValues[0]).Value!).Value!;

    private static int EncodedLength(WorkerControlMessage message)
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        channel.Send(message);
        return checked((int)stream.Length - sizeof(int));
    }

    private static WorkerConnectionSnapshot Connection() =>
        new(
            WorkerConnectionState.Connected,
            WorkerExecutionReadinessState.ExecutionReady,
            StatusCode: 0,
            Attempt: 1,
            MaximumAttempts: 1,
            DiagnosticCode: "sdk-connected",
            DateTimeOffset.UnixEpoch);
}
