using System.Text;
using Briosa.Cli.Engine;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Briosa.Cli.Tests;

/// <summary>
/// Each strict input rule, through the real <c>call</c> path: the CLI refuses before
/// sending any MP RPC, reports "not started" with the schema path, and never echoes
/// the offending text.
/// </summary>
public sealed class StrictInputCommandTests
{
    private const string Marker = "zq9MARKERzq9";

    public static TheoryData<string, string, string?, string?> Rejections() => new()
    {
        // json, code, field path, suggested field name
        { """{"groupName":{}}""", Codes.FieldNameNotExact, null, "group_name" },
        { """{"group_name":{"objectName":"x"}}""", Codes.FieldNameNotExact, "group_name", "object_name" },
        { """{"point_index":null}""", Codes.Null, "point_index", null },
        { """{"group_name":{"object_name":null}}""", Codes.Null, "group_name.object_name", null },
        { """{"point_index":1,"point_index":2}""", Codes.DuplicateField, "point_index", null },
        { $$$"""{"group_name":{"object_name":"{{{Marker}}}","object_name":"b"}}""", Codes.DuplicateField, "group_name.object_name", null },
        { $$"""{"{{Marker}}":1}""", Codes.UnknownField, null, null },
        { $$$"""{"group_name":{"{{{Marker}}}":true}}""", Codes.UnknownField, "group_name", null },
        { """{"point_index":"1"}""", Codes.NumberForm, "point_index", null },
        { """{"point_index":1.0}""", Codes.NumberForm, "point_index", null },
        { """{"point_index":1e2}""", Codes.NumberForm, "point_index", null },
        { """{"point_index":2147483648}""", Codes.OutOfRange, "point_index", null },
        { """{"group_name":{"object_type":18}}""", Codes.EnumNameRequired, "group_name.object_type", null },
        { $$$"""{"group_name":{"object_type":"{{{Marker}}}"}}""", Codes.UnknownEnumName, "group_name.object_type", null },
        { $$"""{"group_name":"{{Marker}}"}""", Codes.TypeMismatch, "group_name", null },
        { """{"group_name":{"object_name":true}}""", Codes.TypeMismatch, "group_name.object_name", null },
        { """{"point_index":true}""", Codes.TypeMismatch, "point_index", null },
        { """{"group_name":{"object_name":"\uD800"}}""", Codes.InvalidString, "group_name.object_name", null },
        { "[]", Codes.RootNotObject, null, null },
        { "\"text\"", Codes.RootNotObject, null, null },
        { "", Codes.MalformedJson, null, null },
        { $$"""{"point_index":{{Marker}}""", Codes.MalformedJson, null, null },
        { """{"point_index":1} {}""", Codes.MalformedJson, null, null },
        { """{/* comment */}""", Codes.MalformedJson, null, null },
        { """{"point_index":1,}""", Codes.MalformedJson, null, null },
        { """{'point_index':1}""", Codes.MalformedJson, null, null }
    };

    [Theory]
    [MemberData(nameof(Rejections))]
    public async Task EachRuleRejectsLocallyBeforeAnyMpCall(string json, string code, string? path, string? suggestion)
    {
        ArgumentNullException.ThrowIfNull(json);
        await using var server = await FakeBriosaServer.StartAsync();

        var run = await CliRun.RunAsync(["call", CallOutcomeTests.Operation, "--json", json, "--endpoint", server.Endpoint]);

        AssertRejected(run, server, code);
        var input = run.Envelope["failure"]!["input"]!;
        Assert.Equal(path, input["field_path"]?.GetValue<string>());
        Assert.Equal(suggestion, input["suggested_field_name"]?.GetValue<string>());
        if (code != Codes.MalformedJson || json.Length > 0)
        {
            Assert.NotNull(input["line"]);
            Assert.NotNull(input["column"]);
        }
    }

    [Fact]
    public async Task ALoneSurrogateInCommandLineTextIsRejectedNotReplaced()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var json = "{\"group_name\":{\"object_name\":\"" + (char)0xD800 + "\"}}";

        var run = await CliRun.RunAsync(["call", CallOutcomeTests.Operation, "--json", json, "--endpoint", server.Endpoint]);

