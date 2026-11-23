using System.Buffers;
using System.Text;
using System.Text.Json;
using Briosa.Worker.Control;

namespace Briosa.Worker.Tests;

// The private channel encodes into a buffer bounded by the frame budget and writes
// long strings in segments. These tests show that the encoded bytes are unchanged
// and that an oversized result is rejected without being encoded in full.
public sealed class WorkerControlEncodingTests
{
    private static readonly Guid CorrelationId = new("1b2c3d4e-5f60-4718-9a2b-3c4d5e6f7081");
    private const string Placeholder = "encoding-test-placeholder";

    // Fewer, equal and more characters than one segment; characters that need
    // escaping; non-ASCII; surrogate pairs, one of them split by a segment
    // boundary; control characters.
    public static TheoryData<string> Strings()
    {
        var mixed = new StringBuilder();
        for (var index = 0; mixed.Length < 9_000; index++)
        {
            mixed.Append(((index * 7 + 3) % 6) switch
            {
                0 => "plain text ",
                1 => "<&>'\"+`",
                2 => "\u00e9\u00e8\u4e2d\u6587",
                3 => "\ud83d\ude00",
                4 => "\u0001\t\n\\",
                _ => "0123456789"
            });
        }

        return new TheoryData<string>
        {
            string.Empty,
            new string('a', 1_023),
            new string('a', 1_024),
            new string('a', 1_025),
            new string('x', 100_000),
            new string('<', 5_000),
            new string('\u00e9', 3_000),
            new string('a', 1_023) + "\ud83d\ude00" + new string('b', 2_000),
            string.Concat(Enumerable.Repeat("\ud83d\ude00", 2_000)),
            new string('\u0001', 2_000),
            mixed.ToString()
        };
    }

    [Theory]
    [MemberData(nameof(Strings))]
    public void AStringEncodesExactlyAsASingleStringWriteAndRoundTrips(string value)
    {
        var encoded = Payload(Result(value));
        var expected = Splice(Payload(Result(Placeholder)), Token(Placeholder), Token(value));

        Assert.Equal(expected, encoded);
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        channel.Send(Result(value));
        stream.Position = 0;
        var output = Assert.IsType<WorkerRetrievedOutput>(
            Assert.Single(channel.Receive().ExecutionResponse!.Execution!.OutputValues));
        Assert.Equal(value, Assert.IsType<WorkerTextValue>(output.Value).Value);
    }

    // Results far beyond the response budget. Encoding each in full would
    // allocate tens of megabytes; the bounded encoder stops at the budget.
    public static TheoryData<string> OversizedResults() =>
        new() { "text", "escaped-text", "double-list", "string-list" };

