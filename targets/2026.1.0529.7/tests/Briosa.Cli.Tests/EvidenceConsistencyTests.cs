using Google.Protobuf;
using Grpc.Core;

namespace Briosa.Cli.Tests;

/// <summary>
/// A trailer is evidence only when it is about this call and agrees with everything
/// else received. Conflicting evidence never yields a disposition or advice to send
/// the request again; it is completion evidence unavailable, exit code 10.
/// </summary>
public sealed class EvidenceConsistencyTests
{
    private const string ErrorTrailer = "briosa-operation-error-bin";

    public static TheoryData<string, int, string> Conflicts()
    {
        var data = new TheoryData<string, int, string>();
        void Add(string label, StatusCode status, OperationError error, string code) =>
            data.Add(label, (int)status, Convert.ToBase64String(error.ToByteArray()) + "|" + code);

        var validation = Validation();
        validation.OperationId = "file_operations.get_working_directory";
        Add("another operation", StatusCode.InvalidArgument, validation, "operation-error-unrelated");

        var missingOperation = Validation();
        missingOperation.OperationId = string.Empty;
        Add("no operation", StatusCode.InvalidArgument, missingOperation, "operation-error-unrelated");

        var dataLoss = Validation();
        dataLoss.MpExecution = new MpExecutionDetails { State = MpExecutionState.Succeeded, MpResultCode = 2 };
        Add("data loss, not started, MP success", StatusCode.DataLoss, dataLoss, "operation-error-inconsistent");

        Add("validation with the wrong status", StatusCode.Unavailable, Validation(), "operation-error-inconsistent");

        var startedValidation = Validation();
        startedValidation.ExecutionDisposition = ExecutionDisposition.Completed;
        Add("validation that completed", StatusCode.InvalidArgument, startedValidation, "operation-error-inconsistent");

        var mpFailure = Error(OperationFailureKind.MpFailure, ExecutionDisposition.Completed);
        mpFailure.MpExecution = CallOutcomeTests.Details(MpExecutionState.Succeeded, 2, OutputRetrievalState.NotAttempted);
        Add("MP failure with MP success", StatusCode.FailedPrecondition, mpFailure, "operation-error-inconsistent");

        var noDetails = Error(OperationFailureKind.MpFailure, ExecutionDisposition.Completed);
        Add("MP failure without MP details", StatusCode.FailedPrecondition, noDetails, "operation-error-inconsistent");

        var stateCode = Error(OperationFailureKind.OutputRetrievalFailure, ExecutionDisposition.Completed);
        stateCode.MpExecution = CallOutcomeTests.Details(MpExecutionState.Succeeded, 4, OutputRetrievalState.Failed);
        Add("success state with code 4", StatusCode.DataLoss, stateCode, "operation-error-inconsistent");

        var unknownField = Error(OperationFailureKind.OutputRetrievalFailure, ExecutionDisposition.Completed);
        unknownField.MpExecution = new MpExecutionDetails
        {
            State = MpExecutionState.Succeeded,
            MpResultCode = 2,
            OutputRetrievals = { new OutputRetrievalDetails { FieldName = "directory", State = OutputRetrievalState.Failed } }
        };
        Add("output of another result", StatusCode.DataLoss, unknownField, "operation-error-unrelated");

        var policyStarted = Error(OperationFailureKind.PolicyDenied, ExecutionDisposition.StartedOutcomeUnknown);
        policyStarted.PolicyDenial = new PolicyDenialDetails { Rule = PolicyDenialRule.Override };
        Add("policy denial that may have started", StatusCode.PermissionDenied, policyStarted, "operation-error-inconsistent");

        var notStartedMp = Error(OperationFailureKind.Overloaded, ExecutionDisposition.NotStarted);
        notStartedMp.ReplayGuidance = ReplayGuidance.MayReplay;
        notStartedMp.MpExecution = CallOutcomeTests.Details(MpExecutionState.Failed, 0, OutputRetrievalState.NotAttempted);
        Add("not started with an MP result", StatusCode.ResourceExhausted, notStartedMp, "operation-error-inconsistent");

        Add("unknown kind", StatusCode.Internal, Error((OperationFailureKind)99, ExecutionDisposition.NotStarted),
            "operation-error-kind-unrecognized");
        return data;
    }

