using System.Text;
using Briosa.Cli.Engine;
using Grpc.Core;

namespace Briosa.Cli.Tests;

/// <summary>Findings of the first independent review, each pinned through the real CLI.</summary>
public sealed class ReviewRoundOneTests
{
    private const string Operation = "analysis_operations.get_ith_point_from_group";

    // Finding 2: one call, one submission, whatever the HTTP stack would do.
    [Fact]
    public async Task ARefusedStreamIsNeverSubmittedAgain()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var attempts = 0;
        server.Reply = _ => Interlocked.Increment(ref attempts) == 1
            ? FakeReply.RefuseStream()
            : FakeReply.Ok(CallOutcomeTests.Result(MpExecutionState.Succeeded, 2));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server));

        Assert.Equal(1, Volatile.Read(ref attempts));
        Assert.Single(server.MpCalls);
        Assert.Equal(10, run.ExitCode);
        Assert.Equal("completion_evidence_unavailable", run.Outcome);
        Assert.Equal("request-resend-blocked", run.DiagnosticCode);
        Assert.Contains("request-resend-blocked", CallOutcomeTests.Warnings(run));
        Assert.Null(run.Execution["disposition"]);
    }

    // Finding 5: a failed delivery never claims a completion the evidence did not establish.
    [Fact]
    public async Task AFailedSpillOfAnUntrustedResultDoesNotClaimCompletion()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        using var environment = TestEnvironment.Create(new FailingWriteFileSystem(_ => true));
        server.Reply = _ => FakeReply.Ok(CallOutcomeTests.Result(MpExecutionState.Succeeded, 4));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--max-inline-bytes", "0"),
            environment.Environment);

        Assert.Equal(10, run.ExitCode);
        Assert.Equal("completion_evidence_unavailable", run.Outcome);
        Assert.Equal("result-execution-contract-violation", run.DiagnosticCode);
        Assert.Null(run.Execution["disposition"]);
        Assert.Equal("lost", run.Data["result_delivery"]!.GetValue<string>());
        Assert.Equal("spill-write-failed", run.Data["result_delivery_error"]!.GetValue<string>());
        var text = string.Join(' ', CallOutcomeTests.Guidance(run)) + " " +
            string.Join(' ', run.Envelope["warnings"]!.AsArray().Select(warning => warning!["message"]!.GetValue<string>()));
        Assert.DoesNotContain("completed", text, StringComparison.OrdinalIgnoreCase);
    }

    // Finding 7: only locally recognized codes reach standard error.
    [Fact]
    public async Task AServerDiagnosticCodeNeverReachesStandardError()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var error = CallOutcomeTests.Error(OperationFailureKind.PolicyDenied, "customer-part-17",
            ExecutionDisposition.NotStarted, ReplayGuidance.DoNotReplay);
        error.PolicyDenial = new PolicyDenialDetails { Rule = PolicyDenialRule.Override };
        server.Reply = _ => FakeReply.Error(StatusCode.PermissionDenied, error);

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server));

        Assert.Equal(6, run.ExitCode);
        Assert.Equal("customer-part-17", run.DiagnosticCode);
        Assert.DoesNotContain("customer-part-17", run.StandardError, StringComparison.Ordinal);
        Assert.Equal("briosa-cli: not_started (server-diagnostic)" + Environment.NewLine, run.StandardError);
    }

    [Fact]
    public async Task ALocallyRecognizedCodeStillReachesStandardError()
    {
        await using var server = await FakeBriosaServer.StartAsync();

        var run = await CliRun.RunAsync(["call", Operation, "--json", "{\"point_index\":null}", "--endpoint", server.Endpoint]);

        Assert.Equal("briosa-cli: rejected_locally (input-null)" + Environment.NewLine, run.StandardError);
    }

    // Finding 8: an inconsistent listing is refused, never resolved by guessing.
    [Theory]
    [InlineData("call")]
    [InlineData("describe")]
    [InlineData("search")]
    [InlineData("categories")]
    public async Task DuplicateOperationIdsInDiscoveryAreRefused(string command)
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var capabilities = ServerProjection.AllCapabilities;
        var impostor = capabilities.Operations.Single(operation =>
            operation.OperationId == "file_operations.get_working_directory").Clone();
        impostor.OperationId = Operation;
        capabilities.Operations.Insert(0, impostor);
        server.AllCapabilities = capabilities;

        var run = await CliRun.RunAsync(Arguments(command, server));

        Assert.Equal(5, run.ExitCode);
        Assert.Equal("server_incompatible", run.Outcome);
        Assert.Equal("capabilities-inconsistent", run.DiagnosticCode);
        Assert.Empty(server.MpCalls);
    }

    [Fact]
    public async Task ADiscoveryRowWhoseMethodDisagreesWithItsServiceIsRefused()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var capabilities = ServerProjection.AllCapabilities;
        capabilities.Operations.Single(operation => operation.OperationId == Operation).Rpc = "GetWorkingDirectory";
        server.AllCapabilities = capabilities;

        var run = await CliRun.RunAsync(Arguments("search", server));

        Assert.Equal(5, run.ExitCode);
        Assert.Equal("capabilities-inconsistent", run.DiagnosticCode);
    }

    [Fact]
    public async Task AnOversizedDiscoveryReplyIsReportedForWhatItIs()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var capabilities = ServerProjection.AllCapabilities;
        capabilities.Operations[0].MpStep = new string('x', 5 * 1024 * 1024);
        server.AllCapabilities = capabilities;

        var run = await CliRun.RunAsync(Arguments("search", server));

        Assert.Equal(5, run.ExitCode);
        Assert.Equal("server_incompatible", run.Outcome);
        Assert.Equal("discovery-response-too-large", run.DiagnosticCode);
        Assert.Equal("RESOURCE_EXHAUSTED", run.Envelope["transport"]!["status"]!.GetValue<string>());
        Assert.DoesNotContain(CallOutcomeTests.Guidance(run), text => text.Contains("Start", StringComparison.Ordinal));
    }

    // Finding 9: a bad output path fails before any RPC, and a refused call says nothing was sent.
    [Fact]
    public async Task AnExistingOutputFileFailsBeforeAnyRpc()
    {
        using var environment = TestEnvironment.Create();
        await using var server = await FakeBriosaServer.StartAsync();
        var output = Path.Combine(environment.Root, "existing.json");
        await File.WriteAllTextAsync(output, "keep");

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--output", output), environment.Environment);

        Assert.Equal(3, run.ExitCode);
        Assert.Equal("output-exists", run.DiagnosticCode);
        Assert.Empty(server.Calls);
    }

    [Fact]
    public async Task AReservedOutputIsRemovedWhenTheCallIsRefusedLater()
    {
        using var environment = TestEnvironment.Create();
        await using var server = await FakeBriosaServer.StartAsync();
        server.ServerInfo = FakeBriosaServer.DefaultServerInfo(revision: 1);
        var output = Path.Combine(environment.Root, "never.json");

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--output", output), environment.Environment);

        Assert.Equal(5, run.ExitCode);
        Assert.False(File.Exists(output));
    }

    [Theory]
    [InlineData("--endpoint", "http://localhost:50051", 3)]
    [InlineData("--deadline-seconds", "0", 2)]
    [InlineData("--unknown", "x", 2)]
    public async Task ACallRefusedBeforeAnyRpcCarriesTheNotSentExecution(string option, string value, int exitCode)
    {
        var run = await CliRun.RunAsync(["call", Operation, "--json", "{}", option, value]);

        Assert.Equal(exitCode, run.ExitCode);
        Assert.Equal("call", run.Envelope["command"]!.GetValue<string>());
        Assert.False(run.Execution["request_sent"]!.GetValue<bool>());
        Assert.Equal("EXECUTION_DISPOSITION_NOT_STARTED", run.Execution["disposition"]!.GetValue<string>());
        Assert.Equal("cli", run.Execution["disposition_source"]!.GetValue<string>());
    }

    // Finding 10: the normalized endpoint is one the CLI accepts again.
    [Theory]
    [InlineData("http://127.0.0.1:80", "http://127.0.0.1:80")]
    [InlineData("http://[::1]:80", "http://[::1]:80")]
    public void TheNormalizedEndpointKeepsItsPort(string text, string normalized)
    {
        Assert.True(LoopbackEndpoint.TryParse(text, out var endpoint));
        Assert.Equal(normalized, endpoint!.Text);
        Assert.True(LoopbackEndpoint.TryParse(endpoint.Text, out var again));
        Assert.Equal(normalized, again!.Text);
    }

    // Finding 11: an offset counts the byte order mark the file actually has.
    [Fact]
    public async Task AnOffsetInAFileWithAByteOrderMarkCountsTheMark()
    {
        using var environment = TestEnvironment.Create();
        await using var server = await FakeBriosaServer.StartAsync();
        var path = Path.Combine(environment.Root, "request.json");
        await File.WriteAllBytesAsync(path, [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("{\"groupName\":{}}")]);

        var run = await CliRun.RunAsync(["call", Operation, "--input", path, "--endpoint", server.Endpoint]);

        Assert.Equal(3, run.ExitCode);
        Assert.Equal(4, run.Envelope["failure"]!["input"]!["byte_offset"]!.GetValue<int>());
    }

    private static string[] Arguments(string command, FakeBriosaServer server) => command switch
    {
        "call" => CallOutcomeTests.CallArguments(server),
        "describe" => ["ops", "describe", Operation, "--endpoint", server.Endpoint],
        "search" => ["ops", "search", "--endpoint", server.Endpoint],
        _ => ["ops", "categories", "--endpoint", server.Endpoint]
    };
}
