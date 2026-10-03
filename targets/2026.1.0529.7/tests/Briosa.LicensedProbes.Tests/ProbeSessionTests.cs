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
    public async Task ControlsMustSucceedButVariantsRecordEveryCompletedOutcome()
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
