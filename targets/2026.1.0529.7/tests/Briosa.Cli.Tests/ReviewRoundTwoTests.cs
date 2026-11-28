using Briosa.Cli.Engine;
using Grpc.Core;

namespace Briosa.Cli.Tests;

/// <summary>Findings of the second independent review, each pinned through the real CLI.</summary>
public sealed class ReviewRoundTwoTests
{
    // A: a genuine result whose outputs live under a nested result field is a success.
    [Fact]
    public async Task AGenuineResultWithNestedOutputsIsASuccess()
    {
        var operation = GenuineResponses.Operation("relationship_operations.get_relationship_status");
        var reply = GenuineResponses.Complete(operation, 2);
        Assert.NotNull(reply.Result);
        await using var server = await FakeBriosaServer.StartAsync();
        server.Reply = _ => FakeReply.Ok(reply.Result);

        var run = await CliRun.RunAsync(["call", operation.OperationId, "--json", "{}", "--endpoint", server.Endpoint]);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("succeeded", run.Outcome);
    }

    // A: an OK result without the outputs its fields need is not evidence of success.
    [Fact]
    public async Task AnOkResultWithoutAnyOutputRowsForItsFieldsIsNotASuccess()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        server.Reply = _ => FakeReply.Ok(new GetIthPointFromGroupResult
        {
            Execution = new MpExecutionDetails { State = MpExecutionState.Succeeded, MpResultCode = 2 }
        });

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server));

        EvidenceConsistencyTests.AssertConflict(run, Codes.ResultOutputMissing);
    }

    // A: an OK result that names one of its three outputs is not a success either.
    [Fact]
    public async Task AnOkResultMissingTheRowOfOneTopLevelFieldIsNotASuccess()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        server.Reply = _ => FakeReply.Ok(CallOutcomeTests.Result(MpExecutionState.Succeeded, 2,
            new OutputRetrievalDetails { FieldName = "complete_point_name", State = OutputRetrievalState.Retrieved },
            new OutputRetrievalDetails { FieldName = "point_name_only", State = OutputRetrievalState.Retrieved }));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server));

        EvidenceConsistencyTests.AssertConflict(run, Codes.ResultOutputMissing);
    }

    private static readonly Dictionary<string, (string Shape, Action<OperationError> Change, string Code)> Contradictions = new()
    {
        ["MP failure that may be replayed"] = ("mp-failure", error => error.ReplayGuidance = ReplayGuidance.MayReplay, Codes.ErrorInconsistent),
        ["MP failure without rows for its outputs"] = ("mp-failure", error => error.MpExecution.OutputRetrievals.Clear(), Codes.ErrorInconsistent),
        ["unknown replay guidance"] = ("request-violation-after-mapping", error => error.ReplayGuidance = (ReplayGuidance)123, Codes.ErrorEnumUnrecognized),
        ["request violation that did not start"] = ("request-violation-after-mapping",
            error => error.ExecutionDisposition = ExecutionDisposition.NotStarted, Codes.ErrorInconsistent),
        ["unknown recovery guidance"] = ("caller-restriction-invalid", error => error.RecoveryGuidance = (RecoveryGuidance)77, Codes.ErrorEnumUnrecognized),
        ["unknown disposition"] = ("caller-restriction-invalid", error => error.ExecutionDisposition = (ExecutionDisposition)9, Codes.ErrorEnumUnrecognized),
        ["unknown replay safety"] = ("caller-restriction-invalid", error => error.ReplaySafety = (ReplaySafety)9, Codes.ErrorEnumUnrecognized),
        ["unspecified disposition"] = ("caller-restriction-invalid", error => error.ExecutionDisposition = ExecutionDisposition.Unspecified, Codes.ErrorInconsistent),
        ["operator recovery without an open dialog"] = ("watchdog-timeout",
            error => error.RecoveryGuidance = RecoveryGuidance.OperatorInterventionRequired, Codes.ErrorInconsistent),
        ["worker replacement for an open dialog"] = ("watchdog-modal-dialog",
            error => error.RecoveryGuidance = RecoveryGuidance.WorkerReplacement, Codes.ErrorInconsistent),
        ["operator recovery for the worker"] = ("worker-unavailable-not-started",
            error => error.RecoveryGuidance = RecoveryGuidance.OperatorInterventionRequired, Codes.ErrorInconsistent),
        ["overload that may not be replayed"] = ("overloaded", error => error.ReplayGuidance = ReplayGuidance.DoNotReplay, Codes.ErrorInconsistent),
        ["replay guidance for another replay safety"] = ("worker-failure",
            error => error.ReplayGuidance = ReplayGuidance.ReconcileBeforeReplay, Codes.ErrorInconsistent),
        ["another operation's replay safety"] = ("caller-restriction-invalid", error => error.ReplaySafety = ReplaySafety.Unsafe, Codes.ErrorUnrelated),
        ["denial detail on a validation failure"] = ("caller-restriction-invalid",
            error => error.PolicyDenial = new PolicyDenialDetails { Rule = PolicyDenialRule.Override }, Codes.ErrorInconsistent),
        ["policy denial without its detail"] = ("policy-denied", error => error.PolicyDenial = null, Codes.ErrorInconsistent),
        ["unknown policy rule"] = ("policy-denied", error => error.PolicyDenial.Rule = (PolicyDenialRule)99, Codes.ErrorEnumUnrecognized),
        ["request field on a policy denial"] = ("policy-denied",
            error => error.RequestValidation = new RequestValidationDetails { FieldPath = "group_name" }, Codes.ErrorInconsistent),
        ["request field with another code"] = ("request-validation-with-field", error => error.DiagnosticCode = "request-encoding-rejected",
            Codes.ErrorInconsistent),
        ["no worker generation"] = ("overloaded", error => error.ClearWorkerGeneration(), Codes.ErrorInconsistent),
        ["result mapping failure without MP details"] = ("result-mapping-failed", error => error.MpExecution = null, Codes.ErrorInconsistent),
        ["output shape failure that may have started"] = ("output-shape-invalid", error =>
        {
            error.ExecutionDisposition = ExecutionDisposition.StartedOutcomeUnknown;
            error.ReplayGuidance = ReplayGuidance.MayReplay;
        }, Codes.ErrorInconsistent)
    };

    public static TheoryData<string> ContradictionLabels() => [.. Contradictions.Keys];

    // B: a genuine error with one change the mapper never makes is conflicting evidence.
    [Theory]
    [MemberData(nameof(ContradictionLabels))]
    public async Task ContradictoryGuidanceIsNeverPublished(string label)
    {
        var (shape, change, code) = Contradictions[label];
        var genuine = ServerMapperShapes.All.Single(candidate => candidate.Label == shape).Exception;
        var error = OperationError.Parser.ParseFrom(genuine.Trailers.Single().ValueBytes);
        change(error);
        await using var server = await FakeBriosaServer.StartAsync();
        server.Reply = _ => FakeReply.Error(genuine.StatusCode, error);

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server));

        EvidenceConsistencyTests.AssertConflict(run, code);
        Assert.Null(run.Execution["recovery_guidance"]);
        Assert.Null(run.Execution["replay_guidance"]);
    }

    // B: a diagnostic code the mapper does not fix stays data, whatever it says.
    [Fact]
    public async Task AnUnknownDiagnosticCodeAloneIsNotAConflict()
    {
        var genuine = ServerMapperShapes.All.Single(candidate => candidate.Label == "overloaded").Exception;
        var error = OperationError.Parser.ParseFrom(genuine.Trailers.Single().ValueBytes);
        error.DiagnosticCode = "customer-part-17";
        await using var server = await FakeBriosaServer.StartAsync();
        server.Reply = _ => FakeReply.Error(genuine.StatusCode, error);

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server));

        Assert.Equal(6, run.ExitCode);
        Assert.Equal("customer-part-17", run.DiagnosticCode);
        Assert.Equal("REPLAY_GUIDANCE_MAY_REPLAY", run.Execution["replay_guidance"]!.GetValue<string>());
    }

    // B: the CLI accepts MP code 4 as a failure of any operation, because it cannot
    // tell the operations whose code 4 the server reports as failure. None of them
    // is one that call sends.
    [Fact]
    public void NoOperationWhoseCodeFourIsAFailureIsServedByCall()
    {
        var capabilities = ServerProjection.AllCapabilities.Operations.ToDictionary(row => row.OperationId, StringComparer.Ordinal);
        Assert.Equal(4, GenuineResponses.PartialSuccessFailures.Count);
        Assert.All(GenuineResponses.PartialSuccessFailures, operationId =>
            Assert.False(BridgeTiers.ServedByCall(BridgeTiers.Classify(capabilities[operationId])), operationId));
    }

    // C: an operation ID must name the method it is bound to, whatever discovery says.
    [Theory]
    [InlineData("call")]
    [InlineData("search")]
    public async Task ExchangedOperationIdsInDiscoveryAreRefused(string command)
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var capabilities = ServerProjection.AllCapabilities;
        var collections = capabilities.Operations.Single(row => row.OperationId == "analysis_operations.get_number_of_collections");
        var directory = capabilities.Operations.Single(row => row.OperationId == "file_operations.get_working_directory");
        (collections.OperationId, directory.OperationId) = (directory.OperationId, collections.OperationId);
        server.AllCapabilities = capabilities;
        server.Reply = _ => FakeReply.Ok(new GetWorkingDirectoryResult
        {
            Directory = @"C:\jobs",
            Execution = new MpExecutionDetails
            {
                State = MpExecutionState.Succeeded,
                MpResultCode = 2,
                OutputRetrievals = { new OutputRetrievalDetails { FieldName = "directory", State = OutputRetrievalState.Retrieved } }
            }
        });

        var run = await CliRun.RunAsync(command == "call"
            ? ["call", "analysis_operations.get_number_of_collections", "--json", "{}", "--endpoint", server.Endpoint]
            : ["ops", "search", "--endpoint", server.Endpoint]);

        Assert.Equal(5, run.ExitCode);
        Assert.Equal("capabilities-inconsistent", run.DiagnosticCode);
        Assert.Empty(server.MpCalls);
    }

    // C: the binding rule holds for every registered operation, with no exception,
    // and no operation ID names any other registered method.
    [Fact]
    public void EveryRegisteredOperationIdNamesItsOwnMethodAndNoOther()
    {
        var operations = ServerProjection.AllCapabilities.Operations;
        Assert.Equal(1018, operations.Count);
        Assert.Equal(operations.Select(row => row.OperationId).Order(StringComparer.Ordinal),
            GenuineResponses.Operations.Select(operation => operation.OperationId).Order(StringComparer.Ordinal));
        Assert.All(operations, row => Assert.True(BridgeEngine.NamesMethod(row.OperationId, row.GrpcService, row.Rpc), row.OperationId));
        var crossBindings = 0;
        foreach (var row in operations)
        {
            crossBindings += operations.Count(other => !ReferenceEquals(other, row) &&
                BridgeEngine.NamesMethod(row.OperationId, other.GrpcService, other.Rpc));
        }

        Assert.Equal(0, crossBindings);
        Assert.True(BridgeEngine.IsConsistent(ServerProjection.AllCapabilities));
    }

    // E: an output failure of an operation with no outputs is a completed output failure.
    [Fact]
    public async Task AnOutputFailureOfAnOperationWithoutOutputsIsCompleted()
    {
        var operation = GenuineResponses.Operations.First(candidate => candidate.Contracts.Count == 0 &&
            candidate.Tier == BridgeTier.ReadOnly);
        var reply = GenuineResponses.Map(operation, new Briosa.Worker.Control.WorkerMpOutputsUnavailable(2, 1, null));
        await using var server = await FakeBriosaServer.StartAsync();
        server.Reply = _ => FakeReply.FromRpcException(reply.Error!);

        var run = await CliRun.RunAsync(["call", operation.OperationId, "--json", "{}", "--endpoint", server.Endpoint]);

        Assert.Equal(9, run.ExitCode);
        Assert.Equal("outputs_incomplete", run.Outcome);
        Assert.Equal("EXECUTION_DISPOSITION_COMPLETED", run.Execution["disposition"]!.GetValue<string>());
    }

    // F: a small, status-only RESOURCE_EXHAUSTED is reported as what it is.
    [Fact]
    public async Task AStatusOnlyResourceExhaustedDiscoveryReplyIsNotCalledTooLarge()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        server.CapabilitiesReply = () => FakeReply.Status(StatusCode.ResourceExhausted);

        var run = await CliRun.RunAsync(["ops", "search", "--endpoint", server.Endpoint]);

        Assert.Equal(5, run.ExitCode);
        Assert.Equal("transport-resource-exhausted", run.DiagnosticCode);
        Assert.Equal("RESOURCE_EXHAUSTED", run.Envelope["transport"]!["status"]!.GetValue<string>());
    }

    // G: the blocked-resend warning claims no more than the CLI observed.
    [Fact]
    public async Task TheBlockedResendWarningClaimsOnlyWhatWasObserved()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var attempts = 0;
        server.Reply = _ => Interlocked.Increment(ref attempts) == 1
            ? FakeReply.RefuseStream()
            : FakeReply.Ok(CallOutcomeTests.Result(MpExecutionState.Succeeded, 2));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server));

        var warning = run.Envelope["warnings"]!.AsArray()
            .Single(entry => entry!["code"]!.GetValue<string>() == "request-resend-blocked")!["message"]!.GetValue<string>();
        Assert.DoesNotContain("before processing", warning, StringComparison.Ordinal);
        Assert.DoesNotContain("refused the stream", warning, StringComparison.Ordinal);
        Assert.Contains("not known", warning, StringComparison.Ordinal);
        Assert.Equal(10, run.ExitCode);
    }
}
