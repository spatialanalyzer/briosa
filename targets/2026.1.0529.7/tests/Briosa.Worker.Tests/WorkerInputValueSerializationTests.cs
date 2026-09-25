using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Nodes;
using Briosa.Worker.Control;

namespace Briosa.Worker.Tests;

public sealed class WorkerInputValueSerializationTests
{
    [Fact]
    public void InputOwnsExactlyOneMatchingValue()
    {
        Assert.Throws<ArgumentNullException>(() => new WorkerMpInputArgument("Value", WorkerMpValueKind.Text, null!));
        Assert.Throws<ArgumentException>(() => new WorkerMpInputArgument("Value", WorkerMpValueKind.Text, new WorkerDoubleValue(1)));
        var value = new WorkerDoubleArrayValue([1, 2, 3]);
        var input = new WorkerMpInputArgument("Value", WorkerMpValueKind.DoubleArray, value);
        Assert.Same(value, input.Value);
        Assert.Same(value, input.RequireValue<WorkerDoubleArrayValue>());
    }

    [Theory]
    [InlineData("missing-value")]
    [InlineData("wrong-kind")]
    [InlineData("unknown-value")]
    [InlineData("extra-payload")]
    [InlineData("null-argument")]
    public void InvalidWireInputCannotBypassTheTypedValueBoundary(string mutation)
    {
        using var encoded = new MemoryStream();
        using var writer = new WorkerControlChannel(encoded, leaveOpen: true);
        writer.Send(WorkerControlMessage.Execute(Guid.NewGuid(), new("test", "Test",
            [new("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0))], [])));
        var document = JsonNode.Parse(encoded.ToArray().AsSpan(sizeof(int)))!;
        var input = document["command"]!["inputArguments"]![0]!;
        switch (mutation)
        {
            case "missing-value": input.AsObject().Remove("value"); break;
            case "wrong-kind": input["kind"] = (int)WorkerMpValueKind.Text; break;
            case "unknown-value": input["value"]!["valueType"] = 9999; break;
            case "null-argument": document["command"]!["inputArguments"]![0] = null; break;
            case "extra-payload": input["stringValue"] = "contradiction"; break;
        }
        var payload = JsonSerializer.SerializeToUtf8Bytes(document);
        using var stream = new MemoryStream();
        Span<byte> header = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        stream.Write(header);
        stream.Write(payload);
        stream.Position = 0;
        using var reader = new WorkerControlChannel(stream);
        Assert.Throws<InvalidDataException>(() => reader.Receive());
    }
}
