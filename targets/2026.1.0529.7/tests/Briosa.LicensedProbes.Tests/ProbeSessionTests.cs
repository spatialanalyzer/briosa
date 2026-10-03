using Briosa.Worker.Control;
using Google.Protobuf;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.LicensedProbes.Tests;

public sealed class ProbeSessionTests
{
    private static readonly FixedClock Clock = new();

    private static ProbePlan Plan(ProbePhase phase) => ProbePlan.Create(phase, TestSupport.SentinelManifest());

    private static Task<ProbeSessionRecord> Run(ProbePlan plan, FakeTransport transport) =>
        ProbeSession.RunAsync(plan, transport, Clock, new SessionIdentity { HarnessRevision = new string('a', 40) }, CancellationToken.None);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ASuccessfulSessionExecutesEveryStepExactlyOnceInOrder(bool worker)
    {
        var phase = worker ? ProbePhase.Worker : ProbePhase.PublicApi;
        var plan = Plan(phase);
        var transport = new FakeTransport(phase, TestSupport.Succeeding);

        var record = await Run(plan, transport);

        Assert.True(record.Completed);
        Assert.Null(record.StopReason);
        Assert.Equal(plan.Steps.Select(static step => step.Id), transport.Executed);
        Assert.All(record.Steps, static step => Assert.Equal(StepClassification.Observed, step.Classification));
        Assert.Equal(new string('a', 40), record.Identity.HarnessRevision);
        Assert.Equal(ProbeTarget.SpatialAnalyzerTarget, record.Identity.ActivatedSdkVersion);
    }

    [Fact]
    public async Task AnUnknownOutcomeStopsTheSessionWithoutReplay()
    {
        var plan = Plan(ProbePhase.Worker);
        var target = plan.Steps.First(static step => step.Variant.Kind == VariantKind.OmitArgument).Id;
        var transport = new FakeTransport(ProbePhase.Worker, step => step.Id == target
            ? ProbeOutcome.Unknown("worker:timeout", "worker-step-timeout")
            : TestSupport.Succeeding(step));

        var record = await Run(plan, transport);

        Assert.False(record.Completed);
        Assert.Equal(target, record.StoppedAt);
        Assert.Equal(ProbeSession.UnknownOutcomeStop, record.StopReason);
        Assert.Single(transport.Executed, id => id == target);
        Assert.Equal(target, transport.Executed[^1]);
        var index = plan.Steps.ToList().FindIndex(step => step.Id == target);
        Assert.All(record.Steps.Skip(index + 1), static step => Assert.Equal(StepClassification.NotRun, step.Classification));
    }

    private static WorkerExecutionResponse WorkerCompleted(WorkerMpExecutionResult execution) => new(
        WorkerExecutionResponseStatus.Completed, execution,
        new WorkerConnectionSnapshot(WorkerConnectionState.Connected, WorkerExecutionReadinessState.ExecutionReady, 0, 1, 1,
            "connect-ex-connected", DateTimeOffset.UnixEpoch),
        null);

    // A worker-shaped outcome produced by the real classifier, not a hand-built record.
    private static ProbeOutcome FromWorker(ProbeStep step, WorkerMpExecutionResult execution) =>
        WorkerOutcomes.FromResponse(WorkerCompleted(execution), outputs => step.Operation.ObserveWorker(step.Request, outputs));

