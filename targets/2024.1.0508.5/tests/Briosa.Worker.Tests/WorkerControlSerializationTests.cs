using System.Buffers.Binary;
using System.Text.Json;
using Briosa.Worker.Control;

namespace Briosa.Worker.Tests;

public sealed class WorkerControlSerializationTests
{
    [Fact]
    public async Task CompactEnvelopePreservesZeroFalseAndEmptyValues()
    {
        var request = WorkerControlMessage.Execute(Guid.NewGuid(), new WorkerMpCommand(
            "serialization", "Serialization",
            [
                new("Logical", WorkerMpValueKind.Logical, BooleanValue: false),
                new("Integer", WorkerMpValueKind.WholeNumber, IntegerValue: 0),
                new("Double", WorkerMpValueKind.FloatingPoint, DoubleValue: 0),
                new("Text", WorkerMpValueKind.Text, StringValue: ""),
                new("List", WorkerMpValueKind.DoubleArray, DoubleArrayValue: new([]))
            ], []));
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        await channel.SendAsync(request, CancellationToken.None);
        var frame = stream.ToArray();
        Assert.Equal(frame.Length - sizeof(int), BinaryPrimitives.ReadInt32LittleEndian(frame));
        using var document = JsonDocument.Parse(frame.AsMemory(sizeof(int)));
        var root = document.RootElement;
        Assert.False(root.TryGetProperty("executionResponse", out _));
        Assert.Equal((int)WorkerControlMessageKind.Execute, root.GetProperty("kind").GetInt32());
        var inputs = root.GetProperty("command").GetProperty("inputArguments");
        Assert.False(inputs[0].TryGetProperty("doubleValue", out _));
        Assert.False(inputs[0].GetProperty("booleanValue").GetBoolean());
        Assert.Equal(0, inputs[1].GetProperty("integerValue").GetInt32());
        Assert.Equal(0, inputs[2].GetProperty("doubleValue").GetDouble());
        Assert.Equal("", inputs[3].GetProperty("stringValue").GetString());
        Assert.Empty(inputs[4].GetProperty("doubleArrayValue").GetProperty("values").EnumerateArray());
        stream.Position = 0;
        var decoded = await channel.ReceiveAsync(CancellationToken.None);
        Assert.Equal(request.CorrelationId, decoded.CorrelationId);
        Assert.Equal(false, decoded.Command!.InputArguments[0].BooleanValue);
        Assert.Equal(0, decoded.Command.InputArguments[1].IntegerValue);
        Assert.Equal(0, decoded.Command.InputArguments[2].DoubleValue);
        Assert.Equal("", decoded.Command.InputArguments[3].StringValue);
        Assert.Empty(decoded.Command.InputArguments[4].DoubleArrayValue!.Values);
    }

    [Fact]
    public void CompactEncodingStillRejectsOversizedMessagesBeforeWriting()
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        var request = WorkerControlMessage.Execute(Guid.NewGuid(), new WorkerMpCommand(
            "serialization", "Serialization",
            [new("Text", WorkerMpValueKind.Text, StringValue: new string('x', WorkerControlProtocol.MaximumMessageBytes))], []));
        Assert.Throws<WorkerMessageRejectedException>(() => channel.Send(request));
        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public void PreviousProtocolIsRejectedBeforeWriting()
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        var previous = WorkerControlMessage.Ping(Guid.NewGuid()) with
        {
            ProtocolVersion = WorkerControlProtocol.CurrentVersion - 1
        };
        Assert.Throws<WorkerMessageRejectedException>(() => channel.Send(previous));
        Assert.Equal(0, stream.Length);
    }
}
