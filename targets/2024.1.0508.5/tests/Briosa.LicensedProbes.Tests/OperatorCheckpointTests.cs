using System.Text.Json;
using Api = global::Briosa;

namespace Briosa.LicensedProbes.Tests;

public sealed class OperatorCheckpointTests : IDisposable
{
    private static readonly FixedClock Clock = new();

    private readonly string _root = Path.Combine(Path.GetTempPath(), "briosa-probe-checkpoints-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static ProbePlan Plan(ProbePhase phase) => ProbePlan.Create(phase, TestSupport.SentinelManifest());

    // Since probe 11 (#309) was retired, no catalog step asks before dispatch or sets its
    // own client deadline, so these tests give both to the first public probe.
    private const string Watched = "p02-value";

    private static ProbePlan WatchedPlan() => ProbePlan.FromSteps(ProbePhase.PublicApi,
        [.. Plan(ProbePhase.PublicApi).Steps.Select(static step => step.Id == Watched
            ? step with { Checkpoints = OperatorCheckpoint.Before | OperatorCheckpoint.After, ClientTimeout = TimeSpan.FromSeconds(630) }
            : step)]);

    private static Task<ProbeSessionRecord> Run(ProbePlan plan, FakeTransport transport, IOperatorConsole console) =>
        ProbeSession.RunAsync(plan, transport, Clock, SessionIdentity.None, console, CancellationToken.None);

    // Session

    [Fact]
    public async Task CheckpointsAreAskedInPlanOrderAroundTheirSteps()
    {
        var plan = WatchedPlan();
        var console = new FakeOperator();

        var record = await Run(plan, new FakeTransport(ProbePhase.PublicApi, TestSupport.Succeeding), console);

        Assert.True(record.Completed);
        var expected = plan.Steps.SelectMany(static step => new[]
        {
            step.Checkpoints.HasFlag(OperatorCheckpoint.Before) ? $"{step.Id}:before" : null,
            step.Checkpoints.HasFlag(OperatorCheckpoint.After) ? $"{step.Id}:after" : null
        }).OfType<string>();
        Assert.Equal(expected, console.Checkpoints);
        Assert.Equal([$"{Watched}:before", $"{Watched}:after"], console.Checkpoints.Take(2));
        Assert.Equal(plan.Steps.Select(static step => step.Id), console.Dispatched);
    }

    [Fact]
    public async Task AnUnansweredBeforeCheckpointSendsNothing()
    {
        var plan = WatchedPlan();
        var transport = new FakeTransport(ProbePhase.PublicApi, TestSupport.Succeeding);

        var record = await Run(plan, transport, new FakeOperator((step, timing) => timing == OperatorCheckpoint.Before ? null : "none"));

        Assert.Equal(Watched, record.StoppedAt);
        Assert.Equal(ProbeSession.CheckpointStop, record.StopReason);
        Assert.DoesNotContain(Watched, transport.Executed);
        var watched = record.Steps.Single(static step => step.Step.Id == Watched);
        Assert.Equal(StepClassification.Refused, watched.Classification);
        Assert.Equal("operator-checkpoint-not-confirmed", watched.Outcome!.DiagnosticCode);
        Assert.All(record.Steps.SkipWhile(static step => step.Step.Id != Watched).Skip(1),
            static step => Assert.Equal(StepClassification.NotRun, step.Classification));
    }

    [Fact]
    public async Task AnUnansweredAfterCheckpointKeepsTheResultAndSendsNothingMore()
    {
        var plan = Plan(ProbePhase.PublicApi);
        var transport = new FakeTransport(ProbePhase.PublicApi, TestSupport.Succeeding);

        var record = await Run(plan, transport,
            new FakeOperator((step, timing) => step.Id == "p24-r0" && timing == OperatorCheckpoint.After ? null : "none"));

        Assert.Equal("p24-r0", record.StoppedAt);
        Assert.Equal(ProbeSession.CheckpointStop, record.StopReason);
        Assert.False(record.Completed);
        Assert.Equal("p24-r0", transport.Executed[^1]);
        Assert.Equal(StepClassification.Observed, record.Steps.Single(static step => step.Step.Id == "p24-r0").Classification);
        Assert.Equal(StepClassification.NotRun, record.Steps.Single(static step => step.Step.Id == "c24-r0-after").Classification);
    }

    [Fact]
    public async Task TheOperatorIsAskedAfterAnUnknownOutcomeAndTheStopStaysUnknown()
    {
        var plan = WatchedPlan();
        var console = new FakeOperator((_, _) => "dialog: Example dialog (dismissed during the step)");
        var transport = new FakeTransport(ProbePhase.PublicApi, step => step.Id == Watched
            ? ProbeOutcome.Unknown("grpc:DeadlineExceeded", "untyped-rpc-failure")
            : TestSupport.Succeeding(step));

        var record = await Run(plan, transport, console);

        Assert.Equal(ProbeSession.UnknownOutcomeStop, record.StopReason);
        Assert.Equal([$"{Watched}:before", $"{Watched}:after"], console.Checkpoints);
        Assert.Equal(2, record.Steps.Single(static step => step.Step.Id == Watched).OperatorObservations.Count);
    }

    [Fact]
    public async Task ObservationsAndElapsedTimeReachBothRecords()
    {
        var plan = WatchedPlan();
        var record = await Run(plan, new FakeTransport(ProbePhase.PublicApi, TestSupport.Succeeding),
            new FakeOperator((step, timing) => step.Id == "p25-chart-missing-silent"
                ? "dialog: Template Chart Not found (dismissed during the step)"
                : "none"));

        using var document = JsonDocument.Parse(ProbeRecorder.ToJson(record));
        var steps = document.RootElement.GetProperty("steps").EnumerateArray()
            .ToDictionary(static step => step.GetProperty("id").GetString()!);
        var chart = steps["p25-chart-missing-silent"];
        var watched = steps[Watched];
        var markdown = ProbeRecorder.ToMarkdown(record);

        Assert.Equal(4, document.RootElement.GetProperty("schema_version").GetInt32());
        Assert.Equal(["before", "after"], watched.GetProperty("operator_checkpoints").EnumerateArray().Select(static t => t.GetString()));
        Assert.Equal(630, watched.GetProperty("client_timeout_seconds").GetInt64());
        Assert.Equal(0, watched.GetProperty("elapsed_ms").GetInt64());
        var observation = Assert.Single(chart.GetProperty("operator_observations").EnumerateArray());
        Assert.Equal("after", observation.GetProperty("timing").GetString());
        Assert.Equal("dialog: Template Chart Not found (dismissed during the step)", observation.GetProperty("text").GetString());
        Assert.Empty(steps["s-collection"].GetProperty("operator_checkpoints").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, steps["s-collection"].GetProperty("client_timeout_seconds").ValueKind);
        Assert.Contains("Operator after the result: `dialog: Template Chart Not found (dismissed during the step)`.", markdown, StringComparison.Ordinal);
        Assert.Contains("Operator before dispatch: `none`.", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheRecordStatesTheInteractionModeOnlyWhenThePlanChangesIt()
    {
        var supplied = new InteractionModeSettings(Api.SaInteractionMode.Manual, Supplied: true);
        var withMode = await Run(ProbePlan.Create(ProbePhase.Worker, TestSupport.SentinelManifest(), [], supplied),
            new FakeTransport(ProbePhase.Worker, TestSupport.Succeeding), new FakeOperator());
        var without = await Run(ProbePlan.Create(ProbePhase.Worker, TestSupport.SentinelManifest(), [25], supplied),
            new FakeTransport(ProbePhase.Worker, TestSupport.Succeeding), new FakeOperator());

        using var document = JsonDocument.Parse(ProbeRecorder.ToJson(withMode));
        var mode = document.RootElement.GetProperty("interaction_mode");
        Assert.Equal("Manual", mode.GetProperty("restore_sa_interaction_mode").GetString());
        Assert.True(mode.GetProperty("restore_supplied_by_operator").GetBoolean());
        Assert.Equal("Allow Application Interaction", mode.GetProperty("mp_dialog_interaction_mode").GetString());
        Assert.Contains("**Interaction mode.**", ProbeRecorder.ToMarkdown(withMode), StringComparison.Ordinal);

        using var none = JsonDocument.Parse(ProbeRecorder.ToJson(without));
        Assert.Equal(JsonValueKind.Null, none.RootElement.GetProperty("interaction_mode").ValueKind);
        Assert.DoesNotContain("**Interaction mode.**", ProbeRecorder.ToMarkdown(without), StringComparison.Ordinal);
    }

    // File console

    private static ProbeStep AnyStep() => ProbePlan.Create(ProbePhase.PublicApi, TestSupport.SentinelManifest()).Steps.Single(static step => step.Id == "p24-r0");

    private FileOperatorConsole NewConsole(StringWriter output, TimeSpan timeout) =>
        new(_root, output, timeout, TimeProvider.System, TimeSpan.FromMilliseconds(10));

    private async Task AnswerAsync(string fileName, string text)
    {
        await WaitForRequestAsync(fileName);
        await File.WriteAllTextAsync(Path.Combine(_root, fileName), text);
    }

    // The console writes the request file before it starts polling for the response.
    private async Task WaitForRequestAsync(string responseFileName)
    {
        var request = Path.Combine(_root,
            responseFileName.Replace(FileOperatorConsole.ResponseSuffix, FileOperatorConsole.RequestSuffix, StringComparison.Ordinal));
        for (var attempt = 0; attempt < 1000 && !File.Exists(request); attempt++)
        {
            await Task.Delay(10);
        }

        Assert.True(File.Exists(request));
    }

    [Fact]
    public async Task TheFileConsoleWaitsForTheResponseFileAndNormalizesIt()
    {
        using var output = new StringWriter();
        var console = NewConsole(output, TimeSpan.FromSeconds(60));

        var pending = console.CheckpointAsync(AnyStep(), OperatorCheckpoint.Before, CancellationToken.None);
        await AnswerAsync("001-p24-r0-before.response.txt", "  Dialog:   Encountered an improper argument.  \r\n");
        var answer = await pending;

        Assert.Equal("dialog: Encountered an improper argument.", answer);
        Assert.Contains("p24-r0 (probe 24)", await File.ReadAllTextAsync(Path.Combine(_root, "001-p24-r0-before.request.txt")), StringComparison.Ordinal);
        var text = output.ToString();
        Assert.Contains("OPERATOR CHECKPOINT 001-p24-r0-before: before dispatch of p24-r0.", text, StringComparison.Ordinal);
        Assert.Contains("001-p24-r0-before.response.txt", text, StringComparison.Ordinal);
        Assert.DoesNotContain(_root, text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ARefusedResponseIsSetAsideAndTheConsoleKeepsWaiting()
    {
        using var output = new StringWriter();
        var console = NewConsole(output, TimeSpan.FromSeconds(60));

        var pending = console.CheckpointAsync(AnyStep(), OperatorCheckpoint.After, CancellationToken.None);
        await AnswerAsync("001-p24-r0-after.response.txt", @"dialog: saved to C:\Users\someone\file");
        for (var attempt = 0; attempt < 500 && !File.Exists(Path.Combine(_root, "001-p24-r0-after.refused-1.txt")); attempt++)
        {
            await Task.Delay(10);
        }

        Assert.False(pending.IsCompleted);
        await File.WriteAllTextAsync(Path.Combine(_root, "001-p24-r0-after.response.txt"), "none");

        Assert.Equal("none", await pending);
        Assert.Contains("response refused", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AResponseIsReadOnlyAfterItsWriterClosesIt()
    {
        using var output = new StringWriter();
        var console = NewConsole(output, TimeSpan.FromSeconds(60));
        var pending = console.CheckpointAsync(AnyStep(), OperatorCheckpoint.After, CancellationToken.None);
        await WaitForRequestAsync("001-p24-r0-after.response.txt");

        // A writer that has written only part of a valid answer and still holds the file.
        // CreateNew creates the file and the writer's handle in one call, so the console
        // can never see the file without this writer.
        var writer = new StreamWriter(new FileStream(Path.Combine(_root, "001-p24-r0-after.response.txt"),
            FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete));
        await writer.WriteAsync("dialog: Templ");
        await writer.FlushAsync();
        await Task.Delay(300);

        Assert.False(pending.IsCompleted, "A partly written answer must not be recorded.");
        await writer.WriteAsync("ate Chart Not found");
        await writer.DisposeAsync();

        Assert.Equal("dialog: Template Chart Not found", await pending);
    }

    [Fact]
    public async Task ARefusedResponseThatCannotBeSetAsideYetDoesNotFaultTheCheckpoint()
    {
        using var output = new StringWriter();
        var console = NewConsole(output, TimeSpan.FromSeconds(60));
        var pending = console.CheckpointAsync(AnyStep(), OperatorCheckpoint.After, CancellationToken.None);
        await WaitForRequestAsync("001-p24-r0-after.response.txt");

        // The move fails as it does when another process still holds the answer: here
        // its set-aside name is taken, so File.Move throws an IOException.
        var path = Path.Combine(_root, "001-p24-r0-after.response.txt");
        var setAside = Path.Combine(_root, "001-p24-r0-after.refused-1.txt");
        await File.WriteAllTextAsync(setAside, "taken");
        await File.WriteAllTextAsync(path, "not an answer");
        await Task.Delay(300);

        Assert.False(pending.IsCompleted, "The checkpoint must keep waiting, not fault.");
        Assert.True(File.Exists(path));
        File.Delete(setAside);
        for (var attempt = 0; attempt < 1000 && File.Exists(path); attempt++)
        {
            await Task.Delay(10);
        }

        Assert.Equal("not an answer", await File.ReadAllTextAsync(setAside));
        await File.WriteAllTextAsync(path, "none");
        Assert.Equal("none", await pending);
    }

    [Fact]
    public async Task AnUnansweredCheckpointTimesOutWithoutAnObservation()
    {
        using var output = new StringWriter();

        var answer = await NewConsole(output, TimeSpan.FromMilliseconds(100)).CheckpointAsync(AnyStep(), OperatorCheckpoint.After, CancellationToken.None);

        Assert.Null(answer);
        Assert.Contains("not answered in time", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ACancelledCheckpointReturnsWithoutAnObservation()
    {
        using var output = new StringWriter();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var answer = await NewConsole(output, TimeSpan.FromSeconds(30)).CheckpointAsync(AnyStep(), OperatorCheckpoint.After, cancellation.Token);

        Assert.Null(answer);
    }

    [Fact]
    public void ProgressLinesCarryIdentifiersKindsAndCodesOnly()
    {
        using var output = new StringWriter();
        var console = NewConsole(output, TimeSpan.FromSeconds(1));

        console.Dispatching(AnyStep(), 40, 70);
        console.Completed(AnyStep(), 40, 70, TestSupport.Outcome(ProbeOutcomeKind.MpFailed), 1234);

        var lines = output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.StartsWith("[040/070] p24-r0 (probe 24): dispatched at ", lines[0], StringComparison.Ordinal);
        Assert.Equal("[040/070] p24-r0: MpFailed, MP code 3, 1234 ms.", lines[1]);
    }

    [Theory]
    [InlineData("none", "none")]
    [InlineData("NONE", "none")]
    [InlineData("none; InteractionMode registry value 2", "none; InteractionMode registry value 2")]
    [InlineData("dialog: Blank 'High' Plane Not Allowed!", "dialog: Blank 'High' Plane Not Allowed!")]
    [InlineData("dialog:No objects matched specified criteria.", "dialog: No objects matched specified criteria.")]
    [InlineData("dialog:  two   spaces", "dialog: two spaces")]
    public void ValidObservationsAreNormalized(string raw, string expected)
    {
        Assert.True(OperatorObservationText.TryNormalize(raw, out var observation));
        Assert.Equal(expected, observation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nonexistent")]
    [InlineData("ok")]
    [InlineData("dialog:")]
    [InlineData("dialog: two\nlines")]
    [InlineData(@"dialog: C:\path")]
    [InlineData("dialog: `code`")]
    [InlineData("dialog: a | b")]
    [InlineData("dialog: non-ASCII ’ apostrophe")]
    public void InvalidObservationsAreRefused(string raw)
    {
        Assert.False(OperatorObservationText.TryNormalize(raw, out _));
    }

    [Fact]
    public void LongObservationsAreRefused()
    {
        Assert.True(OperatorObservationText.TryNormalize("dialog: " + new string('x', OperatorObservationText.MaximumLength - 8), out _));
        Assert.False(OperatorObservationText.TryNormalize("dialog: " + new string('x', OperatorObservationText.MaximumLength - 7), out _));
    }
}
