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
        "c05-06-value" => Outcome(ProbeOutcomeKind.Succeeded, ("match_count", "2")),
        "c24-sentinel-before" or "c24-sentinel-after" or "c24-r0-before" or "c24-r0a-before" =>
            Outcome(ProbeOutcomeKind.Succeeded, ("points_count", "8")),
        "c24-r0-after" or "c24-r0a-after" => Outcome(ProbeOutcomeKind.Succeeded, ("points_count", "7")),
        _ => Outcome(ProbeOutcomeKind.Succeeded)
    };
}

/// <summary>Answers every checkpoint at once (or never, for chosen ones) and records the calls.</summary>
internal sealed class FakeOperator(Func<ProbeStep, OperatorCheckpoint, string?>? answer = null) : IOperatorConsole
{
    public List<string> Checkpoints { get; } = [];

    public List<string> Dispatched { get; } = [];

    public void Dispatching(ProbeStep step, int ordinal, int count) => Dispatched.Add(step.Id);

    public void Completed(ProbeStep step, int ordinal, int count, ProbeOutcome outcome, long elapsedMilliseconds)
    {
    }

    public Task<string?> CheckpointAsync(ProbeStep step, OperatorCheckpoint timing, CancellationToken cancellationToken)
    {
        Checkpoints.Add($"{step.Id}:{FileOperatorConsole.TimingName(timing)}");
        return Task.FromResult(answer is null ? "none" : answer(step, timing));
    }

    public static Func<ProbeOptions, TextWriter, IOperatorConsole> Factory { get; } = static (_, _) => new FakeOperator();
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