    [Theory]
    [MemberData(nameof(OversizedResults))]
    public void AnOversizedResultIsRejectedWithoutBeingEncodedInFull(string shape)
    {
        var output = OversizedOutput(shape);
        var message = WorkerControlMessage.ExecutionResult(CorrelationId, Response(
            new WorkerMpResultAvailable(2, 5, [output], null)));
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        // The first rejection fills the shared pool; the second is measured.
        Assert.Throws<WorkerMessageRejectedException>(() => channel.Send(message));

        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<WorkerMessageRejectedException>(() => channel.Send(message));
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, stream.Length);
        Assert.InRange(allocated, 0, BoundedFrameBudgetBytes);
    }

    // The SDK-fault fallback measures each output on its own. An oversized output
    // is measured only up to the budget, then marked unavailable; the others keep
    // their values and statuses.
    [Fact]
    public void TheSdkFaultFallbackMeasuresAnOversizedOutputOnlyUpToTheBudget()
    {
        var fault = new WorkerSdkCallFaulted(
            WorkerSdkCallPhase.OutputGetter,
            5,
            [
                OversizedOutput("text"),
                new WorkerRetrievedOutput("Small", WorkerMpValueKind.Text, new WorkerTextValue("ok")),
                new WorkerUnavailableOutput("Count", WorkerMpValueKind.WholeNumber,
                    WorkerSdkFaultDiagnosticCodes.OutputGetter, WorkerUnavailableOutputReason.SdkCallFaulted)
            ],
            WorkerSdkFaultDiagnosticCodes.OutputGetter,
            resultCode: 2);
        var response = Response(fault);
        _ = WorkerExecutionDelivery.Undeliverable(CorrelationId, response);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var replacement = WorkerExecutionDelivery.Undeliverable(CorrelationId, response);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.InRange(allocated, 0, BoundedFrameBudgetBytes);
        var delivered = Assert.IsType<WorkerSdkCallFaulted>(replacement.ExecutionResponse!.Execution);
        Assert.Equal(2, delivered.ResultCode);
        Assert.Collection(delivered.Outputs,
            first => Assert.Equal(WorkerUnavailableOutputReason.EncodingRejected,
                Assert.IsType<WorkerUnavailableOutput>(first).Reason),
            second => Assert.Equal("ok", Assert.IsType<WorkerTextValue>(
                Assert.IsType<WorkerRetrievedOutput>(second).Value).Value),
            third => Assert.Equal(WorkerUnavailableOutputReason.SdkCallFaulted,
                Assert.IsType<WorkerUnavailableOutput>(third).Reason));
    }

    // The new allocation a measured rejection may make, with a warm buffer pool:
    // well under one budget. A full encoding of these results would allocate more
    // than ten times the budget.
    private const long BoundedFrameBudgetBytes = 512 * 1024;

    private static WorkerRetrievedOutput OversizedOutput(string shape) => shape switch
    {
        "text" => new WorkerRetrievedOutput("Value", WorkerMpValueKind.Text,
            new WorkerTextValue(new string('x', 20_000_000))),
        "escaped-text" => new WorkerRetrievedOutput("Value", WorkerMpValueKind.Text,
            new WorkerTextValue(new string('<', 3_000_000))),
        "double-list" => new WorkerRetrievedOutput("Value", WorkerMpValueKind.DoubleArray,
            new WorkerDoubleArrayValue(new double[5_000_000])),
        "string-list" => new WorkerRetrievedOutput("Value", WorkerMpValueKind.StringList,
            new WorkerStringListValue(Enumerable.Repeat("ab", 2_000_000).ToArray())),
        _ => throw new ArgumentOutOfRangeException(nameof(shape))
    };

    private static WorkerControlMessage Result(string text) =>
        WorkerControlMessage.ExecutionResult(CorrelationId, Response(new WorkerMpResultAvailable(2, 5,
            [new WorkerRetrievedOutput("Value", WorkerMpValueKind.Text, new WorkerTextValue(text))], null)));

    private static WorkerExecutionResponse Response(WorkerMpExecutionResult execution) =>
        new(WorkerExecutionResponseStatus.Completed, execution,
            new WorkerConnectionSnapshot(
                WorkerConnectionState.Connected,
                WorkerExecutionReadinessState.ExecutionReady,
                StatusCode: 0,
                Attempt: 1,
                MaximumAttempts: 1,
                DiagnosticCode: "sdk-connected",
                DateTimeOffset.UnixEpoch),
            DiagnosticCode: null);

    private static byte[] Payload(WorkerControlMessage message)
    {
        using var stream = new MemoryStream();
        using (var channel = new WorkerControlChannel(stream, leaveOpen: true))
        {
            channel.Send(message);
        }

        return stream.ToArray()[sizeof(int)..];
    }

    // The JSON token a single default string write produces.
    private static byte[] Token(string value)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStringValue(value);
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static byte[] Splice(byte[] payload, byte[] placeholder, byte[] replacement)
    {
        var index = payload.AsSpan().IndexOf(placeholder);
        Assert.True(index >= 0);
        return [.. payload[..index], .. replacement, .. payload[(index + placeholder.Length)..]];
    }
}
