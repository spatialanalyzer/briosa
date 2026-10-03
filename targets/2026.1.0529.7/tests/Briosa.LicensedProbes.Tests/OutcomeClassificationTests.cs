using Briosa.Worker.Control;
using Google.Protobuf;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.LicensedProbes.Tests;

public sealed class OutcomeClassificationTests
{
    private static RpcException Failure(StatusCode status, Api.OperationError? error)
    {
        var trailers = new Metadata();
        if (error is not null)
        {
            trailers.Add(PublicOutcomes.ErrorTrailerName, error.ToByteArray());
        }

        return new RpcException(new Status(status, "raw detail that must not be recorded"), trailers);
    }

    private static Api.OperationError Error(Api.OperationFailureKind kind, Api.ExecutionDisposition disposition, Api.MpExecutionState? state = null, int? code = null)
    {
        var error = new Api.OperationError { Kind = kind, ExecutionDisposition = disposition, DiagnosticCode = "mp-command-failed" };
        if (state is { } executionState)
        {
            error.MpExecution = new Api.MpExecutionDetails { State = executionState };
            if (code is { } resultCode)
            {
                error.MpExecution.MpResultCode = resultCode;
            }
        }

        return error;
    }

    [Theory]
    [InlineData(Api.OperationFailureKind.SdkArgumentRejected, Api.ExecutionDisposition.Completed, nameof(ProbeOutcomeKind.ArgumentRejected))]
    [InlineData(Api.OperationFailureKind.SdkArgumentRejected, Api.ExecutionDisposition.NotStarted, nameof(ProbeOutcomeKind.ArgumentRejected))]
    [InlineData(Api.OperationFailureKind.ExecuteStepRejected, Api.ExecutionDisposition.Completed, nameof(ProbeOutcomeKind.ExecuteStepRejected))]
    [InlineData(Api.OperationFailureKind.MpFailure, Api.ExecutionDisposition.Completed, nameof(ProbeOutcomeKind.MpFailed))]
    [InlineData(Api.OperationFailureKind.MpResultRetrievalFailure, Api.ExecutionDisposition.Completed, nameof(ProbeOutcomeKind.MpResultUnavailable))]
    [InlineData(Api.OperationFailureKind.OutputRetrievalFailure, Api.ExecutionDisposition.Completed, nameof(ProbeOutcomeKind.OutputRetrievalFailed))]
    [InlineData(Api.OperationFailureKind.Validation, Api.ExecutionDisposition.NotStarted, nameof(ProbeOutcomeKind.NotStarted))]
    [InlineData(Api.OperationFailureKind.PolicyDenied, Api.ExecutionDisposition.NotStarted, nameof(ProbeOutcomeKind.NotStarted))]
    [InlineData(Api.OperationFailureKind.ExecuteStepRejected, Api.ExecutionDisposition.StartedOutcomeUnknown, nameof(ProbeOutcomeKind.Indeterminate))]
    [InlineData(Api.OperationFailureKind.MpResultRetrievalFailure, Api.ExecutionDisposition.StartedOutcomeUnknown, nameof(ProbeOutcomeKind.Indeterminate))]
    [InlineData(Api.OperationFailureKind.WorkerWatchdogTimeout, Api.ExecutionDisposition.StartedOutcomeUnknown, nameof(ProbeOutcomeKind.Indeterminate))]
    [InlineData(Api.OperationFailureKind.MpFailure, Api.ExecutionDisposition.StartedOutcomeUnknown, nameof(ProbeOutcomeKind.Indeterminate))]
    [InlineData(Api.OperationFailureKind.WorkerFailure, Api.ExecutionDisposition.Completed, nameof(ProbeOutcomeKind.Indeterminate))]
    [InlineData(Api.OperationFailureKind.Internal, Api.ExecutionDisposition.Unspecified, nameof(ProbeOutcomeKind.Indeterminate))]
    public void TypedPublicErrorsMapToProbeOutcomes(Api.OperationFailureKind kind, Api.ExecutionDisposition disposition, string expected)
    {
        Assert.Equal(Enum.Parse<ProbeOutcomeKind>(expected), PublicOutcomes.Classify(Error(kind, disposition)));
    }

