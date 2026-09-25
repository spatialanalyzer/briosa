using System.Buffers.Binary;
using System.Text.Json;
using Briosa.Worker.Control;

namespace Briosa.Worker.Tests;

public sealed class WorkerControlRejectionTests
{
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void InvalidLocalInputWritesNoFrameBytes(double value)
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        var command = new WorkerMpCommand("regression", "Regression",
            [new WorkerMpInputArgument("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(value))], []);

        Assert.Throws<WorkerMessageRejectedException>(() =>
            channel.Send(WorkerControlMessage.Execute(Guid.NewGuid(), command)));
        Assert.Equal(0, stream.Length);
        channel.Send(WorkerControlMessage.Stop(Guid.NewGuid()));
        stream.Position = 0;
        Assert.Equal(WorkerControlMessageKind.Stop, channel.Receive().Kind);
    }

    [Fact]
    public void ReceiverRejectsAnUndefinedExecutionStatus()
    {
        var response = new WorkerExecutionResponse((WorkerExecutionResponseStatus)999, null,
            new WorkerConnectionSnapshot(WorkerConnectionState.Connected,
                WorkerExecutionReadinessState.ExecutionReady, 0, 1, 1, "ready", DateTimeOffset.UnixEpoch), null);
        AssertMalformed(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), response));
    }

    [Fact]
    public void ReceiverRejectsAnUndefinedMessageKind() =>
        AssertMalformed(new WorkerControlMessage(WorkerControlProtocol.CurrentVersion,
            (WorkerControlMessageKind)999, Guid.NewGuid()));

    private static void AssertMalformed(WorkerControlMessage message)
    {
        // Bypass the sender's validation to exercise the untrusted receive boundary.
        var payload = JsonSerializer.SerializeToUtf8Bytes(message, JsonSerializerOptions.Web);
        using var stream = new MemoryStream();
        Span<byte> header = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        stream.Write(header);
        stream.Write(payload);
        stream.Position = 0;
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        Assert.Throws<InvalidDataException>(() => channel.Receive());
    }
}