    private static WorkerMpExecutionResult UnknownCompletion(string name) => name switch
    {
        "execute-rejected" => new WorkerExecuteRejected(1, "execute-step-rejected"),
        "result-unavailable" => new WorkerMpResultUnavailable(1, "sdk-mp-result-retrieval-failed"),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    [Theory]
    [InlineData("execute-rejected", nameof(ProbeOutcomeKind.ExecuteStepRejected))]
    [InlineData("result-unavailable", nameof(ProbeOutcomeKind.MpResultUnavailable))]
    public async Task AnUnknownCompletionWorkerResultOnAProbeStopsTheSessionWithoutReplay(string result, string kind)
    {
        var plan = Plan(ProbePhase.Worker);
        var target = plan.Steps.First(static step => step.Variant.Kind == VariantKind.OmitArgument).Id;
        var transport = new FakeTransport(ProbePhase.Worker, step => step.Id == target
            ? FromWorker(step, UnknownCompletion(result))
            : TestSupport.Succeeding(step));

        var record = await Run(plan, transport);

        Assert.False(record.Completed);
        Assert.Equal(target, record.StoppedAt);
        Assert.Equal(ProbeSession.UnknownOutcomeStop, record.StopReason);
        Assert.Equal(target, transport.Executed[^1]);
        Assert.Single(transport.Executed, id => id == target);
        var index = plan.Steps.ToList().FindIndex(step => step.Id == target);
        Assert.Equal(index + 1, transport.Executed.Count);
        var stopped = record.Steps[index];
        Assert.Equal(StepClassification.Unexpected, stopped.Classification);
        Assert.Equal(Enum.Parse<ProbeOutcomeKind>(kind), stopped.Outcome!.Kind);
        Assert.Equal(ProbeOutcome.StartedOutcomeUnknown, stopped.Outcome.ExecutionDisposition);
        Assert.All(record.Steps.Skip(index + 1), static step => Assert.Equal(StepClassification.NotRun, step.Classification));
    }

    // Every planned step of both phases, whatever its kind (guard, setup, check,
    // probe, variant, or worker control), stops as do-not-replay on an outcome
    // whose completion is unknown, and nothing after it runs.
    [Theory]
    [InlineData(false, "execute-rejected")]
    [InlineData(false, "result-unavailable")]
    [InlineData(true, "execute-rejected")]
    [InlineData(true, "result-unavailable")]
    public async Task EveryPlannedStepStopsOnUnknownCompletion(bool worker, string result)
    {
        var phase = worker ? ProbePhase.Worker : ProbePhase.PublicApi;
        var steps = Plan(phase).Steps;
        for (var index = 0; index < steps.Count; index++)
        {
            var target = steps[index].Id;
            var transport = new FakeTransport(phase, step => step.Id != target
                ? TestSupport.Succeeding(step)
                : worker ? FromWorker(step, UnknownCompletion(result)) : PublicContradiction(result));

            var record = await Run(Plan(phase), transport);

            Assert.Equal(target, record.StoppedAt);
            Assert.Equal(ProbeSession.UnknownOutcomeStop, record.StopReason);
            Assert.Equal(index + 1, transport.Executed.Count);
        }
    }

    // A public error naming ExecuteStepRejected or MpResultRetrievalFailure with a
    // contradictory Completed disposition: the kind alone must stop the session.
    private static ProbeOutcome PublicContradiction(string result)
    {
        var error = new Api.OperationError
        {
            Kind = result == "execute-rejected" ? Api.OperationFailureKind.ExecuteStepRejected : Api.OperationFailureKind.MpResultRetrievalFailure,
            ExecutionDisposition = Api.ExecutionDisposition.Completed,
            DiagnosticCode = "contradictory-disposition"
        };
        var trailers = new Metadata { { PublicOutcomes.ErrorTrailerName, error.ToByteArray() } };
        return PublicOutcomes.FromRpcException(new RpcException(new Status(StatusCode.FailedPrecondition, "detail"), trailers));
    }

    [Theory]
    [InlineData("arguments-rejected")]
    [InlineData("mp-failed")]
    [InlineData("outputs-unavailable")]
    public async Task DeterminateWorkerResultsOnVariantsStillContinue(string result)
    {
        var plan = Plan(ProbePhase.Worker);
        var transport = new FakeTransport(ProbePhase.Worker, step => step.Variant.Kind == VariantKind.Shipped
            ? TestSupport.Succeeding(step)
            : FromWorker(step, result switch
            {
                "arguments-rejected" => new WorkerArgumentsRejected(1, "sdk-argument-rejected"),
                "mp-failed" => new WorkerMpResultAvailable(0, 1, [], "mp-command-failed"),
                _ => new WorkerMpOutputsUnavailable(1, "worker-output-encoding-rejected")
            }));

        var record = await Run(plan, transport);

        Assert.True(record.Completed);
        Assert.Null(record.StopReason);
        Assert.Equal(plan.Steps.Select(static step => step.Id), transport.Executed);
        Assert.Contains(record.Steps, static step => step.Step.Variant.Kind != VariantKind.Shipped &&
            step.Classification == StepClassification.Observed && !step.Outcome!.CompletionUnknown);
    }

    [Fact]
    public async Task AnArgumentRejectionAndAnMpFailureOnPublicProbesStillContinue()
    {
        var plan = Plan(ProbePhase.PublicApi);
        var probes = plan.Steps.Where(static step => step.Kind == ProbeStepKind.Probe &&
            step.Acceptable == ProbeOutcomes.AnyDeterminateSdkOutcome).Select(static step => step.Id).ToList();
        var transport = new FakeTransport(ProbePhase.PublicApi, step => !probes.Contains(step.Id)
            ? TestSupport.Succeeding(step)
            : probes.IndexOf(step.Id) % 2 == 0
                ? TestSupport.Outcome(ProbeOutcomeKind.ArgumentRejected)
                : TestSupport.Outcome(ProbeOutcomeKind.MpFailed));

        var record = await Run(plan, transport);

        Assert.True(probes.Count >= 2);
        Assert.True(record.Completed);
        Assert.Equal(plan.Steps.Select(static step => step.Id), transport.Executed);
    }

    [Fact]
    public async Task AReportedStartedOutcomeUnknownDispositionStopsEvenWithADeterminateKind()
    {
        var plan = Plan(ProbePhase.Worker);
        var target = plan.Steps.First(static step => step.Variant.Kind == VariantKind.BlankArgument).Id;
        var transport = new FakeTransport(ProbePhase.Worker, step => step.Id == target
            ? TestSupport.Outcome(ProbeOutcomeKind.MpFailed) with { ExecutionDisposition = ProbeOutcome.StartedOutcomeUnknown }
            : TestSupport.Succeeding(step));

        var record = await Run(plan, transport);

        Assert.Equal(target, record.StoppedAt);
        Assert.Equal(ProbeSession.UnknownOutcomeStop, record.StopReason);
    }

    [Fact]
    public async Task AFailedFixtureStepStopsBeforeAnyProbe()
    {
        var plan = Plan(ProbePhase.PublicApi);
        var transport = new FakeTransport(ProbePhase.PublicApi, step => step.Id == "s-plane-PA"
            ? TestSupport.Outcome(ProbeOutcomeKind.MpFailed)
            : TestSupport.Succeeding(step));

        var record = await Run(plan, transport);

        Assert.Equal("s-plane-PA", record.StoppedAt);
        Assert.Equal(ProbeSession.UnexpectedDispositionStop, record.StopReason);
        Assert.DoesNotContain(transport.Executed, static id => id.StartsWith('p'));
    }

    [Fact]
    public async Task ControlsMustSucceedButVariantsRecordEveryDeterminateOutcome()
    {
        var plan = Plan(ProbePhase.Worker);
        var transport = new FakeTransport(ProbePhase.Worker, step => step.Variant.Kind != VariantKind.Shipped
            ? TestSupport.Outcome(ProbeOutcomeKind.ArgumentRejected)
            : TestSupport.Succeeding(step));

        var record = await Run(plan, transport);

        Assert.True(record.Completed);
        Assert.Contains(record.Steps, static step => step.Outcome?.Kind == ProbeOutcomeKind.ArgumentRejected);

        var failingControl = new FakeTransport(ProbePhase.Worker, step => step.Id == "p02-shipped"
            ? TestSupport.Outcome(ProbeOutcomeKind.MpFailed)
            : TestSupport.Succeeding(step));
        var stopped = await Run(Plan(ProbePhase.Worker), failingControl);

        Assert.Equal("p02-shipped", stopped.StoppedAt);
        Assert.DoesNotContain("p02-omit", failingControl.Executed);
    }

    [Fact]
    public async Task ACollectionThatAlreadyExistedStopsTheSession()
    {
        var plan = Plan(ProbePhase.PublicApi);
        var transport = new FakeTransport(ProbePhase.PublicApi, step => step.Id == "g-collections-after"
            ? TestSupport.Outcome(ProbeOutcomeKind.Succeeded, ("collection_count", "3"))
            : TestSupport.Succeeding(step));

        var record = await Run(plan, transport);

        Assert.Equal("g-collections-after", record.StoppedAt);
        Assert.Equal(ProbeSession.RequirementStop, record.StopReason);
        Assert.False(record.Steps.Single(static step => step.Step.Id == "g-collections-after").RequirementSatisfied);
        Assert.DoesNotContain(transport.Executed, static id => id.StartsWith("s-point", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AModifiedDisposableCloudStopsBeforeTheDeletion()
    {
        var plan = Plan(ProbePhase.PublicApi);
        var transport = new FakeTransport(ProbePhase.PublicApi, step => step.Id == "c15-before"
            ? TestSupport.Outcome(ProbeOutcomeKind.Succeeded, ("points_count", "9"))
            : TestSupport.Succeeding(step));

        var record = await Run(plan, transport);

        Assert.Equal("c15-before", record.StoppedAt);
        Assert.DoesNotContain("p15", transport.Executed);
    }

    [Fact]
    public async Task HypothesesAreEvaluatedAgainstObservations()
    {
        var record = await Run(Plan(ProbePhase.PublicApi), new FakeTransport(ProbePhase.PublicApi, TestSupport.Succeeding));

        var partial = record.Steps.Single(static step => step.Step.Id == "c16-after");
        Assert.Equal("omitted Y/Z bounds not applied (unbounded)", Assert.Single(partial.Hypotheses, static h => h.Matched).Label);
    }

    [Fact]
    public async Task ATransportRefusalRunsNoStep()
    {
        var plan = Plan(ProbePhase.Worker);
        var transport = new FakeTransport(ProbePhase.Worker, TestSupport.Succeeding)
        {
            Refusal = new ProbeRefusedException("activated-sdk-identity-mismatch")
        };

        var record = await Run(plan, transport);

        Assert.Empty(transport.Executed);
        Assert.Equal("activated-sdk-identity-mismatch", record.StopReason);
        Assert.Null(record.StoppedAt);
        Assert.False(record.Completed);
        Assert.All(record.Steps, static step => Assert.Equal(StepClassification.NotRun, step.Classification));
    }

    [Fact]
    public async Task ATransportForAnotherPhaseIsRejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Run(Plan(ProbePhase.Worker), new FakeTransport(ProbePhase.PublicApi, TestSupport.Succeeding)));
    }

    [Fact]
    public void ADryRunRecordsThePlanWithoutATransport()
    {
        var record = ProbeSession.DryRun(Plan(ProbePhase.PublicApi), Clock);

        Assert.True(record.DryRun);
        Assert.False(record.Completed);
        Assert.All(record.Steps, static step => Assert.Null(step.Outcome));
    }

    [Fact]
    public void RefusalCodesAreSanitized()
    {
        Assert.Equal("probe-refused", new ProbeRefusedException("Raw text: C:\\path").DiagnosticCode);
        Assert.Null(SafeCode.OrNull("Contains Upper"));
        Assert.Equal("ok-code-1", SafeCode.OrNull("ok-code-1"));
    }
}