    [Fact]
    public void AnUntypedRpcFailureIsIndeterminate()
    {
        var outcome = PublicOutcomes.FromRpcException(Failure(StatusCode.DeadlineExceeded, null));

        Assert.Equal(ProbeOutcomeKind.Indeterminate, outcome.Kind);
        Assert.Equal("grpc:DeadlineExceeded", outcome.Transport);
        Assert.Equal("untyped-rpc-failure", outcome.DiagnosticCode);
    }

    [Fact]
    public void PublicMpFailurePreservesTheResultCodeButNotRawText()
    {
        var outcome = PublicOutcomes.FromRpcException(Failure(StatusCode.FailedPrecondition,
            Error(Api.OperationFailureKind.MpFailure, Api.ExecutionDisposition.Completed, Api.MpExecutionState.Failed, 0)));

        Assert.Equal(ProbeOutcomeKind.MpFailed, outcome.Kind);
        Assert.Equal(0, outcome.MpResultCode);
        Assert.True(outcome.ExecuteStepReturned);
        Assert.True(outcome.MpResultRetrieved);
        Assert.Equal("mp-command-failed", outcome.DiagnosticCode);
        Assert.DoesNotContain(outcome.Observations.Values, static value => value.Contains("raw", StringComparison.Ordinal));
    }

    [Fact]
    public void PublicSuccessRequiresASucceededExecution()
    {
        var execution = new Api.MpExecutionDetails { State = Api.MpExecutionState.Succeeded, MpResultCode = 2 };
        execution.OutputRetrievals.Add(new Api.OutputRetrievalDetails { FieldName = "points_count", State = Api.OutputRetrievalState.Retrieved });

        var success = PublicOutcomes.FromSuccess(execution, ProbeOutcome.EmptyObservations);

        Assert.Equal(ProbeOutcomeKind.Succeeded, success.Kind);
        Assert.Equal(2, success.MpResultCode);
        Assert.Equal([new OutputObservation("points_count", true)], success.Outputs);
        Assert.Equal(ProbeOutcomeKind.Indeterminate, PublicOutcomes.FromSuccess(null, ProbeOutcome.EmptyObservations).Kind);
    }

    private static WorkerExecutionResponse Completed(WorkerMpExecutionResult execution) => new(
        WorkerExecutionResponseStatus.Completed, execution,
        new WorkerConnectionSnapshot(WorkerConnectionState.Connected, WorkerExecutionReadinessState.ExecutionReady, 0, 1, 1,
            "connect-ex-connected", DateTimeOffset.UnixEpoch),
        null);

    private static readonly Func<IReadOnlyList<WorkerMpOutputValue>, IReadOnlyDictionary<string, string>> NoObservation =
        static _ => Observations.Of(("observed", "true"));

    private static readonly WorkerRetrievedOutput RetrievedCount =
        new("Total Count", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(4));

    private static readonly WorkerUnavailableOutput MissingOutput =
        new("Other", WorkerMpValueKind.WholeNumber, "sdk-output-retrieval-failed");

