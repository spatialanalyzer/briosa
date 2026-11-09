using System.Text.Json;
using Briosa.Worker.Control;
using Google.Protobuf;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.LicensedProbes.Tests;

// The third licensed session's probes: #313 modal-dialog detection (26 to 28) and the
// InteractionMode setting (29), the runtime-select sample (30, #350), the fit seed omitted
// (31, #357), the typed partial success (32, #360), and USMN's excluded groups omitted or
// empty (33, briosa-dotnet#39). Probes 26 to 29 are opt-in.
public sealed class SessionThreeProbeTests
{
    private static readonly FixedClock Clock = new();
    private static readonly InteractionModeSettings Manual = new(Api.SaInteractionMode.Manual, Supplied: true);

    // Probe 33's USMN errors. Their digits must never reach a record.
    private const double Rms = 0.0173205;
    private const double Maximum = 0.0271828;

    private static ProbePlan Select(ProbePhase phase, params int[] probes) =>
        ProbePlan.Create(phase, TestSupport.SentinelManifest(), [], Manual, probes);

    private static ProbeStep Step(ProbePlan plan, string id) => plan.Steps.Single(step => step.Id == id);

    private static Task<ProbeSessionRecord> Run(ProbePlan plan, Func<ProbeStep, ProbeOutcome> respond, FakeOperator? console = null) =>
        ProbeSession.RunAsync(plan, new FakeTransport(plan.Phase, respond), Clock, new SessionIdentity(), console ?? new FakeOperator(),
            CancellationToken.None);

    // What the shipped server reports when its watchdog finds a SpatialAnalyzer dialog (PR #355).
    private static ProbeOutcome ServerReports(string diagnosticCode, StatusCode status = StatusCode.Unavailable)
    {
        var error = new Api.OperationError
        {
            Kind = Api.OperationFailureKind.WorkerWatchdogTimeout,
            DiagnosticCode = diagnosticCode,
            ExecutionDisposition = Api.ExecutionDisposition.StartedOutcomeUnknown,
            RecoveryGuidance = diagnosticCode == StallExpectation.ModalDialogDiagnosticCode
                ? Api.RecoveryGuidance.OperatorInterventionRequired
                : Api.RecoveryGuidance.WorkerReplacement
        };
        var trailers = new Metadata { { PublicOutcomes.ErrorTrailerName, error.ToByteArray() } };
        return PublicOutcomes.FromRpcException(new RpcException(new Status(status, "detail"), trailers));
    }

    private static ProbeOutcome StallResponder(ProbeStep step, ProbeOutcome stall) =>
        step.ExpectedStall is not null ? stall : TestSupport.Succeeding(step);

    // Selection and opt-in probes

    [Theory]
    [InlineData(false, 26)]
    [InlineData(false, 27)]
    [InlineData(false, 29)]
    [InlineData(true, 28)]
    public void AnOptInProbeRunsOnlyWhenSelected(bool worker, int probe)
    {
        var phase = worker ? ProbePhase.Worker : ProbePhase.PublicApi;
        var text = probe.ToString(System.Globalization.CultureInfo.InvariantCulture);

        Assert.DoesNotContain(ProbePlan.Create(phase, TestSupport.SentinelManifest()).Steps, step => step.Probe == text);
        var plan = Select(phase, probe);
        Assert.Equal([text], plan.ProbesCovered());
        Assert.Equal([probe], plan.SelectedProbes);
        Assert.Contains(ProbePlan.SelectionStatement([probe]), plan.RenderDryRun(), StringComparison.Ordinal);
    }

    [Fact]
    public void ASelectionKeepsEveryGuardAndFixtureStepAndOnlyTheSelectedProbes()
    {
        var full = ProbePlan.Create(ProbePhase.PublicApi, TestSupport.SentinelManifest());
        var plan = Select(ProbePhase.PublicApi, 31);

        Assert.Equal(
            full.Steps.Where(static step => step.Kind is ProbeStepKind.Guard or ProbeStepKind.Setup || step.Probe == "31").Select(static step => step.Id),
            plan.Steps.Select(static step => step.Id));
        Assert.Equal(plan.Steps, plan.CatalogSteps);
        Assert.Empty(plan.ExcludedProbes);
        Assert.False(plan.SetsInteractionMode);
    }

    [Fact]
    public void TheOptionsParseASelection()
    {
        var options = ProbeOptions.Parse(["--phase", "public-api", "--dry-run", "--probe", "30", "--probe", "26"]);

        Assert.Equal([26, 30], options.SelectedProbes);
        Assert.Empty(options.ExcludedProbes);
    }

    [Theory]
    [InlineData("selected more than once", "--probe", "30", "--probe", "30")]
    [InlineData("names a retired probe", "--probe", "22")]
    [InlineData("not a probe in the approved list", "--probe", "34")]
    [InlineData("takes one probe number", "--probe", "030")]
    [InlineData("cannot be combined", "--probe", "30", "--exclude-probe", "2")]
    [InlineData("names an opt-in probe", "--exclude-probe", "29")]
    [InlineData("do not apply", "--probe", "30", ProbeOptions.RestoreOnlyFlag, ProbeOptions.RestoreModeOption, "manual")]
    public void AnInvalidSelectionIsAUsageError(string message, params string[] arguments)
    {
        var exception = Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse(["--phase", "public-api", "--dry-run", .. arguments]));