    [Theory]
    [MemberData(nameof(Conflicts))]
    public async Task ConflictingTrailersLeaveCompletionUnknown(string label, int status, string encoded)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        Assert.NotEmpty(label);
        var parts = encoded.Split('|');
        await using var server = await FakeBriosaServer.StartAsync();
        server.Reply = _ => FakeReply.WithTrailers((StatusCode)status, (ErrorTrailer, parts[0]));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server));

        AssertConflict(run, parts[1]);
        CallOutcomeTests.AssertOneRestrictedMpCall(server);
    }

    [Fact]
    public async Task AnOkResponseWithAnErrorTrailerLeavesCompletionUnknown()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var error = Error(OperationFailureKind.MpFailure, ExecutionDisposition.Completed);
        error.MpExecution = CallOutcomeTests.Details(MpExecutionState.Failed, 0, OutputRetrievalState.NotAttempted);
        server.Reply = _ => FakeReply.OkWithTrailers(CallOutcomeTests.Result(MpExecutionState.Succeeded, 2),
            (ErrorTrailer, Convert.ToBase64String(error.ToByteArray())));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server));

        AssertConflict(run, "ok-with-operation-error");
        CallOutcomeTests.AssertOneRestrictedMpCall(server);
    }

    [Fact]
    public async Task AnOkResultNamingOutputsOfAnotherResultLeavesCompletionUnknown()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        server.Reply = _ => FakeReply.Ok(CallOutcomeTests.Result(MpExecutionState.Succeeded, 2,
            new OutputRetrievalDetails { FieldName = "directory", State = OutputRetrievalState.Retrieved }));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server));

        AssertConflict(run, "result-output-unrelated");
    }

    // The server reports code 4 as an MP failure only for its reviewed exceptions,
    // none of which call serves (ReviewRoundThreeTests covers a served read). Outside
    // the read-only tier, the genuine reply of each one is still an MP failure.
    [Fact]
    public void AGenuineCodeFourFailureOutsideTheReadOnlyTierStaysAnMpFailure()
    {
        Assert.All(GenuineResponses.PartialSuccessFailures, operationId =>
        {
            var operation = GenuineResponses.Operation(operationId);
            Assert.True(operation.Context.CodeFourMayBeFailure);

            var envelope = GenuineResponses.Classify(operation, GenuineResponses.Complete(operation, 4));

            Assert.Equal(Engine.BridgeOutcome.MpFailed, envelope.Outcome);
        });
    }

    internal static void AssertConflict(CliRun run, string code)
    {
        Assert.Equal(10, run.ExitCode);
        Assert.Equal("completion_evidence_unavailable", run.Outcome);
        Assert.Equal(code, run.DiagnosticCode);
        Assert.Null(run.Execution["disposition"]);
        Assert.Null(run.Execution["disposition_source"]);
        Assert.Null(run.Execution["replay_guidance"]);
        Assert.Null(run.Execution["recovery_guidance"]);
        Assert.Equal("unavailable", run.Execution["completion_evidence"]!.GetValue<string>());
        Assert.Contains("completion-evidence-unavailable", CallOutcomeTests.Warnings(run));
        var guidance = string.Join(' ', CallOutcomeTests.Guidance(run));
        Assert.Contains("never retries", guidance, StringComparison.Ordinal);
        Assert.DoesNotContain("send it again", guidance, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("completed", guidance, StringComparison.OrdinalIgnoreCase);
    }

    private static OperationError Validation() => Error(OperationFailureKind.Validation, ExecutionDisposition.NotStarted);

    private static OperationError Error(OperationFailureKind kind, ExecutionDisposition disposition) => new()
    {
        OperationId = "analysis_operations.get_ith_point_from_group",
        Kind = kind,
        DiagnosticCode = "request-validation-failed",
        ExecutionDisposition = disposition,
        RecoveryGuidance = RecoveryGuidance.None,
        ReplayGuidance = ReplayGuidance.DoNotReplay,
        ReplaySafety = ReplaySafety.Safe,
        WorkerGeneration = 3
    };
}
