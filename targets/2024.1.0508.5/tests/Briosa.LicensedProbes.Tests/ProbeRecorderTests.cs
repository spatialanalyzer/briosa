using System.Text.Json;

namespace Briosa.LicensedProbes.Tests;

public sealed class ProbeRecorderTests
{
    private static async Task<ProbeSessionRecord> Session(ProbePhase phase, Func<ProbeStep, ProbeOutcome> respond) =>
        await ProbeSession.RunAsync(ProbePlan.Create(phase, TestSupport.SentinelManifest()), new FakeTransport(phase, respond),
            new FixedClock(), new SessionIdentity
            {
                ConnectedSpatialAnalyzerAttestedVersion = ProbeTarget.SpatialAnalyzerTarget,
                ConnectedSpatialAnalyzerAttestationReference = "r277-connected"
            }, CancellationToken.None);

    [Fact]
    public async Task JsonRecordsStructureWithoutManualValues()
    {
        var record = await Session(ProbePhase.Worker, TestSupport.Succeeding);

        var json = ProbeRecorder.ToJson(record);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(ProbeRecorder.SchemaVersion, root.GetProperty("schema_version").GetInt32());
        Assert.Equal(ProbeTarget.SpatialAnalyzerTarget, root.GetProperty("spatial_analyzer_target").GetString());
        Assert.Equal("worker", root.GetProperty("phase").GetString());
        Assert.Equal("licensed", root.GetProperty("mode").GetString());
        Assert.True(root.GetProperty("completed").GetBoolean());
        Assert.Equal("r277-connected", root.GetProperty("identity").GetProperty("connected_sa_attestation_reference").GetString());
        var steps = root.GetProperty("steps").EnumerateArray().ToList();
        Assert.Equal(record.Steps.Count, steps.Count);
        var omit = steps.Single(static step => step.GetProperty("id").GetString() == "p02-omit");
        Assert.Equal("omit-argument", omit.GetProperty("variant").GetProperty("kind").GetString());
        Assert.Equal(2, omit.GetProperty("outcome").GetProperty("mp_result_code").GetInt32());
        Assert.Equal("observed", omit.GetProperty("classification").GetString());
        Assert.DoesNotContain(TestSupport.Sentinel, json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MarkdownFollowsTheEvidenceStyleAndReportsTheStop()
    {
        var record = await Session(ProbePhase.Worker, step => step.Id == "p05-omit"
            ? TestSupport.Outcome(ProbeOutcomeKind.ArgumentRejected)
            : step.Id == "p05-blank" ? ProbeOutcome.Unknown("worker:timeout", "worker-step-timeout") : TestSupport.Succeeding(step));

        var markdown = ProbeRecorder.ToMarkdown(record);

        Assert.StartsWith($"# SA {ProbeTarget.SpatialAnalyzerTarget} licensed probe observations, worker phase", markdown, StringComparison.Ordinal);
        Assert.Contains("| Step | # | MP step | Variant | Observed result |", markdown, StringComparison.Ordinal);
        Assert.Contains("a setter returned false; `ExecuteStep` was not called.", markdown, StringComparison.Ordinal);
        Assert.Contains("`GetMPStepResult` returned true with MP code `2`", markdown, StringComparison.Ordinal);
        Assert.Contains("stopped at `p05-blank` (`outcome-unknown-do-not-replay`)", markdown, StringComparison.Ordinal);
        Assert.Contains("not vendor guarantees", markdown, StringComparison.Ordinal);
        Assert.Contains("Not run:", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain(TestSupport.Sentinel, markdown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnExecuteStepRejectionIsRecordedAsUnknownCompletionAndTheStop()
    {
        var record = await Session(ProbePhase.Worker, step => step.Id == "p05-omit"
            ? new ProbeOutcome(ProbeOutcomeKind.ExecuteStepRejected, "worker:completed", false, null, null, [],
                "execute-step-rejected", null, WorkerOutcomes.DispositionOf(ProbeOutcomeKind.ExecuteStepRejected), ProbeOutcome.EmptyObservations)
            : TestSupport.Succeeding(step));

        var markdown = ProbeRecorder.ToMarkdown(record);
        using var document = JsonDocument.Parse(ProbeRecorder.ToJson(record));
        var root = document.RootElement;
        var stopped = root.GetProperty("steps").EnumerateArray().Single(static step => step.GetProperty("id").GetString() == "p05-omit");

        Assert.Contains("setters returned true; `ExecuteStep` returned false; completion unknown; not replayed.", markdown, StringComparison.Ordinal);
        Assert.Contains("**Completion unknown; the session stopped here (do not replay).**", markdown, StringComparison.Ordinal);
        Assert.Contains("stopped at `p05-omit` (`outcome-unknown-do-not-replay`)", markdown, StringComparison.Ordinal);
        Assert.Contains("Completion of that step is unknown.", markdown, StringComparison.Ordinal);
        Assert.Equal("outcome-unknown-do-not-replay", root.GetProperty("stop_reason").GetString());
        Assert.Equal("p05-omit", root.GetProperty("stopped_at").GetString());
        Assert.Equal("unexpected", stopped.GetProperty("classification").GetString());
        Assert.Equal("ExecuteStepRejected", stopped.GetProperty("outcome").GetProperty("kind").GetString());
        Assert.Equal("StartedOutcomeUnknown", stopped.GetProperty("outcome").GetProperty("execution_disposition").GetString());
        Assert.False(stopped.GetProperty("outcome").GetProperty("execute_step_returned").GetBoolean());
    }

    [Fact]
    public async Task PublicMarkdownShowsHypothesisMatches()
    {
        var markdown = ProbeRecorder.ToMarkdown(await Session(ProbePhase.PublicApi, TestSupport.Succeeding));

        Assert.Contains("Matches: omitted Y/Z bounds not applied (unbounded).", markdown, StringComparison.Ordinal);
        Assert.Contains("Public RPC `fake`", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain(TestSupport.Sentinel, markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void DryRunRecordsAreMarkedAsNotEvidence()
    {
        var record = ProbeSession.DryRun(ProbePlan.Create(ProbePhase.PublicApi, TestSupport.SentinelManifest()), new FixedClock());

        Assert.Contains("**Dry run.**", ProbeRecorder.ToMarkdown(record), StringComparison.Ordinal);
        Assert.EndsWith("-dry-run", ProbeRecorder.FileStem(record), StringComparison.Ordinal);
        using var document = JsonDocument.Parse(ProbeRecorder.ToJson(record));
        Assert.Equal("dry-run", document.RootElement.GetProperty("mode").GetString());
        Assert.All(document.RootElement.GetProperty("steps").EnumerateArray(),
            static step => Assert.Equal(JsonValueKind.Null, step.GetProperty("outcome").ValueKind));
    }

    [Fact]
    public void WritingNeverOverwritesARecord()
    {
        var directory = Path.Combine(Path.GetTempPath(), "briosa-probe-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var record = ProbeSession.DryRun(ProbePlan.Create(ProbePhase.Worker, FixtureManifest.Placeholder), new FixedClock());
            var (json, markdown) = ProbeRecorder.Write(record, directory);

            Assert.True(File.Exists(json));
            Assert.True(File.Exists(markdown));
            Assert.Throws<IOException>(() => ProbeRecorder.Write(record, directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