        Assert.Contains(message, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, "select all of them or none", 5)]
    [InlineData(true, "--probe 2", 17)]
    [InlineData(true, "no steps in the worker phase", 26)]
    [InlineData(false, "no steps in the public-api phase", 28)]
    [InlineData(false, "select one of them per run", 26, 27)]
    [InlineData(false, "run it alone (--probe 29)", 29, 30)]
    [InlineData(false, "run it alone (--probe 29)", 26, 29)]
    public void ASelectionThatCannotFormAPlanIsRefused(bool worker, string message, params int[] probes)
    {
        var exception = Assert.Throws<ProbeUsageException>(() => Select(worker ? ProbePhase.Worker : ProbePhase.PublicApi, probes));

        Assert.Contains(message, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectionAndExclusionCannotBeCombined()
    {
        Assert.Throws<ProbeUsageException>(() =>
            ProbePlan.Create(ProbePhase.PublicApi, TestSupport.SentinelManifest(), [2], Manual, [30]));
    }

    // Probes 26 to 28: planned stalls

    [Theory]
    [InlineData(false, 26, "p26-zero-match-stall")]
    [InlineData(false, 27, "p27-chart-missing-stall")]
    [InlineData(true, 28, "p28-blank-high-stall")]
    public void EachStallProbeEndsItsPlanAfterItsManualBaseline(bool worker, int probe, string id)
    {
        var plan = Select(worker ? ProbePhase.Worker : ProbePhase.PublicApi, probe);
        var stall = plan.Steps[^1];
        var baseline = plan.Steps[^2];

        Assert.Equal(id, stall.Id);
        Assert.Same(stall, plan.StallStep);
        Assert.Equal(ProbeOutcomes.None, stall.Acceptable);
        Assert.Equal(OperatorCheckpoint.Before | OperatorCheckpoint.After, stall.Checkpoints);
        Assert.Equal([baseline.Id], stall.Prerequisites);
        Assert.Equal(Api.SaInteractionMode.Manual, Assert.IsType<Api.SetInteractionModeRequest>(baseline.Request).SaInteractionMode);
        Assert.Equal(ProbeOutcomes.Succeeded, baseline.Acceptable);
        if (worker)
        {
            Assert.Same(StallExpectation.HarnessReportsModalDialog, stall.ExpectedStall);
            Assert.Equal(ProbeCatalog.WorkerStallClientTimeout, stall.ClientTimeout);
            Assert.Equal((VariantKind.BlankArgument, ProbeCatalog.HighPlaneLabel), (stall.Variant.Kind, stall.Variant.ArgumentLabel));
        }
        else
        {
            // The server's 2-minute quick budget, which the runbook sets, ends a public stall first.
            Assert.Same(StallExpectation.ServerReportsModalDialog, stall.ExpectedStall);
            Assert.True(stall.ClientTimeout > TimeSpan.FromMinutes(2));
            Assert.Equal(ProbePlan.UnchangedSetting, plan.ServerInteractionModeSetting);
        }
    }

    [Fact]
    public void TheStallTriggersAreTheDialogsLicensedSessionsObserved()
    {
        var zeroMatch = Assert.IsType<Api.MakeCollectionObjectNameRefListWildcardSelectionRequest>(Select(ProbePhase.PublicApi, 26).Steps[^1].Request);
        var chart = Assert.IsType<Api.CreateChartFromVectorGroupRequest>(Select(ProbePhase.PublicApi, 27).Steps[^1].Request);

        Assert.Equal((FixtureNames.Collection, "NOMATCH25*"), (zeroMatch.CollectionWildcardCriteria, zeroMatch.ObjectWildcardCriteria));
        Assert.Equal(ProbeCatalog.MissingTemplateChart, chart.TemplateChartName.Name);
        Assert.Contains(ProbePlan.DescribeSequence(Select(ProbePhase.Worker, 28).Steps[^1]),
            static line => line.EndsWith($"\"{ProbeCatalog.HighPlaneLabel}\" = BLANK", StringComparison.Ordinal));
    }

    [Fact]
    public void AStallPlanNeedsTheManualRestoreMode()
    {
        var exception = Assert.Throws<ProbeUsageException>(() => ProbePlan.Create(ProbePhase.PublicApi, TestSupport.SentinelManifest(), [],
            new InteractionModeSettings(Api.SaInteractionMode.Automatic, Supplied: true), [26]));

        Assert.Contains($"{ProbeOptions.RestoreModeOption} manual", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidationKeepsAStallStepLastAndAloneAndChecked()
    {
        var steps = Select(ProbePhase.PublicApi, 26).Steps.ToList();
        var stall = steps[^1];

        ProbePlan.Validate(ProbePhase.PublicApi, steps);
        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi, [.. steps[..^2], stall, steps[^2]]));
        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi,
            [.. steps, stall with { Id = "second-stall", Prerequisites = [] }]));
        foreach (var changed in new[]
        {
            stall with { Acceptable = ProbeOutcomes.MpFailed },
            stall with { Checkpoints = OperatorCheckpoint.After },
            stall with { ClientTimeout = null },
            stall with { CreatesFixture = "plane:X" }
        })
        {
            Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi, [.. steps[..^1], changed]));
        }
    }

    [Fact]
    public void OnlyTheServersModalDialogReportMatchesAPublicStall()
    {
        var expected = StallExpectation.ServerReportsModalDialog;
        var reported = ServerReports(StallExpectation.ModalDialogDiagnosticCode);

        Assert.True(expected.Matches(reported));
        Assert.Equal(("grpc:Unavailable", "WorkerWatchdogTimeout", "OperatorInterventionRequired", "StartedOutcomeUnknown"),
            (reported.Transport, reported.FailureKind, reported.RecoveryGuidance, reported.ExecutionDisposition));

        // The generic watchdog code, a client deadline, and a dismissed dialog are not the planned stall.
        Assert.False(expected.Matches(ServerReports("worker-execution-watchdog-timeout")));
        Assert.False(expected.Matches(ServerReports(StallExpectation.ModalDialogDiagnosticCode, StatusCode.DeadlineExceeded)));
        Assert.False(expected.Matches(TestSupport.Outcome(ProbeOutcomeKind.MpFailed)));
        Assert.False(StallExpectation.HarnessReportsModalDialog.Matches(reported));
    }

    [Fact]
    public async Task AnObservedStallEndsTheRunCompletedAndRecordsWhatTheServerReported()
    {
        var plan = Select(ProbePhase.PublicApi, 26);
        var console = new FakeOperator(static (step, _) =>
            step.ExpectedStall is null ? "none" : "dialog: No objects matched specified criteria. (left open; dismissed after the result)");

        var record = await Run(plan, step => StallResponder(step, ServerReports(StallExpectation.ModalDialogDiagnosticCode)), console);

        Assert.True(record.Completed);
        Assert.True(record.EndedInPlannedStall);
        Assert.Null(record.StopReason);
        Assert.All(record.Steps, static step => Assert.Equal(StepClassification.Observed, step.Classification));
        Assert.Equal(["p26-baseline:after", "p26-zero-match-stall:before", "p26-zero-match-stall:after"],
            console.Checkpoints.Where(static name => name.StartsWith("p26", StringComparison.Ordinal)));

        using var document = JsonDocument.Parse(ProbeRecorder.ToJson(record));
        var root = document.RootElement;
        var stall = root.GetProperty("steps").EnumerateArray().Last();
        Assert.True(root.GetProperty("ended_in_planned_stall").GetBoolean());
        Assert.Equal("only its planned stall", stall.GetProperty("acceptable").GetString());
        Assert.Equal("sa-modal-dialog-open", stall.GetProperty("expected_stall").GetProperty("diagnostic_code").GetString());
        Assert.Equal("OperatorInterventionRequired", stall.GetProperty("outcome").GetProperty("recovery_guidance").GetString());
        var markdown = ProbeRecorder.ToMarkdown(record);
        Assert.Contains("Stalled as planned.", markdown, StringComparison.Ordinal);
        Assert.Contains("Recovery guidance `OperatorInterventionRequired`.", markdown, StringComparison.Ordinal);
        Assert.Contains("stall recovery", markdown, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("dismissed")]
    [InlineData("generic")]
    public async Task AStallThatIsNotThePlannedOneIsUnexpected(string variant)
    {
        var plan = Select(ProbePhase.PublicApi, 27);
        var outcome = variant == "dismissed"
            ? TestSupport.Outcome(ProbeOutcomeKind.MpFailed)
            : ServerReports("worker-execution-watchdog-timeout");

        var record = await Run(plan, step => StallResponder(step, outcome));

        Assert.False(record.Completed);
        Assert.False(record.EndedInPlannedStall);
        Assert.Equal("p27-chart-missing-stall", record.StoppedAt);
        Assert.Equal(variant == "dismissed" ? ProbeSession.UnexpectedDispositionStop : ProbeSession.UnknownOutcomeStop, record.StopReason);
        Assert.Equal(StepClassification.Unexpected, record.Steps[^1].Classification);
    }

    // Probe 28: the worker phase runs the server's own detector when a step's deadline ends it

    [Theory]
    [InlineData(true, "sa-modal-dialog-open")]
    [InlineData(false, WorkerTransport.TimeoutDiagnosticCode)]
    public void AWorkerStepTimeoutReportsAnOpenDialogAsTheServerDoes(bool open, string expected)
    {
        Assert.Equal(expected, WorkerTransport.StalledStepDiagnostic(() => open));
    }

    [Fact]
    public void AFailedOrMissingDetectorKeepsTheTimeoutCode()
    {
        Assert.Equal(WorkerTransport.TimeoutDiagnosticCode, WorkerTransport.StalledStepDiagnostic(null));
        Assert.Equal(WorkerTransport.TimeoutDiagnosticCode,
            WorkerTransport.StalledStepDiagnostic(static () => throw new InvalidOperationException("The window list could not be read.")));
    }

    [Fact]
    public async Task AWorkerStallIsObservedWhenTheDetectorSawTheDialog()
    {
        var plan = Select(ProbePhase.Worker, 28);
        var stall = ProbeOutcome.Unknown(WorkerTransport.TimeoutTransport, WorkerTransport.StalledStepDiagnostic(static () => true));

        var record = await Run(plan, step => StallResponder(step, stall));

        Assert.True(record.Completed);
        Assert.True(record.EndedInPlannedStall);
        Assert.Null(record.ServerInteractionModeSetting);
        Assert.False(StallExpectation.HarnessReportsModalDialog.Matches(ProbeOutcome.Unknown(WorkerTransport.TimeoutTransport, WorkerTransport.TimeoutDiagnosticCode)));
    }

    [Fact]
    public async Task OnlyTheWorkerTransportRunsTheServersDetector()
    {
        var options = ProbeOptions.Parse(["--phase", "worker", ProbeTarget.ConfirmFlag, "--fixtures", "manifest.json", "--output-directory", "out",
            "--worker-path", "Briosa.Worker.exe", "--connected-sa-attested-version", ProbeTarget.SpatialAnalyzerTarget,
            "--connected-sa-attestation-reference", "probe-s3"]);

        // Creating the transport starts nothing; only a step timeout calls the detector.
        var transport = Assert.IsType<WorkerTransport>(LicensedProbeProgram.CreateTransport(options));
        await using (transport.ConfigureAwait(false))
        {
            Assert.True(transport.DetectsModalDialogs);
        }
    }

    // Probe 29: the server's InteractionMode setting

    [Fact]
    public void Probe29RunsAloneUnderTheServersSilentSettingAndEndsByRestoring()
    {
        var plan = Select(ProbePhase.PublicApi, 29);
        var probe = plan.Steps.Where(static step => step.Probe == "29").ToList();

        Assert.Equal(["c29-zero-match-setting", "p29-chart-missing-setting", "p29-restore"], probe.Select(static step => step.Id));
        Assert.Equal("Silent", plan.ServerInteractionModeSetting);
        Assert.True(plan.ServerSetsSilent);
        Assert.Equal(["--Briosa:SpatialAnalyzer:InteractionMode=Silent"], plan.ServerSettingArguments);
        Assert.Equal("p29-restore", plan.Steps[^1].Id);
        Assert.Equal(Api.SaInteractionMode.Manual, Assert.IsType<Api.SetInteractionModeRequest>(plan.Steps[^1].Request).SaInteractionMode);
        Assert.All(probe, static step =>
        {
            Assert.Equal(Api.SaInteractionMode.Silent, step.ServerInteractionMode);
            Assert.True(step.Checkpoints.HasFlag(OperatorCheckpoint.After));
        });
        Assert.All(probe.SkipLast(1), static step => Assert.Equal(
            (ProbeOutcome.FactPrefix + "mp_result_code", "3"), (step.Hypotheses.Single().ObservationKey, step.Hypotheses.Single().ExpectedValue)));
        Assert.Null(plan.StallStep);
    }

    [Fact]
    public void ValidationRefusesTheSettingPlanWithoutItsRestore()
    {
        var steps = Select(ProbePhase.PublicApi, 29).Steps;

        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi, [.. steps.SkipLast(1)]));
        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.Worker,
            [.. steps.Select(static step => step with { Phase = ProbePhase.Worker })]));
    }

    [Theory]
    [InlineData(false, "--Briosa:SpatialAnalyzer:InteractionMode=Unchanged")]
    [InlineData(true, "--Briosa:SpatialAnalyzer:InteractionMode=Silent")]
    public void ThePublicDryRunPrintsTheServerSettingForTheOperatorScript(bool setting, string expected)
    {
        var plan = setting ? Select(ProbePhase.PublicApi, 29) : ProbePlan.Create(ProbePhase.PublicApi, TestSupport.SentinelManifest());

        // The operator script passes every dry-run line that starts with two spaces and --Briosa:.
        Assert.Single(plan.RenderDryRun().Split('\n'), line => line == "  " + expected);
        Assert.Empty(ProbePlan.Create(ProbePhase.Worker, TestSupport.SentinelManifest()).ServerSettingArguments);
        Assert.DoesNotContain("InteractionMode=", ProbePlan.Create(ProbePhase.Worker, TestSupport.SentinelManifest()).RenderDryRun(),
            StringComparison.Ordinal);
    }

    // Probe 30: the runtime-select sample. Its vector-name-list step was retired with its
    // operation after licensed session 3 (#363).

    [Fact]
    public void TheRuntimeSelectSampleCoversFourListGetterKindsAndThreeSingleGetters()
    {
        var plan = Select(ProbePhase.PublicApi, 30);
        var steps = plan.Steps.Where(static step => step.Probe == "30").ToList();

        Assert.Equal(
            [
                ("p30-point-list", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg"),
                ("p30-object-list", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg"),
                ("p30-relationship-list", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg"),
                ("p30-instrument-list", WorkerMpValueKind.CollectionInstrumentIdList, "GetColInstIdRefListArg"),
                ("p30-instrument", WorkerMpValueKind.CollectionInstrumentId, "GetColInstIdArg"),
                ("p30-point", WorkerMpValueKind.PointName, "GetPointNameArg"),
                ("p30-collection", WorkerMpValueKind.CollectionName, "GetCollectionNameArg")
            ],
            steps.Select(static step => (step.Id, step.Command.OutputArguments.Single().Kind, step.Command.OutputArguments.Single().SdkBinding!)));
        Assert.All(steps, static step =>
        {
            Assert.True(step.RequiresOperator);
            Assert.Equal(OperatorCheckpoint.After, step.Checkpoints);
            Assert.Equal(ProbeOutcomes.AnyDeterminateSdkOutcome, step.Acceptable);
            var prompt = step.Command.InputArguments[0];
            Assert.Equal("User Prompt", prompt.Name);
            var text = prompt.RequireValue<WorkerTextValue>().Value;
            Assert.StartsWith("Briosa probe 30: press F2, check ", text, StringComparison.Ordinal);
            Assert.EndsWith(", click OK, then press Enter.", text, StringComparison.Ordinal);
            Assert.DoesNotContain(TestSupport.Sentinel, text, StringComparison.Ordinal);
        });
        Assert.All(steps, step => Assert.Contains(
            $"--Briosa:Security:Operations:Overrides:{OperationOf(step).Replace('.', ':')}=allow", plan.ServerAdmissionArguments));

        string OperationOf(ProbeStep step) => step.Command.OperationId;
    }

    [Fact]
    public void RuntimeSelectObservationsAreACountAndOneFlag()
    {
        var plan = Select(ProbePhase.PublicApi, 30);
        var request = new Api.MakePointNameRefListRuntimeSelectRequest();
        var operation = (ProbeOperation<Api.MakePointNameRefListRuntimeSelectRequest, Api.MakePointNameRefListRuntimeSelectResult>)
            Step(plan, "p30-point-list").Operation;

        Assert.Equal(("1", "true"), Observe(Point("P1")));
        Assert.Equal(("1", "false"), Observe(Point("P2")));
        Assert.Equal(("2", "false"), Observe(Point("P1"), Point("P2")));
        Assert.Equal(("0", "false"), Observe());

        (string Count, string Requested) Observe(params Api.PointName[] points)
        {
            var result = new Api.MakePointNameRefListRuntimeSelectResult();
            result.ResultantPointNameList.AddRange(points);
            var observations = operation.ObservePublic(request, result);

            // Counts and flags only: no name reaches a record.
            Assert.Equal(["selection_count", "selection_is_requested"], observations.Keys);
            return (observations["selection_count"], observations["selection_is_requested"]);
        }

        static Api.PointName Point(string target) =>
            new() { CollectionName = FixtureNames.Collection, GroupName = FixtureNames.GroupOne, TargetName = target };
    }

    // Both instrument getters read the same designated fixture, so each is a control for
    // the other: the first USMN instrument matches, and the second USMN instrument does not.
    [Fact]
    public void BothInstrumentStepsAskForTheSameDesignatedInstrument()
    {
        var plan = Select(ProbePhase.PublicApi, 30);
        var list = Step(plan, "p30-instrument-list");
        var single = Step(plan, "p30-instrument");
        var listOperation = (ProbeOperation<Api.MakeCollectionInstrumentRefListRuntimeSelectRequest, Api.MakeCollectionInstrumentRefListRuntimeSelectResult>)list.Operation;
        var singleOperation = (ProbeOperation<Api.MakeCollectionInstrumentIdRuntimeSelectRequest, Api.MakeCollectionInstrumentIdRuntimeSelectResult>)single.Operation;
        var fixtures = TestSupport.SentinelManifest().UsmnInstruments;
        var prompt = Assert.IsType<Api.MakeCollectionInstrumentRefListRuntimeSelectRequest>(list.Request).UserPrompt;

        Assert.Equal(prompt, Assert.IsType<Api.MakeCollectionInstrumentIdRuntimeSelectRequest>(single.Request).UserPrompt);
        Assert.Contains("the first USMN fixture instrument", prompt, StringComparison.Ordinal);
        Assert.Equal(("1", "true"), ListSelection(fixtures[0]));
        Assert.Equal(("1", "false"), ListSelection(fixtures[1]));
        Assert.Equal(("1", "true"), SingleSelection(fixtures[0]));
        Assert.Equal(("1", "false"), SingleSelection(fixtures[1]));

        var nothing = singleOperation.ObservePublic(new Api.MakeCollectionInstrumentIdRuntimeSelectRequest(),
            new Api.MakeCollectionInstrumentIdRuntimeSelectResult());
        Assert.Equal(("0", "false"), (nothing["selection_count"], nothing["selection_is_requested"]));

        (string Count, string Requested) ListSelection(ManifestInstrument fixture)
        {
            var result = new Api.MakeCollectionInstrumentRefListRuntimeSelectResult();
            result.ResultantCollectionInstrumentRefList.Add(Instrument(fixture));
            var observations = listOperation.ObservePublic(new Api.MakeCollectionInstrumentRefListRuntimeSelectRequest(), result);
            return (observations["selection_count"], observations["selection_is_requested"]);
        }

        (string Count, string Requested) SingleSelection(ManifestInstrument fixture)
        {
            var observations = singleOperation.ObservePublic(new Api.MakeCollectionInstrumentIdRuntimeSelectRequest(),
                new Api.MakeCollectionInstrumentIdRuntimeSelectResult { InstrumentId = Instrument(fixture) });
            return (observations["selection_count"], observations["selection_is_requested"]);
        }

        static Api.CollectionInstrumentId Instrument(ManifestInstrument fixture) =>
            new() { CollectionName = fixture.Collection, InstrumentId = fixture.InstrumentId };
    }

    [Fact]
    public void ASingleValueRuntimeSelectWithNoValueCountsNothing()
    {
        var operation = (ProbeOperation<Api.MakeCollectionNameRuntimeSelectRequest, Api.MakeCollectionNameRuntimeSelectResult>)
            Step(Select(ProbePhase.PublicApi, 30), "p30-collection").Operation;

        var empty = operation.ObservePublic(new Api.MakeCollectionNameRuntimeSelectRequest(), new Api.MakeCollectionNameRuntimeSelectResult());
        var named = operation.ObservePublic(new Api.MakeCollectionNameRuntimeSelectRequest(), new Api.MakeCollectionNameRuntimeSelectResult
        {
            ResultantCollectionName = new Api.CollectionName { Name = FixtureNames.Collection }
        });

        Assert.Equal(("0", "false"), (empty["selection_count"], empty["selection_is_requested"]));
        Assert.Equal(("1", "true"), (named["selection_count"], named["selection_is_requested"]));
    }

    // Probe 31: the fit seed omitted

    [Theory]
    [InlineData("p31-points-value", true)]
    [InlineData("p31-points-omitted", false)]
    [InlineData("p31-projected-omitted", false)]
    public void TheOmittedSeedFitsSendNoSeedSetter(string id, bool seeded)
    {
        var step = Step(Select(ProbePhase.PublicApi, 31), id);

        Assert.Equal(seeded, step.Command.InputArguments.Any(static argument => argument.Name == ProbeCatalog.SeedGeometryLabel));
        Assert.Equal(VariantKind.Shipped, step.Variant.Kind);
        Assert.Equal(OperatorCheckpoint.After, step.Checkpoints);
    }

    [Fact]
    public void TheFitsUseGfitAndEachIsCountedByItsOwnName()
    {
        var plan = Select(ProbePhase.PublicApi, 31);
        var points = Assert.IsType<Api.FitGeometryToPointsRequest>(Step(plan, "p31-points-omitted").Request);
        var projected = Assert.IsType<Api.FitGeometryToPointGroupProjectedToPlaneRequest>(Step(plan, "p31-projected-omitted").Request);

        Assert.Equal(ProbeCatalog.FitPoints.Select(static point => (FixtureNames.GroupFit, point.Name)),
            points.PointsToFit.Select(static point => (point.GroupName, point.TargetName)));
        Assert.Null(points.StartingConditionGeometry);
        Assert.Equal((Api.GeometryType.Circle, FixtureNames.GroupFit, FixtureNames.PlaneA, Api.ObjectType.Circle),
            (projected.GeometryType, projected.GroupToFit.ObjectName, projected.PlaneName.ObjectName, projected.ResultingObjectName.ObjectType));
        Assert.Null(projected.StartingConditionGeometry);
        foreach (var (check, name, type) in new[]
        {
            ("c31-points-value", "FIT31V", Api.ObjectType.Plane),
            ("c31-points-omitted", "FIT31O", Api.ObjectType.Plane),
            ("c31-projected-omitted", "FIT31C", Api.ObjectType.Circle)
        })
        {
            var count = Assert.IsType<Api.MakeCollectionObjectNameRefListWildcardSelectionRequest>(Step(plan, check).Request);
            Assert.Equal((FixtureNames.Collection, name, type), (count.CollectionWildcardCriteria, count.ObjectWildcardCriteria, count.ObjectType));
            Assert.Equal(("match_count", "1"), (Step(plan, check).Hypotheses.Single().ObservationKey, Step(plan, check).Hypotheses.Single().ExpectedValue));
        }
    }

    [Fact]
    public void GfitsCornersLieOnOneCircleInPa()
    {
        var radii = ProbeCatalog.FitPoints.Select(static point => Math.Sqrt(Math.Pow(point.X - 50, 2) + Math.Pow(point.Y - 50, 2))).ToList();

        Assert.All(radii, radius => Assert.Equal(radii[0], radius, 9));
        Assert.All(ProbeCatalog.FitPoints, static point => Assert.Equal(0, point.Z));
    }

    // Probe 32: the typed partial success

    [Fact]
    public void Probe32FitsTheNonCoplanarGroupAndExpectsATypedPartialSuccess()
    {
        var plan = Select(ProbePhase.PublicApi, 32);
        var fit = Step(plan, "p32-partial");
        var request = Assert.IsType<Api.FitGeometryToPointGroupRequest>(fit.Request);

        Assert.Equal(FixtureNames.GroupOne, request.GroupToFit.ObjectName);
        Assert.Contains(ProbeCatalog.GroupOnePoints, static point => point.Z != 0);
        Assert.Equal(
            [(ProbeOutcome.FactPrefix + "execution_state", "PartialSuccess"), (ProbeOutcome.FactPrefix + "mp_result_code", "4")],
            fit.Hypotheses.Select(static hypothesis => (hypothesis.ObservationKey, hypothesis.ExpectedValue)));
        Assert.Equal(["p32-partial"], Step(plan, "c32-partial").Prerequisites);
    }

    [Fact]
    public async Task APartialSuccessMatchesProbe32sHypothesesEndToEnd()
    {
        var plan = Select(ProbePhase.PublicApi, 32);
        var execution = new Api.MpExecutionDetails { State = Api.MpExecutionState.PartialSuccess, MpResultCode = 4 };

        var record = await Run(plan, step => step.Id switch
        {
            "p32-partial" => PublicOutcomes.FromSuccess(execution, ProbeOutcome.EmptyObservations),
            "c32-partial" => TestSupport.Outcome(ProbeOutcomeKind.Succeeded, ("match_count", "1")),
            _ => TestSupport.Succeeding(step)
        });

        Assert.True(record.Completed);
        var fit = record.Steps.Single(static step => step.Step.Id == "p32-partial");
        Assert.All(fit.Hypotheses, static hypothesis => Assert.True(hypothesis.Matched));
        Assert.Equal("PartialSuccess", fit.Outcome!.ExecutionState);
        Assert.Contains("Execution state `PartialSuccess`.", ProbeRecorder.ToMarkdown(record), StringComparison.Ordinal);
    }

    [Fact]
    public void AnOutcomeFactNamesAFieldOrElseAnObservation()
    {
        var outcome = TestSupport.Outcome(ProbeOutcomeKind.Succeeded, ("match_count", "1")) with { ExecutionState = "Succeeded" };

        Assert.Equal("2", outcome.Fact("outcome.mp_result_code"));
        Assert.Equal("Succeeded", outcome.Fact("outcome.execution_state"));
        Assert.Equal("Succeeded", outcome.Fact("outcome.kind"));
        Assert.Equal("1", outcome.Fact("match_count"));
        Assert.Null(outcome.Fact("outcome.match_count"));
        Assert.Null(outcome.Fact("outcome.recovery_guidance"));
    }

    [Fact]
    public void AnOutcomeFactCountsTheRetrievedOutputs()
    {
        var retrieved = new OutputObservation("RMS Error Value", true);
        var missing = new OutputObservation("Max Error Value", false);

        Assert.Equal("2 of 2",
            (TestSupport.Outcome(ProbeOutcomeKind.Succeeded) with { Outputs = [retrieved, retrieved] }).Fact("outcome.outputs_retrieved"));
        Assert.Equal("1 of 2",
            (TestSupport.Outcome(ProbeOutcomeKind.OutputRetrievalFailed) with { Outputs = [retrieved, missing] }).Fact("outcome.outputs_retrieved"));
        Assert.Null(TestSupport.Outcome(ProbeOutcomeKind.MpFailed).Fact("outcome.outputs_retrieved"));
    }

    // Probe 33: USMN excluded groups omitted or empty

    // A worker-shaped outcome produced by the real classifier and the step's own observations.
    private static ProbeOutcome FromWorker(ProbeStep step, WorkerMpExecutionResult execution) =>
        WorkerOutcomes.FromResponse(
            new WorkerExecutionResponse(WorkerExecutionResponseStatus.Completed, execution,
                new WorkerConnectionSnapshot(WorkerConnectionState.Connected, WorkerExecutionReadinessState.ExecutionReady, 0, 1, 1,
                    "connect-ex-connected", DateTimeOffset.UnixEpoch),
                null),
            outputs => step.Operation.ObserveWorker(step.Request, outputs), step.Command.OperationId);

    // USMN's two outputs as the worker returns them after MP code 2.
    private static WorkerMpResultAvailable Usmn(double rms, double maximum) => new(2, 1,
    [
        WorkerMpOutputValue.FromRetrieval("RMS Error Value", WorkerMpValueKind.FloatingPoint, true, new WorkerDoubleValue(rms)),
        WorkerMpOutputValue.FromRetrieval("Max Error Value", WorkerMpValueKind.FloatingPoint, true, new WorkerDoubleValue(maximum))
    ], null);

    [Fact]
    public void Probe33RunsInTheWorkerPhaseOnly()
    {
        var plan = Select(ProbePhase.Worker, 33);

        Assert.Equal(["33"], plan.ProbesCovered());
        Assert.Equal(["p33-shipped", "p33-omit", "p33-empty"],
            plan.Steps.Where(static step => step.Probe == "33").Select(static step => step.Id));
        Assert.False(plan.SetsInteractionMode);
        Assert.Contains(ProbePlan.Create(ProbePhase.Worker, TestSupport.SentinelManifest()).Steps, static step => step.Id == "p33-empty");
        Assert.Equal([33], ProbeOptions.Parse(["--phase", "worker", "--dry-run", "--probe", "33"]).SelectedProbes);
        var exception = Assert.Throws<ProbeUsageException>(() => Select(ProbePhase.PublicApi, 33));
        Assert.Contains("no steps in the public-api phase", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Probe33SendsTheShippedControlThenNoSetterThenAnEmptyList()
    {
        var plan = Select(ProbePhase.Worker, 33);
        var control = Step(plan, "p33-shipped");
        var omitted = Step(plan, "p33-omit");
        var empty = Step(plan, "p33-empty");

        // The control is probe 1's shipped request: it excludes G3, which USMN does not use.
        var request = Assert.IsType<Api.LocateInstrumentsUsmnRequest>(control.Request);
        var excluded = Assert.Single(request.ExcludedGroups);
        Assert.Equal((FixtureNames.Collection, FixtureNames.GroupMeasured, Api.ObjectType.PointGroup),
            (excluded.CollectionName, excluded.ObjectName, excluded.ObjectType));
        Assert.Equal((VariantKind.Shipped, ProbeOutcomes.Succeeded), (control.Variant.Kind, control.Acceptable));
        Assert.Equal((VariantKind.OmitArgument, ProbeCatalog.ExcludedGroupsLabel), (omitted.Variant.Kind, omitted.Variant.ArgumentLabel));
        Assert.Equal((VariantKind.EmptyListArgument, ProbeCatalog.ExcludedGroupsLabel), (empty.Variant.Kind, empty.Variant.ArgumentLabel));
        foreach (var variant in new[] { omitted, empty })
        {
            Assert.Equal(request, variant.Request);
            Assert.Equal(ProbeOutcomes.AnyDeterminateSdkOutcome, variant.Acceptable);
            Assert.Equal(["p33-shipped"], variant.Prerequisites);
        }

        Assert.All(new[] { control, omitted, empty }, static step => Assert.Equal(OperatorCheckpoint.After, step.Checkpoints));

        var shipped = Shape(control.Command);
        Assert.Contains((ProbeCatalog.ExcludedGroupsLabel, WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", 1), shipped);
        Assert.Equal(shipped.Where(static argument => argument.Name != ProbeCatalog.ExcludedGroupsLabel), Shape(omitted.Command));
        Assert.Equal(
            shipped.Select(static argument => argument.Name == ProbeCatalog.ExcludedGroupsLabel
                ? (argument.Name, argument.Kind, argument.Binding, 0)
                : argument),
            Shape(empty.Command));

        // Each argument's label, kind, and binding, and how many items an object-name list sends.
        static List<(string Name, WorkerMpValueKind Kind, string? Binding, int Items)> Shape(WorkerMpCommand command) =>
            [.. command.InputArguments.Select(static argument => (argument.Name, argument.Kind, argument.SdkBinding,
                argument.Value is WorkerCollectionObjectNameListValue list ? list.Values.Count : -1))];
    }

    [Fact]
    public void Probe33ExpectsMpCode2BothOutputsAndForEachVariantTheControlsErrors()
    {
        var plan = Select(ProbePhase.Worker, 33);
        (string, string)[] ran = [(ProbeOutcome.FactPrefix + "mp_result_code", "2"), (ProbeOutcome.FactPrefix + "outputs_retrieved", "2 of 2")];
        (string, string)[] same = [.. ran, ("errors_equal_control", "true")];

        Assert.Equal(ran, Expected(Step(plan, "p33-shipped")));
        Assert.Equal(same, Expected(Step(plan, "p33-omit")));
        Assert.Equal(same, Expected(Step(plan, "p33-empty")));

        static (string, string)[] Expected(ProbeStep step) =>
            [.. step.Hypotheses.Select(static hypothesis => (hypothesis.ObservationKey, hypothesis.ExpectedValue))];
    }

    // The errors stay in the harness's memory: an observation says only whether a variant's
    // errors equal the control's.
    [Fact]
    public void Probe33ComparesEachVariantsErrorsWithTheControlsWithoutKeepingThem()
    {
        var plan = Select(ProbePhase.Worker, 33);
        var control = Step(plan, "p33-shipped");
        var omitted = Step(plan, "p33-omit");
        var empty = Step(plan, "p33-empty");

        Assert.Equal("no-control", Observe(omitted, Rms, Maximum)["errors_equal_control"]);
        Assert.Equal(["errors_captured"], Observe(control, Rms, Maximum).Keys);
        Assert.Equal("true", Observe(control, Rms, Maximum)["errors_captured"]);
        Assert.Equal("true", Observe(omitted, Rms, Maximum)["errors_equal_control"]);
        Assert.Equal("true", Observe(empty, Rms * (1 + 1e-9), Maximum)["errors_equal_control"]);
        Assert.Equal("false", Observe(empty, Rms * 1.001, Maximum)["errors_equal_control"]);
        Assert.Equal("false", Observe(omitted, Rms, Maximum * 1.001)["errors_equal_control"]);
        Assert.Equal(["errors_equal_control"], Observe(empty, Rms, Maximum).Keys);
        Assert.Equal("unreadable-output-shape", omitted.Operation.ObserveWorker(omitted.Request, [])["observation"]);

        static IReadOnlyDictionary<string, string> Observe(ProbeStep step, double rms, double maximum) =>
            step.Operation.ObserveWorker(step.Request, Usmn(rms, maximum).Outputs);
    }

    [Theory]
    [InlineData(0.0123456, 0.0123456, true)]
    [InlineData(0.0123456, 0.01234561, true)]
    [InlineData(0.0123456, 0.0123457, false)]
    [InlineData(0.0, 0.0, true)]
    [InlineData(0.0, 1e-12, true)]
    [InlineData(0.0, 1e-6, false)]
    [InlineData(double.NaN, double.NaN, false)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity, false)]
    public void ErrorsAreEqualWithinOnePartPerMillion(double control, double value, bool equal) =>
        Assert.Equal(equal, UsmnErrorComparison.Equal(control, value));

    [Fact]
    public void TheDryRunShowsTheSkippedSetterAndTheEmptyListButNoValue()
    {
        var plan = Select(ProbePhase.Worker, 33);
        var binding = $"SetCollectionObjectNameRefListArg \"{ProbeCatalog.ExcludedGroupsLabel}\"";

        Assert.Contains($"{binding} = value", ProbePlan.DescribeSequence(Step(plan, "p33-shipped")));
        Assert.Contains($"{binding} OMITTED (setter not called)", ProbePlan.DescribeSequence(Step(plan, "p33-omit")));
        Assert.Contains($"{binding} = EMPTY LIST", ProbePlan.DescribeSequence(Step(plan, "p33-empty")));
        Assert.DoesNotContain(TestSupport.Sentinel, plan.RenderDryRun(), StringComparison.Ordinal);
    }

    // The worker control channel carries the empty list unchanged, so the worker hands SA no items.
    [Fact]
    public void TheEmptyListCrossesTheWorkerControlChannel()
    {
        var command = Step(Select(ProbePhase.Worker, 33), "p33-empty").Command;
        using var stream = new MemoryStream();
        using (var sender = new WorkerControlChannel(stream, leaveOpen: true))
        {
            sender.Send(WorkerControlMessage.Execute(Guid.NewGuid(), command));
        }

        stream.Position = 0;
        using var receiver = new WorkerControlChannel(stream, leaveOpen: true);
        var received = Assert.Single(receiver.Receive().Command!.InputArguments,
            static argument => argument.Name == ProbeCatalog.ExcludedGroupsLabel);

        Assert.Empty(Assert.IsType<WorkerCollectionObjectNameListValue>(received.Value).Values);
        Assert.Equal("SetCollectionObjectNameRefListArg", received.SdkBinding);
    }

    [Fact]
    public async Task ListsThatSaRunsCorrectlyMatchEveryProbe33HypothesisEndToEnd()
    {
        var plan = Select(ProbePhase.Worker, 33);

        var record = await Run(plan, step => step.Probe == "33" ? FromWorker(step, Usmn(Rms, Maximum)) : TestSupport.Succeeding(step));

        Assert.True(record.Completed);
        var steps = record.Steps.Where(static step => step.Step.Probe == "33").ToList();
        Assert.Equal(3, steps.Count);
        Assert.All(steps, static step => Assert.All(step.Hypotheses, static hypothesis => Assert.True(hypothesis.Matched)));
        var markdown = ProbeRecorder.ToMarkdown(record);
        var json = ProbeRecorder.ToJson(record);
        Assert.Contains($"setter `{ProbeCatalog.ExcludedGroupsLabel}` skipped", markdown, StringComparison.Ordinal);
        Assert.Contains($"`{ProbeCatalog.ExcludedGroupsLabel}` empty list", markdown, StringComparison.Ordinal);
        Assert.Contains("`errors_equal_control` = `true`", markdown, StringComparison.Ordinal);
        Assert.Contains("\"empty-list-argument\"", json, StringComparison.Ordinal);
        foreach (var text in new[] { markdown, json })
        {
            Assert.DoesNotContain("173205", text, StringComparison.Ordinal);
            Assert.DoesNotContain("271828", text, StringComparison.Ordinal);
            Assert.DoesNotContain(TestSupport.Sentinel, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task ARefusedListIsAnAnswerAndTheNextVariantStillRuns()
    {
        var plan = Select(ProbePhase.Worker, 33);

        var record = await Run(plan, step => step.Id switch
        {
            "p33-shipped" => FromWorker(step, Usmn(Rms, Maximum)),
            "p33-omit" => FromWorker(step, new WorkerMpResultAvailable(3, 1, [], "mp-command-failed")),
            "p33-empty" => FromWorker(step, new WorkerArgumentsRejected(1, "sdk-argument-rejected")),
            _ => TestSupport.Succeeding(step)
        });

        Assert.True(record.Completed);
        var omitted = record.Steps.Single(static step => step.Step.Id == "p33-omit");
        var empty = record.Steps.Single(static step => step.Step.Id == "p33-empty");
        Assert.Equal((ProbeOutcomeKind.MpFailed, 3), (omitted.Outcome!.Kind, omitted.Outcome.MpResultCode));
        Assert.Equal(ProbeOutcomeKind.ArgumentRejected, empty.Outcome!.Kind);
        Assert.All(new[] { omitted, empty }, static step =>
        {
            Assert.Equal(StepClassification.Observed, step.Classification);
            Assert.All(step.Hypotheses, static hypothesis => Assert.False(hypothesis.Matched));
        });
    }

    [Fact]
    public async Task OtherErrorsAreRecordedOnlyAsUnequal()
    {
        var plan = Select(ProbePhase.Worker, 33);

        var record = await Run(plan, step => step.Id switch
        {
            "p33-omit" => FromWorker(step, Usmn(Rms * 2, Maximum)),
            _ when step.Probe == "33" => FromWorker(step, Usmn(Rms, Maximum)),
            _ => TestSupport.Succeeding(step)
        });

        Assert.True(record.Completed);
        var omitted = record.Steps.Single(static step => step.Step.Id == "p33-omit");
        Assert.Equal("false", omitted.Outcome!.Observations["errors_equal_control"]);
        Assert.Equal([true, true, false], omitted.Hypotheses.Select(static hypothesis => hypothesis.Matched));
        Assert.Equal("true", record.Steps.Single(static step => step.Step.Id == "p33-empty").Outcome!.Observations["errors_equal_control"]);
    }

    [Fact]
    public async Task AnUnknownOutcomeForTheSkippedSetterEndsTheRunBeforeTheEmptyList()
    {
        var plan = Select(ProbePhase.Worker, 33);
        var transport = new FakeTransport(plan.Phase, step => step.Id switch
        {
            "p33-shipped" => FromWorker(step, Usmn(Rms, Maximum)),
            "p33-omit" => FromWorker(step, new WorkerExecuteRejected(1, "execute-step-rejected")),
            _ => TestSupport.Succeeding(step)
        });

        var record = await ProbeSession.RunAsync(plan, transport, Clock, new SessionIdentity(), new FakeOperator(), CancellationToken.None);

        Assert.Equal(("p33-omit", ProbeSession.UnknownOutcomeStop), (record.StoppedAt, record.StopReason));
        Assert.DoesNotContain("p33-empty", transport.Executed);
        Assert.Equal(StepClassification.NotRun, record.Steps.Single(static step => step.Step.Id == "p33-empty").Classification);
    }

    // Records

    [Fact]
    public void SchemaFourRecordsTheSelectionTheServerSettingAndTheExpectedStall()
    {
        using var selected = JsonDocument.Parse(ProbeRecorder.ToJson(ProbeSession.DryRun(Select(ProbePhase.PublicApi, 26, 30), Clock)));
        using var defaults = JsonDocument.Parse(ProbeRecorder.ToJson(ProbeSession.DryRun(
            ProbePlan.Create(ProbePhase.Worker, TestSupport.SentinelManifest()), Clock)));
        var root = selected.RootElement;
        var stall = root.GetProperty("steps").EnumerateArray().Last();

        Assert.Equal(4, root.GetProperty("schema_version").GetInt32());
        Assert.Equal([26, 30], root.GetProperty("selection").GetProperty("probes").EnumerateArray().Select(static p => p.GetInt32()));
        Assert.Equal("operator", root.GetProperty("selection").GetProperty("selected_by").GetString());
        Assert.Equal("Unchanged", root.GetProperty("server_interaction_mode_setting").GetString());
        Assert.False(root.GetProperty("ended_in_planned_stall").GetBoolean());
        Assert.Equal("grpc:Unavailable", stall.GetProperty("expected_stall").GetProperty("transport").GetString());
        Assert.Equal("WorkerWatchdogTimeout", stall.GetProperty("expected_stall").GetProperty("failure_kind").GetString());
        Assert.Equal(JsonValueKind.Null, stall.GetProperty("server_interaction_mode").ValueKind);
        Assert.Equal(JsonValueKind.Null, defaults.RootElement.GetProperty("selection").ValueKind);
        Assert.Equal(JsonValueKind.Null, defaults.RootElement.GetProperty("server_interaction_mode_setting").ValueKind);
    }

    [Fact]
    public async Task ALicensedStallRunExitsCompletedAndSaysToRecover()
    {
        var root = Path.Combine(Path.GetTempPath(), "briosa-probe-s3-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var profile = Path.Combine(root, "profile.xml");
            await File.WriteAllTextAsync(profile, "<profile />");
            var manifest = Path.Combine(root, "manifest.json");
            await File.WriteAllTextAsync(manifest, JsonSerializer.Serialize(new
            {
                schema_version = 1,
                spatial_analyzer_target = ProbeTarget.SpatialAnalyzerTarget,
                usmn_instruments = new[] { new { collection = "SENTINEL-M", instrument_id = 0 }, new { collection = "SENTINEL-M", instrument_id = 1 } },
                usmn_nominals_group = new { collection = "SENTINEL-M", name = "SENTINEL-NOMINALS" },
                template_chart_name = "SENTINEL-CHART",
                ui_profile_file = profile
            }));
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exit = await LicensedProbeProgram.RunAsync(
                ["--phase", "public-api", ProbeTarget.ConfirmFlag, "--fixtures", manifest, "--output-directory", Path.Combine(root, "run"),
                    "--probe", "26", ProbeOptions.RestoreModeOption, "manual"],
                output, error,
                options => new FakeTransport(options.Phase, step => StallResponder(step, ServerReports(StallExpectation.ModalDialogDiagnosticCode))),
                FakeOperator.Factory, Clock, CancellationToken.None);

            Assert.Equal(0, exit);
            Assert.Contains("completed=True", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("stalled as planned", output.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain(TestSupport.Sentinel, output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
