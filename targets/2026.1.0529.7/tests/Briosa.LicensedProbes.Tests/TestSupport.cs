namespace Briosa.LicensedProbes.Tests;

internal static class TestSupport
{
    public const string Sentinel = "SENTINEL";

    // Manual fixture values that must never appear in plans, records, or console output.
    public static FixtureManifest SentinelManifest(string? profileFile = null) => new()
    {
        SchemaVersion = 1,
        SpatialAnalyzerTarget = ProbeTarget.SpatialAnalyzerTarget,
        UsmnInstruments =
        [
            new ManifestInstrument { Collection = "SENTINEL-INSTRUMENTS", InstrumentId = 0 },
            new ManifestInstrument { Collection = "SENTINEL-INSTRUMENTS", InstrumentId = 1 }
        ],
        UsmnNominalsGroup = new ManifestObject { Collection = "SENTINEL-INSTRUMENTS", Name = "SENTINEL-NOMINALS" },
        TemplateChartName = "SENTINEL-TEMPLATE",
        UiProfileName = "SENTINEL-PROFILE",
        UiProfileFile = profileFile ?? @"C:\SENTINEL\profile.xml"
    };

    // A determinate outcome with the disposition the shipped server reports for its kind.
    public static ProbeOutcome Outcome(ProbeOutcomeKind kind, params (string Key, string Value)[] observations) =>
        new(kind, "fake", kind is ProbeOutcomeKind.ArgumentRejected ? null : true,
            kind is ProbeOutcomeKind.ArgumentRejected ? null : true,
            kind switch { ProbeOutcomeKind.Succeeded => 2, ProbeOutcomeKind.ArgumentRejected => null, _ => 3 }, [], null, null,
            WorkerOutcomes.DispositionOf(kind), Observations.Of(observations));

    // Answers every step as a successful, well-formed SDK sequence with
    // observations consistent with the fixture design.
    public static ProbeOutcome Succeeding(ProbeStep step) => step.Id switch
    {
        "g-collections-before" => Outcome(ProbeOutcomeKind.Succeeded, ("collection_count", "3")),
        "g-collections-after" => Outcome(ProbeOutcomeKind.Succeeded, ("collection_count", "4")),
        "c15-before" or "c16-before" => Outcome(ProbeOutcomeKind.Succeeded, ("points_count", "8")),
        "c15-after" => Outcome(ProbeOutcomeKind.Succeeded, ("points_count", "6")),
        "c16-after" => Outcome(ProbeOutcomeKind.Succeeded, ("points_count", "2")),
        _ => Outcome(ProbeOutcomeKind.Succeeded)
    };
}

internal sealed class FakeTransport(ProbePhase phase, Func<ProbeStep, ProbeOutcome> respond) : IProbeTransport
{
    public List<string> Executed { get; } = [];

    public bool Started { get; private set; }

    public bool Disposed { get; private set; }

    public ProbeRefusedException? Refusal { get; init; }

    public ProbePhase Phase { get; } = phase;

    public Task<SessionIdentity> StartAsync(ProbePlan plan, CancellationToken cancellationToken)
    {
        Started = true;
        if (Refusal is not null)
        {
            throw Refusal;
        }

        return Task.FromResult(new SessionIdentity
        {
            ActivatedSdkVersion = ProbeTarget.SpatialAnalyzerTarget,
            ActivatedSdkEvidence = "runtime-verified"
        });
    }

    public Task<ProbeOutcome> ExecuteAsync(ProbeStep step, CancellationToken cancellationToken)
    {
        Executed.Add(step.Id);
        return Task.FromResult(respond(step));
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}

internal sealed class FixedClock : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
}