        Assert.Equal(3, run.ExitCode);
        Assert.Equal(Codes.InvalidString, run.DiagnosticCode);
        Assert.Empty(server.Calls);
    }

    [Fact]
    public async Task AnInputOverTheSizeLimitIsRejected()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var json = "{\"group_name\":{\"object_name\":\"" + new string('x', Wire.MaximumInputBytes) + "\"}}";

        var run = await CliRun.RunAsync(["call", CallOutcomeTests.Operation, "--json", json, "--endpoint", server.Endpoint]);

        AssertRejected(run, server, Codes.TooLarge);
    }

    [Fact]
    public async Task AnInputFileOverTheSizeLimitIsRejectedWithoutReadingItAll()
    {
        using var environment = TestEnvironment.Create();
        await using var server = await FakeBriosaServer.StartAsync();
        var path = Path.Combine(environment.Root, "large.json");
        await File.WriteAllBytesAsync(path, new byte[Wire.MaximumInputBytes + 1]);

        var run = await CliRun.RunAsync(["call", CallOutcomeTests.Operation, "--input", path, "--endpoint", server.Endpoint]);

        AssertRejected(run, server, Codes.TooLarge);
    }

    [Fact]
    public async Task AnEncodedRequestOverTheServerLimitIsRejected()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var names = string.Join(',', Enumerable.Range(0, 2000).Select(index =>
            $$"""{"collection_name":"collection-{{index:D6}}","group_name":"group-{{index:D6}}","target_name":"t"}"""));
        var json = $$$"""{"point_names":[{{{names}}}],"single_point":{"target_name":"t"}}""";
        Assert.True(Encoding.UTF8.GetByteCount(json) < Wire.MaximumInputBytes);

        var run = await CliRun.RunAsync(["call", "analysis_operations.query_points_to_single_point", "--json", json,
            "--endpoint", server.Endpoint]);

        AssertRejected(run, server, Codes.EncodedRequestTooLarge);
    }

    [Fact]
    public async Task AnInputFileIsReadWithOrWithoutAByteOrderMark()
    {
        using var environment = TestEnvironment.Create();
        await using var server = await FakeBriosaServer.StartAsync();
        server.Reply = _ => FakeReply.Ok(CallOutcomeTests.Result(MpExecutionState.Succeeded, 2));
        var path = Path.Combine(environment.Root, "request.json");
        await File.WriteAllBytesAsync(path, [.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes(CallOutcomeTests.Request)]);

        var run = await CliRun.RunAsync(["call", CallOutcomeTests.Operation, "--input", path, "--endpoint", server.Endpoint]);

        Assert.Equal(0, run.ExitCode);
        CallOutcomeTests.AssertOneRestrictedMpCall(server);
    }

    [Fact]
    public async Task InvalidUtf8AndUnreadableFilesAreRejected()
    {
        using var environment = TestEnvironment.Create();
        await using var server = await FakeBriosaServer.StartAsync();
        var path = Path.Combine(environment.Root, "invalid.json");
        await File.WriteAllBytesAsync(path, [.. "{\"group_name\":{\"object_name\":\""u8.ToArray(), 0xFF, 0xFE, .. "\"}}"u8.ToArray()]);

        var invalid = await CliRun.RunAsync(["call", CallOutcomeTests.Operation, "--input", path, "--endpoint", server.Endpoint]);
        var missing = await CliRun.RunAsync(["call", CallOutcomeTests.Operation, "--input",
            Path.Combine(environment.Root, Marker + ".json"), "--endpoint", server.Endpoint]);

        AssertRejected(invalid, server, Codes.InvalidString);
        AssertRejected(missing, server, "input-file-unreadable");
    }

    [Fact]
    public void NestingBeyondTheLimitIsRejectedWithItsPath()
    {
        var node = RecursiveNode();
        var within = Nest(Wire.MaximumDepth);
        var beyond = Nest(Wire.MaximumDepth + 1);

        Assert.Null(StrictJsonInput.Validate(Encoding.UTF8.GetBytes(within), node));
        var rejection = StrictJsonInput.Validate(Encoding.UTF8.GetBytes(beyond), node);
        Assert.NotNull(rejection);
        Assert.Equal(Codes.TooDeep, rejection.Code);
        Assert.Equal(string.Join('.', Enumerable.Repeat("child", Wire.MaximumDepth)), rejection.FieldPath);

        static string Nest(int levels) =>
            string.Concat(Enumerable.Repeat("{\"child\":", levels - 1)) + "{}" + new string('}', levels - 1);
    }

    [Fact]
    public async Task DeeplyNestedJsonUnderAnyFieldIsRejectedWithoutRecursion()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var json = "{\"group_name\":" + new string('[', 10_000) + new string(']', 10_000) + "}";

        var run = await CliRun.RunAsync(["call", CallOutcomeTests.Operation, "--json", json, "--endpoint", server.Endpoint]);

        AssertRejected(run, server, Codes.TypeMismatch);
    }

    private static void AssertRejected(CliRun run, FakeBriosaServer server, string code)
    {
        Assert.Equal(3, run.ExitCode);
        Assert.Equal("rejected_locally", run.Outcome);
        Assert.Equal(code, run.DiagnosticCode);
        Assert.Equal("cli", run.Envelope["failure"]!["source"]!.GetValue<string>());
        Assert.False(run.Execution["request_sent"]!.GetValue<bool>());
        Assert.Equal("EXECUTION_DISPOSITION_NOT_STARTED", run.Execution["disposition"]!.GetValue<string>());
        Assert.Equal("cli", run.Execution["disposition_source"]!.GetValue<string>());
        Assert.Equal(CallOutcomeTests.ExecutionMembers, run.Execution.Select(member => member.Key).ToArray());
        Assert.Empty(server.MpCalls);
        Assert.DoesNotContain(Marker, run.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Marker, run.StandardError, StringComparison.Ordinal);
    }

    // message Node { Node child = 1; }, built at run time: no registered message nests
    // deep enough to reach the limit.
    private static MessageDescriptor RecursiveNode()
    {
        var file = new FileDescriptorProto { Name = "test/node.proto", Package = "test", Syntax = "proto3" };
        file.MessageType.Add(new DescriptorProto
        {
            Name = "Node",
            Field =
            {
                new FieldDescriptorProto
                {
                    Name = "child",
                    JsonName = "child",
                    Number = 1,
                    Type = FieldDescriptorProto.Types.Type.Message,
                    TypeName = ".test.Node",
                    Label = FieldDescriptorProto.Types.Label.Optional
                }
            }
        });
        return FileDescriptor.BuildFromByteStrings([file.ToByteString()])[0].MessageTypes[0];
    }
}
