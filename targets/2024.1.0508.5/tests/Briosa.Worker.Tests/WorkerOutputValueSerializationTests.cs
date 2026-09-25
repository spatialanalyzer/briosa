using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Nodes;
using Briosa.Worker.Control;

namespace Briosa.Worker.Tests;

public sealed class WorkerOutputValueSerializationTests
{
    [Fact]
    public void EveryOutputFamilyRoundTripsThroughItsExplicitValueAlternative()
    {
        var limit = new WorkerToleranceLimit(true, 1);
        var transform = new WorkerTransformValue(Enumerable.Range(0, 16).Select(value => (double)value).ToArray());
        (WorkerMpValueKind Kind, WorkerMpValue Value)[] values =
        [
            (WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
            (WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(0)),
            (WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0)),
            (WorkerMpValueKind.Text, new WorkerTextValue("")),
            (WorkerMpValueKind.PointName, new WorkerPointNameValue("C", "G", "P")),
            (WorkerMpValueKind.Vector, new WorkerVectorValue(1, 2, 3)),
            (WorkerMpValueKind.ToleranceVectorOptions, new WorkerToleranceVectorOptionsValue(limit, limit, limit, limit, limit, limit, limit, limit)),
            (WorkerMpValueKind.CollectionInstrumentId, new WorkerCollectionInstrumentIdValue("C", 1)),
            (WorkerMpValueKind.CollectionInstrumentIdList, new WorkerCollectionInstrumentIdListValue([new("C", 1)])),
            (WorkerMpValueKind.CollectionMachineId, new WorkerCollectionMachineIdValue("C", 1)),
            (WorkerMpValueKind.CollectionItemName, new WorkerCollectionItemNameValue("C", "I", WorkerItemTypeValue.Relationship)),
            (WorkerMpValueKind.CollectionItemNameList, new WorkerCollectionItemNameListValue([new("C", "I", WorkerItemTypeValue.Relationship)])),
            (WorkerMpValueKind.CollectionObjectName, new WorkerCollectionObjectNameValue("C", "O", WorkerObjectTypeValue.Frame)),
            (WorkerMpValueKind.CollectionObjectNameList, new WorkerCollectionObjectNameListValue([new("C", "O", WorkerObjectTypeValue.Frame)])),
            (WorkerMpValueKind.CollectionGroupNameList, new WorkerCollectionGroupNameListValue([new("C", "G")])),
            (WorkerMpValueKind.CollectionVectorGroupName, new WorkerCollectionVectorGroupNameValue("C", "G")),
            (WorkerMpValueKind.CollectionVectorGroupNameList, new WorkerCollectionVectorGroupNameListValue([new("C", "G")])),
            (WorkerMpValueKind.PointNameList, new WorkerPointNameListValue([new("C", "G", "P")])),
            (WorkerMpValueKind.StringList, new WorkerStringListValue([])),
            (WorkerMpValueKind.EditText, new WorkerStringListValue(["", "text"])),
            (WorkerMpValueKind.VectorNameList, new WorkerVectorNameListValue([new("C", "G", "V")])),
            (WorkerMpValueKind.DoubleArray, new WorkerDoubleArrayValue([])),
            (WorkerMpValueKind.Transform, transform),
            (WorkerMpValueKind.WorldTransform, new WorkerWorldTransformValue(transform, 1)),
            (WorkerMpValueKind.FileReference, new WorkerFileReferenceValue("example", false)),
            (WorkerMpValueKind.FitConstraintScalarOptions, new WorkerFitConstraintScalarOptionsValue(limit, limit)),
            (WorkerMpValueKind.ToleranceScalarOptions, new WorkerToleranceScalarOptionsValue(limit, limit))
        ];

        foreach (var (kind, value) in values)
        {
            using var stream = new MemoryStream();
            using var channel = new WorkerControlChannel(stream, leaveOpen: true);
            channel.Send(Message(new WorkerRetrievedOutput("Value", kind, value, "local-only")));
            var original = stream.ToArray();
            stream.Position = 0;
            var decoded = channel.Receive();
            var output = Assert.IsType<WorkerRetrievedOutput>(Assert.Single(decoded.ExecutionResponse!.Execution!.OutputValues));
            Assert.Equal(value.GetType(), output.Value.GetType());
            Assert.Null(output.DiagnosticCode);
            stream.SetLength(0);
            stream.Position = 0;
            channel.Send(decoded);
            Assert.Equal(original, stream.ToArray());
        }
    }

    [Theory]
    [InlineData("unavailable-with-value")]
    [InlineData("missing-value")]
    [InlineData("wrong-kind")]
    [InlineData("unknown-value")]
    [InlineData("extra-payload")]
    [InlineData("null-output")]
    [InlineData("unknown-kind")]
    public void ContradictoryOrUnknownWirePayloadsFailClosed(string mutation)
    {
        using var encoded = new MemoryStream();
        using var writer = new WorkerControlChannel(encoded, leaveOpen: true);
        writer.Send(Message(new WorkerRetrievedOutput("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1))));
        var document = JsonNode.Parse(encoded.ToArray().AsSpan(sizeof(int)))!;
        var output = document["executionResponse"]!["execution"]!["outputs"]![0]!;
        switch (mutation)
        {
            case "unavailable-with-value": output["retrieval"] = "unavailable"; break;
            case "missing-value": output.AsObject().Remove("value"); break;
            case "wrong-kind": output["kind"] = (int)WorkerMpValueKind.Text; break;
            case "unknown-value": output["value"]!["valueType"] = 9999; break;
            case "null-output": document["executionResponse"]!["execution"]!["outputs"]![0] = null; break;
            case "unknown-kind": output["retrieval"] = "unavailable"; output.AsObject().Remove("value"); output["kind"] = 9999; break;
            case "extra-payload": output["value"]!["stringValue"] = "contradiction"; break;
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

    [Fact]
    public void OutputConstructionRejectsContradictorySdkEvidence()
    {
        Assert.Throws<ArgumentNullException>(() => new WorkerRetrievedOutput("Value", WorkerMpValueKind.Text, null!));
        Assert.Throws<ArgumentException>(() => new WorkerRetrievedOutput("Value", WorkerMpValueKind.Text, new WorkerDoubleValue(1)));
        Assert.Throws<ArgumentException>(() => WorkerMpOutputValue.FromRetrieval("Value", WorkerMpValueKind.Text, true, null));
        Assert.Throws<ArgumentException>(() => WorkerMpOutputValue.FromRetrieval("Value", WorkerMpValueKind.Text, false, new WorkerTextValue("")));
    }

    private static WorkerControlMessage Message(WorkerMpOutputValue output) =>
        WorkerControlMessage.ExecutionResult(Guid.NewGuid(), new(
            WorkerExecutionResponseStatus.Completed, new WorkerMpResultAvailable(2, 1, [output], null),
            new(WorkerConnectionState.Disconnected, WorkerExecutionReadinessState.Unverified,
                null, 0, 1, "disconnected", DateTimeOffset.UnixEpoch), null));
}