    private static WorkerMpExecutionResult WorkerResult(string name) => name switch
    {
        "arguments-rejected" => new WorkerArgumentsRejected(1, "sdk-argument-rejected"),
        "execute-rejected" => new WorkerExecuteRejected(1, "execute-step-rejected"),
        "result-unavailable" => new WorkerMpResultUnavailable(1, "sdk-mp-result-retrieval-failed"),
        "outputs-unavailable" => new WorkerMpOutputsUnavailable(1, "worker-output-encoding-rejected"),
        "mp-failed" => new WorkerMpResultAvailable(0, 1, [], "mp-command-failed"),
        "output-retrieval-failed" => new WorkerMpResultAvailable(2, 1, [RetrievedCount, MissingOutput], "sdk-output-retrieval-failed"),
        "succeeded" => new WorkerMpResultAvailable(2, 1, [RetrievedCount], null),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    // Kind and disposition for every worker result type, matching the shipped
    // server's GrpcOperationOutcomeMapper.
    [Theory]
    [InlineData("arguments-rejected", nameof(ProbeOutcomeKind.ArgumentRejected), "NotStarted", false)]
    [InlineData("execute-rejected", nameof(ProbeOutcomeKind.ExecuteStepRejected), "StartedOutcomeUnknown", true)]
    [InlineData("result-unavailable", nameof(ProbeOutcomeKind.MpResultUnavailable), "StartedOutcomeUnknown", true)]
    [InlineData("outputs-unavailable", nameof(ProbeOutcomeKind.OutputRetrievalFailed), "Completed", false)]
    [InlineData("mp-failed", nameof(ProbeOutcomeKind.MpFailed), "Completed", false)]
    [InlineData("output-retrieval-failed", nameof(ProbeOutcomeKind.OutputRetrievalFailed), "Completed", false)]
    [InlineData("succeeded", nameof(ProbeOutcomeKind.Succeeded), "Completed", false)]
    public void WorkerOutcomesCarryTheServerDispositionForEveryResultType(string result, string kind, string disposition, bool completionUnknown)
    {
        var outcome = WorkerOutcomes.FromResponse(Completed(WorkerResult(result)), NoObservation);

        Assert.Equal(Enum.Parse<ProbeOutcomeKind>(kind), outcome.Kind);
        Assert.Equal(disposition, outcome.ExecutionDisposition);
        Assert.Equal(completionUnknown, outcome.CompletionUnknown);
        Assert.Equal(completionUnknown, outcome.Kind.IsCompletionUnknown());
        Assert.Equal(!completionUnknown, ProbeOutcomes.AnyDeterminateSdkOutcome.Accepts(outcome));
        Assert.Equal("worker:completed", outcome.Transport);
    }

    [Fact]
    public void WorkerOutcomesPreserveTheSdkEvidenceOfUnknownCompletion()
    {
        var rejected = WorkerOutcomes.FromResponse(Completed(new WorkerExecuteRejected(1, "execute-step-rejected")), NoObservation);
        var unavailable = WorkerOutcomes.FromResponse(Completed(new WorkerMpResultUnavailable(1, "sdk-mp-result-retrieval-failed")), NoObservation);
        var argument = WorkerOutcomes.FromResponse(Completed(new WorkerArgumentsRejected(1, "sdk-argument-rejected")), NoObservation);

        Assert.False(rejected.ExecuteStepReturned);
        Assert.Null(rejected.MpResultRetrieved);
        Assert.Equal("execute-step-rejected", rejected.DiagnosticCode);
        Assert.True(unavailable.ExecuteStepReturned);
        Assert.False(unavailable.MpResultRetrieved);
        Assert.Null(unavailable.MpResultCode);
        Assert.Null(argument.ExecuteStepReturned);
        Assert.Empty(rejected.Observations);
        Assert.Empty(unavailable.Observations);

        var failed = WorkerOutcomes.FromResponse(Completed(new WorkerMpResultAvailable(0, 1, [], "mp-command-failed")), NoObservation);
        Assert.Equal(ProbeOutcomeKind.MpFailed, failed.Kind);
        Assert.Equal(0, failed.MpResultCode);
        Assert.Empty(failed.Observations);
    }

    [Theory]
    [InlineData(nameof(ProbeOutcomeKind.Succeeded), "Completed")]
    [InlineData(nameof(ProbeOutcomeKind.MpFailed), "Completed")]
    [InlineData(nameof(ProbeOutcomeKind.OutputRetrievalFailed), "Completed")]
    [InlineData(nameof(ProbeOutcomeKind.ArgumentRejected), "NotStarted")]
    [InlineData(nameof(ProbeOutcomeKind.ExecuteStepRejected), "StartedOutcomeUnknown")]
    [InlineData(nameof(ProbeOutcomeKind.MpResultUnavailable), "StartedOutcomeUnknown")]
    [InlineData(nameof(ProbeOutcomeKind.Indeterminate), "StartedOutcomeUnknown")]
    public void WorkerDispositionsMatchTheShippedServer(string kind, string disposition)
    {
        Assert.Equal(disposition, WorkerOutcomes.DispositionOf(Enum.Parse<ProbeOutcomeKind>(kind)));
    }

    [Theory]
    [InlineData(Api.OperationFailureKind.ExecuteStepRejected, Api.ExecutionDisposition.StartedOutcomeUnknown, Api.MpExecutionState.ExecuteStepRejected)]
    [InlineData(Api.OperationFailureKind.MpResultRetrievalFailure, Api.ExecutionDisposition.StartedOutcomeUnknown, Api.MpExecutionState.ResultUnavailable)]
    [InlineData(Api.OperationFailureKind.ExecuteStepRejected, Api.ExecutionDisposition.Completed, Api.MpExecutionState.ExecuteStepRejected)]
    [InlineData(Api.OperationFailureKind.MpResultRetrievalFailure, Api.ExecutionDisposition.Completed, Api.MpExecutionState.ResultUnavailable)]
    public void PublicUnknownCompletionIsNeverAccepted(Api.OperationFailureKind kind, Api.ExecutionDisposition disposition, Api.MpExecutionState state)
    {
        // The server reports these as StartedOutcomeUnknown; even a contradictory
        // Completed disposition cannot make them acceptable.
        var outcome = PublicOutcomes.FromRpcException(Failure(StatusCode.FailedPrecondition, Error(kind, disposition, state)));

        Assert.True(outcome.CompletionUnknown);
        Assert.False(ProbeOutcomes.AnyDeterminateSdkOutcome.Accepts(outcome));
        Assert.Equal(kind.ToString(), outcome.FailureKind);
        Assert.Equal(disposition.ToString(), outcome.ExecutionDisposition);
    }

    [Fact]
    public void WorkerSuccessObservesOutputsAndPartialRetrievalIsNotSuccess()
    {
        var retrieved = new WorkerRetrievedOutput("Total Count", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(4));
        var missing = new WorkerUnavailableOutput("Other", WorkerMpValueKind.WholeNumber, "sdk-output-retrieval-failed");

        var success = WorkerOutcomes.FromResponse(Completed(new WorkerMpResultAvailable(2, 1, [retrieved], null)), NoObservation);
        var partial = WorkerOutcomes.FromResponse(Completed(new WorkerMpResultAvailable(2, 1, [retrieved, missing], "sdk-output-retrieval-failed")), NoObservation);

        Assert.Equal(ProbeOutcomeKind.Succeeded, success.Kind);
        Assert.Equal("true", success.Observations["observed"]);
        Assert.Equal(ProbeOutcomeKind.OutputRetrievalFailed, partial.Kind);
        Assert.Equal([true, false], partial.Outputs.Select(static output => output.Retrieved));
    }

    [Fact]
    public void AnUnavailableWorkerResponseIsNotStarted()
    {
        var response = new WorkerExecutionResponse(WorkerExecutionResponseStatus.Unavailable, null,
            new WorkerConnectionSnapshot(WorkerConnectionState.Faulted, WorkerExecutionReadinessState.Unverified, null, 1, 1,
                "sdk-not-ready", DateTimeOffset.UnixEpoch),
            "sdk-not-ready");

        Assert.Equal(ProbeOutcomeKind.NotStarted, WorkerOutcomes.FromResponse(response, NoObservation).Kind);
    }

    [Fact]
    public void WorkerObservationsReadTypedOutputsOnly()
    {
        var plan = ProbePlan.Create(ProbePhase.Worker, FixtureManifest.Placeholder);
        var count = plan.Steps.First(static step => step.Operation == ProbeOperations.GetNumberOfCollections);
        var unique = plan.Steps.First(static step => step.Id == "p18-shipped");

        var countObservation = count.Operation.ObserveWorker(count.Request,
            [new WorkerRetrievedOutput("Total Count", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(7))]);
        var uniqueObservation = unique.Operation.ObserveWorker(unique.Request,
            [new WorkerRetrievedOutput("Point Name", WorkerMpValueKind.PointName, new WorkerPointNameValue(FixtureNames.Collection, FixtureNames.GroupOne, "P1_1"))]);
        var wrongShape = count.Operation.ObserveWorker(count.Request,
            [new WorkerRetrievedOutput("Total Count", WorkerMpValueKind.Text, new WorkerTextValue("7"))]);
        var missing = count.Operation.ObserveWorker(count.Request, []);

        Assert.Equal("7", countObservation["collection_count"]);
        Assert.Equal("true", uniqueObservation["differs_from_input"]);
        Assert.Equal("unreadable-output-shape", wrongShape["observation"]);
        Assert.Equal("unreadable-output-shape", missing["observation"]);
    }
}
