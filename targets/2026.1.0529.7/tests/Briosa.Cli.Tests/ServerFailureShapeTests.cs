using System.Text.Json.Nodes;

namespace Briosa.Cli.Tests;

/// <summary>
/// Every typed failure the server's outcome mapper can produce for an MP call, as
/// the mapper produces it, through the real CLI: the outcome and exit code of each
/// shape, and the disposition carried through unchanged.
/// </summary>
public sealed class ServerFailureShapeTests
{
    public static TheoryData<string> Shapes()
    {
        var data = new TheoryData<string>();
        foreach (var shape in ServerMapperShapes.All)
        {
            data.Add(shape.Label);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public async Task EachServerFailureShapeMapsToItsOutcome(string label)
    {
        var shape = ServerMapperShapes.All.Single(candidate => candidate.Label == label);
        await using var server = await FakeBriosaServer.StartAsync();
        server.Reply = _ => FakeReply.FromRpcException(shape.Exception);

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server));

        Assert.Equal(shape.Outcome, run.Outcome);
        Assert.Equal(shape.ExitCode, run.ExitCode);
        var error = OperationError.Parser.ParseFrom(shape.Exception.Trailers.Single().ValueBytes);
        Assert.Equal(Name(error.ExecutionDisposition), run.Execution["disposition"]!.GetValue<string>());
        Assert.Equal("operation_error", run.Execution["disposition_source"]!.GetValue<string>());
        Assert.Equal(error.DiagnosticCode, run.DiagnosticCode);
        Assert.Equal("server", run.Envelope["failure"]!["source"]!.GetValue<string>());
        CallOutcomeTests.AssertOneRestrictedMpCall(server);
    }

    [Fact]
    public void TheShapeTableCoversEveryFailureKindTheMapperProduces()
    {
        var kinds = ServerMapperShapes.All
            .Select(shape => OperationError.Parser.ParseFrom(shape.Exception.Trailers.Single().ValueBytes).Kind)
            .ToHashSet();
        var expected = Enum.GetValues<OperationFailureKind>().Where(kind => kind != OperationFailureKind.Unspecified);
        Assert.Equal(expected.Order(), kinds.Order());
    }

    private static string Name(ExecutionDisposition disposition) => disposition switch
    {
        ExecutionDisposition.NotStarted => "EXECUTION_DISPOSITION_NOT_STARTED",
        ExecutionDisposition.StartedOutcomeUnknown => "EXECUTION_DISPOSITION_STARTED_OUTCOME_UNKNOWN",
        ExecutionDisposition.Completed => "EXECUTION_DISPOSITION_COMPLETED",
        _ => JsonValue.Create((int)disposition).ToJsonString()
    };
}
