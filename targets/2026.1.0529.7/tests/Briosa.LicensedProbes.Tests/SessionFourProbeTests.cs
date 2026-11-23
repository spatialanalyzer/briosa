using System.Text.Json;
using Briosa.Worker.Control;
using Google.Protobuf;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.LicensedProbes.Tests;

// Licensed session 4 (#402): result sizes (34), the callout text setters (35), the TCP
// uncertainty notes getter (36), and the robot Machine ID getter (37). All are opt-in, and a
// missing manual fixture is recorded as not run.
public sealed class SessionFourProbeTests
{
    private static readonly FixedClock Clock = new();

    private static FixtureManifest FullManifest() => TestSupport.SentinelManifest() with
    {
        ResultSizePointGroup = new ManifestObject { Collection = "SENTINEL-JOB", Name = "SENTINEL-LARGE-GROUP" },
        ResultSizeCloud = new ManifestObject { Collection = "SENTINEL-JOB", Name = "SENTINEL-LARGE-CLOUD" },
        CalloutView = new ManifestObject { Collection = "SENTINEL-JOB", Name = "SENTINEL-CALLOUT-VIEW" },
        TcpFixture = new ManifestTcpFixture
        {
            Fixture = new ManifestObject { Collection = "SENTINEL-JOB", Name = "SENTINEL-TCP" },
            Measurements = [new ManifestPoint { Collection = "SENTINEL-JOB", Group = "SENTINEL-TCP-POINTS", Target = "SENTINEL-M1" }]
        },
        RobotMachine = new ManifestRobotMachine { Collection = "SENTINEL-JOB", InstrumentId = 3, ParameterName = "SENTINEL-PARAMETER" }
    };

    private static ProbePlan Select(ProbePhase phase, FixtureManifest manifest, params int[] probes) =>
        ProbePlan.Create(phase, manifest, [], InteractionModeSettings.Placeholder, probes);

    private static ProbeStep Step(ProbePlan plan, string id) => plan.CatalogSteps.Single(step => step.Id == id);

    private static Task<ProbeSessionRecord> Run(ProbePlan plan, Func<ProbeStep, ProbeOutcome> respond, FakeTransport? transport = null) =>
        ProbeSession.RunAsync(plan, transport ?? new FakeTransport(plan.Phase, respond), Clock, new SessionIdentity(), new FakeOperator(),
            CancellationToken.None);

    // Catalog and selection

    [Fact]
    public void SessionFourProbesAreOptInAndOutsideEveryDefaultPlan()
    {
        Assert.Equal(["34", "35", "36", "37"], ProbeCatalog.OptInProbeNumbers.Intersect(["34", "35", "36", "37"]));
        Assert.Contains("37", ProbeCatalog.ProbeNumbers);
        foreach (var phase in new[] { ProbePhase.PublicApi, ProbePhase.Worker })
        {
            var plan = ProbePlan.Create(phase, TestSupport.SentinelManifest());
            Assert.DoesNotContain(plan.Steps, static step => step.Probe is "34" or "35" or "36" or "37");
            Assert.Empty(plan.MissingFixtures);
        }

        var exception = Assert.Throws<ProbeUsageException>(() =>
            ProbeOptions.Parse(["--phase", "public-api", "--dry-run", "--exclude-probe", "34"]));
        Assert.Contains("names an opt-in probe", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProbeThirtyFourPlansTheAscendingLadderInBothPhases(bool workerPhase)
    {
        var plan = Select(workerPhase ? ProbePhase.Worker : ProbePhase.PublicApi, FullManifest(), 34);
        var ids = plan.Steps.Where(static step => step.Probe == "34").Select(static step => step.Id).ToList();

        Assert.Equal("p34-bspline", ids[0]);
        Assert.Equal([100, 500, 2000, 10000, 20000], ProbeCatalog.ResultSizeLadder);
        Assert.True(ProbeCatalog.ResultSizeLadder.SequenceEqual(ProbeCatalog.ResultSizeLadder.Order()));
        foreach (var count in ProbeCatalog.ResultSizeLadder)
        {
            var construct = ids.IndexOf($"p34-points-{count}");
            Assert.True(construct > 0);
            Assert.Equal(construct + 1, ids.IndexOf($"c34-count-{count}"));
            Assert.Equal(construct + 2, ids.IndexOf($"p34-list-{count}"));
            var list = Step(plan, $"p34-list-{count}");
            Assert.True(list.MeasuresResponseSize);
            Assert.Equal(ProbeOutcomes.AnyDeterminateSdkOutcome, list.Acceptable);
            Assert.Equal(Observations.Count(count), Assert.Single(list.Hypotheses).ExpectedValue);
        }

        Assert.True(Step(plan, "p34-objects-job").MeasuresResponseSize);
        Assert.Equal(FixtureKeys.ResultSizePointGroup, Step(plan, "p34-list-job-group").RequiredFixture);
        Assert.Equal(FixtureKeys.ResultSizeCloud, Step(plan, "c34-count-job-cloud").RequiredFixture);
        Assert.Equal(["34"], plan.ProbesCovered());
    }

    [Theory]
    [InlineData(35)]
    [InlineData(36)]
    [InlineData(37)]
    public void TheCorrectedBindingProbesRunOnlyThroughTheGeneratedClient(int probe)
    {
        Assert.Contains(Select(ProbePhase.PublicApi, FullManifest(), probe).Steps, step => step.Probe == $"{probe}");
        var exception = Assert.Throws<ProbeUsageException>(() => Select(ProbePhase.Worker, FullManifest(), probe));
        Assert.Contains("no steps in the worker phase", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheCorrectedBindingsAreTheOnesSent()
    {
        var plan = Select(ProbePhase.PublicApi, FullManifest(), 35, 36, 37);
        string Binding(string step, string label) => Step(plan, step).Command.InputArguments.Single(argument => argument.Name == label).SdkBinding!;

        Assert.Equal("SetEditTextArg", Binding("p35-text", "Text"));
        Assert.Equal("SetEditTextArg", Binding("p35-point", "Notes (blank for none)"));
        Assert.Equal("SetEditTextArg", Binding("p35-point-comparison", "Additional Notes (blank for none)"));
        Assert.Equal("SetEditTextArg", Binding("p35-relationship", "Additional Notes (blank for none)"));
        Assert.Equal("SetEditTextArg", Binding("p35-vector", "Additional Notes (blank for none)"));
        Assert.Equal("GetEditTextArg", Step(plan, "p36-notes").Command.OutputArguments.Single(output => output.Name == "Result Notes").SdkBinding);
        Assert.Equal("SetColInstIdArg", Binding("p37-parameter", "Machine ID"));
        Assert.All(plan.Steps.Where(static step => step.Probe is "35" or "36" or "37"),
            static step => Assert.True(step.Checkpoints.HasFlag(OperatorCheckpoint.After)));
        Assert.Equal(["c35-callouts-before"], Step(plan, "p35-vector").Prerequisites);
    }

    [Fact]
    public void ThePublicPlanIsAdmittedButTheExclusiveRobotInterfaceRoutesNeverAre()
    {
        var plan = Select(ProbePhase.PublicApi, FullManifest(), 34, 35, 36, 37);

        Assert.Contains("--Briosa:Security:Operations:Profile=device", plan.ServerAdmissionArguments);
        foreach (var request in new IMessage[]
        {
            new Api.StartRobotMachineInterfaceRequest { MachineId = new Api.CollectionInstrumentId { CollectionName = "J", InstrumentId = 0 } },
            new Api.StopRobotMachineInterfaceRequest { MachineId = new Api.CollectionInstrumentId { CollectionName = "J", InstrumentId = 0 } }
        })
        {
            var method = request is Api.StartRobotMachineInterfaceRequest
                ? "/briosa.RobotOperations/StartRobotMachineInterface"
                : "/briosa.RobotOperations/StopRobotMachineInterface";
            Assert.Throws<InvalidOperationException>(() => ProbePlan.CreateServerAdmissionArguments([(method, request)]));
        }
    }

    // Missing fixtures

    [Fact]
    public async Task AMissingFixtureIsRecordedAsNotRunAndNeverSent()
    {
        var plan = Select(ProbePhase.PublicApi, TestSupport.SentinelManifest(), 34, 35, 36, 37);
        var transport = new FakeTransport(plan.Phase, TestSupport.Succeeding);
        var record = await Run(plan, TestSupport.Succeeding, transport);

        string[] missing = [FixtureKeys.ResultSizePointGroup, FixtureKeys.ResultSizeCloud, FixtureKeys.CalloutView, FixtureKeys.TcpFixture, FixtureKeys.RobotMachine];
        Assert.Equal(missing, plan.MissingFixtures);
        Assert.Equal(missing, record.MissingFixtures);
        var unsent = record.Steps.Where(static step => step.Classification == StepClassification.FixtureMissing).Select(static step => step.Step.Id).ToList();
        Assert.Equal(["c34-count-job-group", "p34-list-job-group", "c34-count-job-cloud", "c35-callouts-before", "p35-text", "p35-point",
            "p35-point-comparison", "p35-relationship", "p35-vector", "c35-callouts-after", "p36-notes", "p37-parameter"], unsent);
        Assert.Empty(transport.Executed.Intersect(unsent));
        Assert.Contains("p34-list-20000", transport.Executed);
        Assert.True(record.Completed);

        using var document = JsonDocument.Parse(ProbeRecorder.ToJson(record));
        Assert.Equal(missing, document.RootElement.GetProperty("fixtures_missing").EnumerateArray().Select(static item => item.GetString()));
        var json = document.RootElement.GetProperty("steps").EnumerateArray().ToDictionary(static step => step.GetProperty("id").GetString()!);
        Assert.Equal("fixture-missing", json["p35-text"].GetProperty("classification").GetString());
        Assert.Equal(FixtureKeys.CalloutView, json["p35-text"].GetProperty("required_fixture").GetString());
        Assert.Equal(JsonValueKind.Null, json["p35-text"].GetProperty("outcome").ValueKind);
        var markdown = ProbeRecorder.ToMarkdown(record);
        Assert.Contains("Not run: the manifest has no `callout_view`; not sent and not a pass.", markdown, StringComparison.Ordinal);
        Assert.Contains("Fixtures missing from the manifest: result_size_point_group", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void TheDryRunNamesEveryMissingFixtureAndNoManifestValue()
    {
        var missing = Select(ProbePhase.PublicApi, TestSupport.SentinelManifest(), 35).RenderDryRun();
        var supplied = Select(ProbePhase.PublicApi, FullManifest(), 34, 35, 36, 37).RenderDryRun();

        Assert.Contains("FIXTURE MISSING: the manifest has no callout_view; never sent, recorded as not run.", missing, StringComparison.Ordinal);
        Assert.Contains("Fixtures missing from the manifest: callout_view", missing, StringComparison.Ordinal);
        Assert.DoesNotContain("FIXTURE MISSING", supplied, StringComparison.Ordinal);
        Assert.Contains("manual fixture: tcp_fixture", supplied, StringComparison.Ordinal);
        Assert.Contains("RESULT SIZE: records the element count", supplied, StringComparison.Ordinal);
        Assert.Contains("worker response budget is", supplied, StringComparison.Ordinal);
        Assert.DoesNotContain(TestSupport.Sentinel, supplied, StringComparison.Ordinal);
        Assert.DoesNotContain(TestSupport.Sentinel, missing, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SuppliedFixturesRunAndNoManifestValueReachesARecord()
    {
        var plan = Select(ProbePhase.PublicApi, FullManifest(), 34, 35, 36, 37);
        var record = await Run(plan, TestSupport.Succeeding);

        Assert.Empty(record.MissingFixtures);
        Assert.True(record.Completed);
        Assert.All(record.Steps.Where(static step => step.Step.Probe is "35" or "36" or "37"),
            static step => Assert.Equal(StepClassification.Observed, step.Classification));
        Assert.DoesNotContain(TestSupport.Sentinel, ProbeRecorder.ToJson(record), StringComparison.Ordinal);
        Assert.DoesNotContain(TestSupport.Sentinel, ProbeRecorder.ToMarkdown(record), StringComparison.Ordinal);
    }

    [Fact]
    public void TheManifestAcceptsTheSessionFourKeysAndRefusesInvalidOnes()
    {
        const string valid = """
            {
              "schema_version": 1,
              "spatial_analyzer_target": "TARGET",
              "usmn_instruments": [ { "collection": "M", "instrument_id": 0 }, { "collection": "M", "instrument_id": 1 } ],
              "usmn_nominals_group": { "collection": "M", "name": "Nominals" },
              "template_chart_name": "T",
              "ui_profile_file": "C:\\p\\profile.xml",
              "result_size_point_group": { "collection": "M", "name": "Big" },
              "result_size_cloud": { "collection": "M", "name": "Cloud" },
              "callout_view": { "collection": "M", "name": "View" },
              "tcp_fixture": {
                "fixture": { "collection": "M", "name": "Tcp" },
                "measurements": [ { "collection": "M", "group": "G", "target": "T1" } ],
                "tcp_in_working": [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1]
              },
              "robot_machine": { "collection": "M", "instrument_id": 2, "parameter_name": "P" }
            }
            """;
        var json = valid.Replace("TARGET", ProbeTarget.SpatialAnalyzerTarget, StringComparison.Ordinal);
        var manifest = FixtureManifest.Parse(json);

        Assert.True(manifest.Supplies(FixtureKeys.TcpFixture));
        Assert.Equal(16, manifest.TcpFixture!.TcpInWorking!.Count);
        Assert.Throws<InvalidDataException>(() => FixtureManifest.Parse(json.Replace("0, 0, 0, 1]", "0, 0, 1]", StringComparison.Ordinal)));
        Assert.Throws<InvalidDataException>(() => FixtureManifest.Parse(json.Replace("\"instrument_id\": 2", "\"instrument_id\": -1", StringComparison.Ordinal)));
        Assert.Throws<InvalidDataException>(() => FixtureManifest.Parse(json.Replace("\"name\": \"View\"", "\"name\": \" \"", StringComparison.Ordinal)));
        Assert.Throws<InvalidDataException>(() => FixtureManifest.Parse(json.Replace("{ \"collection\": \"M\", \"name\": \"Big\" }",
            $"{{ \"collection\": \"{FixtureNames.Collection}\", \"name\": \"Big\" }}", StringComparison.Ordinal)));
        Assert.Throws<InvalidDataException>(() => FixtureManifest.Parse(json.Replace("\"measurements\": [ { \"collection\": \"M\", \"group\": \"G\", \"target\": \"T1\" } ]",
            "\"measurements\": []", StringComparison.Ordinal)));
    }

    // Response sizes

    [Fact]
    public void TheWorkerResponseBudgetIsTheBuildsOwn()
    {
        // 1 MiB since #411, separate from the 64 KiB request budget.
        Assert.Equal(1024 * 1024, WorkerResponseBudget.Bytes);
        Assert.Equal(WorkerControlProtocol.MaximumResponseBytes, WorkerResponseBudget.Bytes);
    }

    [Fact]
    public async Task TheWorkerFrameSizeIsTheExactPayloadOfTheFrameRead()
    {
        var response = Completed(new WorkerMpResultAvailable(2, 5, [PointNames(300)], null));
        var (frame, bytesOnWire, received) = await RoundTripAsync(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), response));
        var size = ResponseSize.FromWorkerResponse(frame, received.ExecutionResponse!);

        Assert.Equal(bytesOnWire - ResponseSize.FrameHeaderBytes, frame);
        Assert.Equal(frame, size.ReceivedFrameBytes);
        Assert.False(size.EncodingRejected);
        Assert.Null(size.PublicMessageBytes);
        var outcome = WorkerOutcomes.FromResponse(received.ExecutionResponse!, outputs =>
            ProbeOperations.MakePointNameRefListFromGroup.ObserveWorker(new Api.MakePointNameRefListFromGroupRequest(), outputs),
            "construction_operations.make_point_name_ref_list_from_group");
        Assert.Equal(ProbeOutcomeKind.Succeeded, outcome.Kind);
        Assert.Equal("300", outcome.Observations["element_count"]);
    }

    [Fact]
    public async Task TheLadderSizeEstimateHoldsForThisBuildsEncoders()
    {
        // The runbook sizes the ladder at about 70 bytes of private frame per point name and the
        // protobuf message at roughly a third of the frame. 500 names fit either budget.
        const int Count = 500;
        var names = Enumerable.Range(10001, Count).Select(static index => $"P{index}").ToList();
        var frameValue = new WorkerPointNameListValue([.. names.Select(static name => new WorkerPointNameValue(FixtureNames.Collection, "SZ10000", name))]);
        var (frame, _, _) = await RoundTripAsync(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), Completed(
            new WorkerMpResultAvailable(2, 5, [new WorkerRetrievedOutput("Resultant Point Name List", WorkerMpValueKind.PointNameList, frameValue)], null))));
        var message = new Api.MakePointNameRefListFromGroupResult
        {
            Execution = new Api.MpExecutionDetails { State = Api.MpExecutionState.Succeeded, MpResultCode = 2 }
        };
        message.ResultantPointNameList.AddRange(names.Select(static name =>
            new Api.PointName { CollectionName = FixtureNames.Collection, GroupName = "SZ10000", TargetName = name }));

        Assert.InRange(frame / (double)Count, 55, 85);
        Assert.InRange(message.CalculateSize() / (double)frame, 0.25, 0.45);
    }

    [Fact]
    public async Task AResultOverTheBudgetArrivesAsTheBoundedReplacementAndIsFlagged()
    {
        // Enough names that the frame exceeds this build's budget, whichever it is.
        var response = Completed(new WorkerMpResultAvailable(2, 5, [PointNames((WorkerResponseBudget.Bytes / 40) + 100)], null));
        var message = WorkerControlMessage.ExecutionResult(Guid.NewGuid(), response);
        using (var rejected = new MemoryStream())
        using (var channel = new WorkerControlChannel(rejected, leaveOpen: true))
        {
            await Assert.ThrowsAsync<WorkerMessageRejectedException>(async () => await channel.SendAsync(message));
            Assert.Equal(0, rejected.Length);
        }

        var replacement = WorkerExecutionDelivery.Undeliverable(message.CorrelationId, response);
        var (frame, _, received) = await RoundTripAsync(replacement);
        var size = ResponseSize.FromWorkerResponse(frame, received.ExecutionResponse!);
        var outcome = WorkerOutcomes.FromResponse(received.ExecutionResponse!, static _ => ProbeOutcome.EmptyObservations,
            "construction_operations.make_point_name_ref_list_from_group");

        Assert.True(size.EncodingRejected);
        Assert.True(frame < WorkerResponseBudget.Bytes);
        Assert.Equal(ProbeOutcomeKind.OutputRetrievalFailed, outcome.Kind);
        Assert.True(ProbeOutcomes.AnyDeterminateSdkOutcome.Accepts(outcome));
    }

    [Fact]
    public async Task ANonFiniteValueFarBelowTheBudgetIsAnEncodingRejectionNotASize()
    {
        // One NaN: the worker rejects the encoding although the result is tiny. The record must
        // say "rejected, size unknown", never that the budget was exceeded.
        var response = Completed(new WorkerMpResultAvailable(2, 5,
            [new WorkerRetrievedOutput("Total Count", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(double.NaN))], null));
        var message = WorkerControlMessage.ExecutionResult(Guid.NewGuid(), response);
        using (var rejected = new MemoryStream())
        using (var channel = new WorkerControlChannel(rejected, leaveOpen: true))
        {
            await Assert.ThrowsAsync<WorkerMessageRejectedException>(async () => await channel.SendAsync(message));
        }

        var (frame, _, received) = await RoundTripAsync(WorkerExecutionDelivery.Undeliverable(message.CorrelationId, response));
        var size = ResponseSize.FromWorkerResponse(frame, received.ExecutionResponse!);
        var plan = Select(ProbePhase.Worker, FullManifest(), 34);
        var record = await Run(plan, step => step.Id == "p34-list-100"
            ? TestSupport.Outcome(ProbeOutcomeKind.OutputRetrievalFailed) with { Size = size }
            : TestSupport.Succeeding(step));
        using var document = JsonDocument.Parse(ProbeRecorder.ToJson(record));
        var json = document.RootElement.GetProperty("steps").EnumerateArray().Single(static step => step.GetProperty("id").GetString() == "p34-list-100")
            .GetProperty("outcome").GetProperty("response_size");
        var markdown = ProbeRecorder.ToMarkdown(record);

        Assert.True(size.EncodingRejected);
        Assert.Equal(frame, size.ReceivedFrameBytes);
        Assert.Equal("unknown", json.GetProperty("original_size").GetString());
        Assert.True(json.GetProperty("encoding_rejected").GetBoolean());
        Assert.False(json.TryGetProperty("budget_exceeded", out _));
        Assert.Contains("rejected; original size unknown", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("exceeded", markdown, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnOutputWithheldFromAnSdkFaultCountsAsAnEncodingRejection()
    {
        var fault = new WorkerSdkCallFaulted(WorkerSdkCallPhase.OutputGetter, 5,
            [new WorkerUnavailableOutput("Resultant Point Name List", WorkerMpValueKind.PointNameList, WorkerExecutionDelivery.OutputEncodingRejected,
                WorkerUnavailableOutputReason.EncodingRejected)],
            "sdk-output-getter-faulted", 2);

        Assert.True(ResponseSize.FromWorkerResponse(200, Completed(fault)).EncodingRejected);
        Assert.False(ResponseSize.FromWorkerResponse(200, Completed(new WorkerMpResultAvailable(2, 5, [PointNames(1)], null))).EncodingRejected);
    }

    [Fact]
    public void TheCountingStreamCountsReadsOnlyAndNeverClosesItsInner()
    {
        using var inner = new MemoryStream([1, 2, 3, 4, 5, 6]);
        using (var counter = new CountingStream(inner))
        {
            var buffer = new byte[4];
            Assert.Equal(4, counter.Read(buffer, 0, 4));
            counter.Write([9], 0, 1);
            Assert.Equal(4, counter.BytesRead);
            Assert.Equal(1, counter.Read(buffer.AsSpan(0, 2)));
            Assert.Equal(5, counter.BytesRead);
        }

        Assert.True(inner.CanRead);
        Assert.Equal(96, WorkerTransport.FrameBytes(1000, 1100));
    }

    [Fact]
    public async Task ThePublicSizeIsTheReceivedProtobufMessage()
    {
        var result = new Api.MakePointNameRefListFromGroupResult
        {
            Execution = new Api.MpExecutionDetails { State = Api.MpExecutionState.Succeeded, MpResultCode = 2 }
        };
        result.ResultantPointNameList.AddRange(Enumerable.Range(1, 250).Select(static index =>
            new Api.PointName { CollectionName = FixtureNames.Collection, GroupName = "SZ250", TargetName = $"P{index}" }));
        var outcome = await ProbeOperations.MakePointNameRefListFromGroup.InvokePublicAsync(
            new ReplyingInvoker(result), new Api.MakePointNameRefListFromGroupRequest(), new CallOptions());

        Assert.Equal(ProbeOutcomeKind.Succeeded, outcome.Kind);
        Assert.Equal("250", outcome.Observations["element_count"]);
        Assert.Equal(result.CalculateSize(), outcome.Size!.PublicMessageBytes);
        Assert.False(outcome.Size.ExceedsPublishedClientLimit);
        Assert.Null(outcome.Size.ReceivedFrameBytes);
    }

    [Fact]
    public async Task APublicResultOverTheBudgetIsTheCompletedDataLossOutcome()
    {
        var error = new Api.OperationError
        {
            Kind = Api.OperationFailureKind.OutputRetrievalFailure,
            DiagnosticCode = ResponseSize.EncodingRejectedDiagnosticCode,
            ExecutionDisposition = Api.ExecutionDisposition.Completed,
            MpExecution = new Api.MpExecutionDetails { State = Api.MpExecutionState.Succeeded, MpResultCode = 2 }
        };
        var other = error.Clone();
        other.DiagnosticCode = "sdk-output-getter-faulted";
        var trailers = new Metadata { { PublicOutcomes.ErrorTrailerName, error.ToByteArray() } };
        var outcome = await ProbeOperations.MakePointNameRefListFromGroup.InvokePublicAsync(
            new ReplyingInvoker(new RpcException(new Status(StatusCode.DataLoss, "x"), trailers)), new Api.MakePointNameRefListFromGroupRequest(), new CallOptions());
        var unrelated = PublicOutcomes.FromRpcException(new RpcException(new Status(StatusCode.DataLoss, "x"),
            new Metadata { { PublicOutcomes.ErrorTrailerName, other.ToByteArray() } }));

        Assert.Equal(ProbeOutcomeKind.OutputRetrievalFailed, outcome.Kind);
        Assert.Equal("grpc:DataLoss", outcome.Transport);
        Assert.True(outcome.Size!.EncodingRejected);
        Assert.Null(outcome.Size.PublicMessageBytes);
        Assert.Null(unrelated.Size);
    }

    [Fact]
    public void AGetterFaultWithOneOutputRejectedForItsEncodingIsSeenPerOutput()
    {
        // On main, a getter fault can deliver every output that fits and mark only the rest.
        var error = new Api.OperationError
        {
            Kind = Api.OperationFailureKind.SdkCallFaulted,
            DiagnosticCode = "sdk-output-getter-faulted",
            ExecutionDisposition = Api.ExecutionDisposition.Completed,
            MpExecution = new Api.MpExecutionDetails { State = Api.MpExecutionState.Succeeded, MpResultCode = 2 }
        };
        error.MpExecution.OutputRetrievals.Add(new Api.OutputRetrievalDetails
        {
            FieldName = "resultant_point_name_list",
            State = Api.OutputRetrievalState.Failed,
            DiagnosticCode = ResponseSize.EncodingRejectedDiagnosticCode
        });
        var outcome = PublicOutcomes.FromRpcException(new RpcException(new Status(StatusCode.DataLoss, "x"),
            new Metadata { { PublicOutcomes.ErrorTrailerName, error.ToByteArray() } }));

        Assert.True(outcome.Size!.EncodingRejected);
        Assert.Equal(ProbeOutcomeKind.OutputRetrievalFailed, outcome.Kind);
        Assert.Equal("sdk-output-getter-faulted", outcome.DiagnosticCode);
    }

    [Fact]
    public void ExternalCodesAndNamesReachARecordOnlyWhenListedOrInShape()
    {
        var error = new Api.OperationError
        {
            Kind = Api.OperationFailureKind.MpFailure,
            DiagnosticCode = "desktop-sentinel-01",
            ExecutionDisposition = Api.ExecutionDisposition.Completed,
            MpExecution = new Api.MpExecutionDetails { State = Api.MpExecutionState.Failed, MpResultCode = 3 }
        };
        error.MpExecution.OutputRetrievals.Add(new Api.OutputRetrievalDetails { FieldName = "C:\\SENTINEL\\file", State = Api.OutputRetrievalState.NotAttempted });
        var publicOutcome = PublicOutcomes.FromRpcException(new RpcException(new Status(StatusCode.FailedPrecondition, "x"),
            new Metadata { { PublicOutcomes.ErrorTrailerName, error.ToByteArray() } }));
        var workerOutcome = WorkerOutcomes.FromResponse(Completed(new WorkerMpResultAvailable(3, 5, [], "sentinel-account-name")),
            static _ => ProbeOutcome.EmptyObservations, "construction_operations.make_point_name_ref_list_from_group");
        var named = WorkerOutcomes.FromResponse(Completed(new WorkerMpResultAvailable(2, 5, [PointNames(1)], null)),
            static _ => ProbeOutcome.EmptyObservations, "construction_operations.make_point_name_ref_list_from_group", ["Some Other Output"]);

        Assert.Equal(ExternalValues.Withheld, publicOutcome.DiagnosticCode);
        Assert.Equal(ExternalValues.Withheld, Assert.Single(publicOutcome.Outputs).Name);
        Assert.Equal(ExternalValues.Withheld, workerOutcome.DiagnosticCode);
        Assert.Equal(ExternalValues.Withheld, Assert.Single(named.Outputs).Name);
        Assert.Equal("Resultant Point Name List", WorkerOutcomes.OutputName("Resultant Point Name List", null));
        Assert.Equal(ExternalValues.Withheld, WorkerOutcomes.OutputName("C:\\SENTINEL", null));
        Assert.Contains("worker-output-encoding-rejected", ExternalValues.DiagnosticCodes);
        Assert.Contains("sa-modal-dialog-open", ExternalValues.DiagnosticCodes);
        Assert.Equal((ExternalValues.Withheld, ExternalValues.Withheld), (ExternalValues.Version("SENTINEL"), ExternalValues.Revision("abc")));
    }

    [Fact]
    public async Task APublicOutputNameIsKeptOnlyWhenTheOperationsResultDeclaresIt()
    {
        // A well-formed name the result does not declare is withheld, however harmless it looks.
        var result = new Api.GetNumberOfCollectionsResult
        {
            TotalCount = 3,
            Execution = new Api.MpExecutionDetails { State = Api.MpExecutionState.Succeeded, MpResultCode = 2 }
        };
        result.Execution.OutputRetrievals.Add(new Api.OutputRetrievalDetails { FieldName = "alice_smith", State = Api.OutputRetrievalState.Retrieved });
        result.Execution.OutputRetrievals.Add(new Api.OutputRetrievalDetails { FieldName = "total_count", State = Api.OutputRetrievalState.Retrieved });

        var outcome = await ProbeOperations.GetNumberOfCollections.InvokePublicAsync(new ReplyingInvoker(result),
            new Api.GetNumberOfCollectionsRequest(), new CallOptions());
        var error = new Api.OperationError
        {
            Kind = Api.OperationFailureKind.MpFailure,
            ExecutionDisposition = Api.ExecutionDisposition.Completed,
            MpExecution = new Api.MpExecutionDetails { State = Api.MpExecutionState.Failed, MpResultCode = 3 }
        };
        error.MpExecution.OutputRetrievals.Add(new Api.OutputRetrievalDetails { FieldName = "alice_smith", State = Api.OutputRetrievalState.NotAttempted });
        var failed = await ProbeOperations.GetNumberOfCollections.InvokePublicAsync(
            new ReplyingInvoker(new RpcException(new Status(StatusCode.FailedPrecondition, "x"), new Metadata { { PublicOutcomes.ErrorTrailerName, error.ToByteArray() } })),
            new Api.GetNumberOfCollectionsRequest(), new CallOptions());

        Assert.Equal([ExternalValues.Withheld, "total_count"], outcome.Outputs.Select(static output => output.Name));
        Assert.Equal(ExternalValues.Withheld, Assert.Single(failed.Outputs).Name);
        Assert.DoesNotContain("alice_smith", JsonSerializer.Serialize(outcome.Outputs), StringComparison.Ordinal);

        // Nested result paths are declared too; without a declaration every name is withheld.
        var nested = DeclaredFields.Of(Api.AlignCloudToCadResult.Descriptor);
        Assert.Contains("alignment.rms_deviation", (IReadOnlySet<string>)nested);
        Assert.Equal("alignment.rms_deviation", ExternalValues.FieldName("alignment.rms_deviation"));
        Assert.Equal(ExternalValues.Withheld,
            Assert.Single(PublicOutcomes.FromSuccess(result.Execution, ProbeOutcome.EmptyObservations).Outputs.Take(1)).Name);
    }

    [Fact]
    public async Task ALicensePromptSignalledDuringAStepStopsTheSessionAndTheTransportSendsNothingMore()
    {
        // Probe 34 has no checkpoints: the operator's signal is the only way to stop it.
        var plan = Select(ProbePhase.Worker, FullManifest(), 34);
        var console = new FakeOperator();
        var transport = new FakeTransport(plan.Phase, step =>
        {
            if (step.Id == "p34-list-500")
            {
                console.LicenseAbortRaised = true;
            }

            return TestSupport.Succeeding(step);
        });

        var record = await ProbeSession.RunAsync(plan, transport, Clock, new SessionIdentity(), console, CancellationToken.None);

        Assert.Equal((ProbeSession.LicensePromptStop, "p34-list-500"), (record.StopReason, record.StoppedAt));
        Assert.Equal("p34-list-500", transport.Executed[^1]);
        Assert.True(transport.Abandoned);
        Assert.Empty(console.Checkpoints);
        Assert.DoesNotContain(record.Steps.SkipWhile(static step => step.Step.Id != "p34-list-500").Skip(1),
            static step => step.Outcome is { } outcome && outcome.Kind != ProbeOutcomeKind.RefusedByHarness);

        // Raised before the run: the transport is never even started.
        var early = new FakeOperator { LicenseAbortRaised = true };
        var untouched = new FakeTransport(plan.Phase, TestSupport.Succeeding);
        var refused = await ProbeSession.RunAsync(plan, untouched, Clock, new SessionIdentity(), early, CancellationToken.None);
        Assert.False(untouched.Started);
        Assert.Empty(untouched.Executed);
        Assert.True(untouched.Abandoned);
        Assert.Equal(ProbeSession.LicensePromptStop, refused.StopReason);

        // The transport is given the session's one signal.
        Assert.Same(console.LicenseAbort, transport.LicenseAbort);
    }

    [Fact]
    public void ALicensePromptTakesPrecedenceOverTheStopItInterruptedAndIsLatched()
    {
        var plan = Select(ProbePhase.Worker, FullManifest(), 34);
        var record = new ProbeSessionRecord(plan.Phase, false, false, Clock.GetUtcNow(), Clock.GetUtcNow(), SessionIdentity.None, [],
            "p34-list-100", ProbeSession.UnknownOutcomeStop);
        var transport = new FakeTransport(plan.Phase, TestSupport.Succeeding);
        var signalFile = false;
        var signal = new LicenseAbortSignal(() => signalFile);

        Assert.Same(record, ProbeSession.WithLicensePrecedence(record, signal, transport));
        Assert.False(transport.Abandoned);

        signalFile = true;
        var superseded = ProbeSession.WithLicensePrecedence(record, signal, transport);
        signalFile = false;

        Assert.Equal((ProbeSession.LicensePromptStop, ProbeSession.UnknownOutcomeStop), (superseded.StopReason, superseded.SupersededStopReason));
        Assert.True(transport.Abandoned);
        Assert.True(signal.Raised);
        using var json = JsonDocument.Parse(ProbeRecorder.ToJson(superseded));
        Assert.Equal(ProbeSession.UnknownOutcomeStop, json.RootElement.GetProperty("superseded_stop_reason").GetString());
        Assert.Contains("takes precedence", ProbeRecorder.ToMarkdown(superseded), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ARaisedSignalStopsTheWorkerTransportBeforeItLaunchesTheWorker()
    {
        // An empty file named like the worker: if the transport ever tried to launch it, the
        // start would fail with a different error. Nothing can start.
        var root = Directory.CreateTempSubdirectory("briosa-license-");
        try
        {
            var worker = Path.Combine(root.FullName, "Briosa.Worker.exe");
            await File.WriteAllBytesAsync(worker, []);
            var transport = new WorkerTransport(worker, TimeSpan.FromSeconds(5), new OneSpatialAnalyzer());
            var signal = new LicenseAbortSignal(static () => true);
            var plan = Select(ProbePhase.Worker, FullManifest(), 34);

            var refusal = await Assert.ThrowsAsync<ProbeRefusedException>(() => transport.StartAsync(plan, signal, CancellationToken.None));

            Assert.Equal(ProbeSession.LicensePromptStop, refusal.DiagnosticCode);
            Assert.False(transport.WouldSendStopOnDispose);
            Assert.Equal(ProbeOutcomeKind.RefusedByHarness, (await transport.ExecuteAsync(plan.Steps[0], CancellationToken.None)).Kind);
            await transport.DisposeAsync();
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    private sealed class OneSpatialAnalyzer : IProcessCensus
    {
        public int Count(string processName) => processName == ProcessCensus.SpatialAnalyzer ? 1 : 0;
    }

    [Theory]
    [InlineData("0.10.1", "0.10.1")]
    [InlineData("0.10.1-probe-s4", "0.10.1-probe-s4")]
    [InlineData("0.10.1-alice.smith", ExternalValues.Withheld)]
    [InlineData("0.10.1-probe-s4.alice", ExternalValues.Withheld)]
    [InlineData("0.10.1-rc.1", ExternalValues.Withheld)]
    [InlineData("0.10.1-probe-s4\n", ExternalValues.Withheld)]
    [InlineData("0.10.1\n", ExternalValues.Withheld)]
    public void AServerVersionKeepsOnlyTheReleaseOrProbeBuildForm(string version, string expected) =>
        Assert.Equal(expected, ExternalValues.Version(version));

    // Reruns (F1, D12): only wholly unstarted probes, none after a shared step's unknown
    // completion, and only from an unedited record of a stopped first run by the same build.

    private const string RevisionA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string RevisionB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    private static async Task<string> StoppedRecordAsync(ProbePlan plan, string unknownAt, bool rerun = false)
    {
        var record = await ProbeSession.RunAsync(plan, new FakeTransport(plan.Phase, step => step.Id == unknownAt
                ? ProbeOutcome.Unknown("grpc:DeadlineExceeded", "untyped-rpc-failure")
                : TestSupport.Succeeding(step)), Clock, new SessionIdentity { HarnessRevision = RevisionA }, new FakeOperator(),
            CancellationToken.None);
        Assert.NotNull(record.StopReason);
        return ProbeRecorder.ToJson(record with { Rerun = rerun });
    }

    // Applies one edit to a genuine record, as the review did by hand.
    private static string Edited(string json, Action<System.Text.Json.Nodes.JsonObject> edit)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
        edit(node);
        return node.ToJsonString();
    }

    private static System.Text.Json.Nodes.JsonObject StepNode(System.Text.Json.Nodes.JsonObject record, string id) =>
        record["steps"]!.AsArray().Select(static step => step!.AsObject()).Single(step => (string?)step["id"] == id);

    [Fact]
    public async Task ASharedSetupStepWithUnknownCompletionAllowsNoRerun()
    {
        var plan = Select(ProbePhase.PublicApi, FullManifest(), 34, 35, 36, 37);
        var json = await StoppedRecordAsync(plan, "s-collection");

        var exception = Assert.Throws<ProbeUsageException>(() => RerunGate.Check(json, ProbePhase.PublicApi, [35], RevisionA));

        Assert.Equal(RerunGate.SharedStepUnknown, exception.Message);
    }

    [Fact]
    public async Task ARerunNamesOnlyProbesNoneOfWhoseStepsWereSent()
    {
        var plan = Select(ProbePhase.PublicApi, FullManifest(), 34, 35, 36, 37);
        var json = await StoppedRecordAsync(plan, "p34-list-100");

        Assert.Equal(["35", "36", "37"], RerunGate.WhollyUnstarted(json, ProbePhase.PublicApi, RevisionA).Order(StringComparer.Ordinal));
        RerunGate.Check(json, ProbePhase.PublicApi, [35, 36, 37], RevisionA);
        Assert.Equal(RerunGate.StartedSelected, Assert.Throws<ProbeUsageException>(() => RerunGate.Check(json, ProbePhase.PublicApi, [34, 35], RevisionA)).Message);
        Assert.Equal(RerunGate.SelectionRequired, Assert.Throws<ProbeUsageException>(() => RerunGate.Check(json, ProbePhase.PublicApi, [], RevisionA)).Message);
        Assert.Equal(RerunGate.WrongRecord, Assert.Throws<ProbeUsageException>(() => RerunGate.Check(json, ProbePhase.Worker, [35], RevisionA)).Message);
        Assert.Equal(RerunGate.Unreadable, Assert.Throws<ProbeUsageException>(() => RerunGate.Check("not json", ProbePhase.PublicApi, [35], RevisionA)).Message);
    }

    [Fact]
    public async Task WhenEveryProbeHadStartedTheRunEnds()
    {
        // The run stops in its last probe: every probe had started.
        var plan = Select(ProbePhase.PublicApi, FullManifest(), 34, 37);
        var json = await StoppedRecordAsync(plan, "p37-parameter");

        Assert.Equal(RerunGate.NoneRemain, Assert.Throws<ProbeUsageException>(() => RerunGate.Check(json, ProbePhase.PublicApi, [37], RevisionA)).Message);
    }

    [Fact]
    public async Task TheGateRefusesARecordWhoseUnknownStepLostItsOutcome()
    {
        // The review's case: a genuine record stopped at s-collection, with that step's outcome
        // removed. Without the outcome the gate cannot see the unknown completion, so it refuses.
        var plan = Select(ProbePhase.PublicApi, FullManifest(), 34, 35, 36, 37);
        var json = Edited(await StoppedRecordAsync(plan, "s-collection"), static record => StepNode(record, "s-collection")["outcome"] = null);

        Assert.Equal(RerunGate.WrongRecord, Assert.Throws<ProbeUsageException>(() => RerunGate.Check(json, ProbePhase.PublicApi, [34], RevisionA)).Message);
    }

    [Fact]
    public async Task TheGateRefusesARecordWithoutCompletedOrWithAnInventedStep()
    {
        var plan = Select(ProbePhase.PublicApi, FullManifest(), 34, 35, 36, 37);
        var genuine = await StoppedRecordAsync(plan, "p34-list-100");
        var cases = new[]
        {
            Edited(genuine, static record => record.Remove("completed")),
            Edited(genuine, static record => { StepNode(record, "p35-text")["kind"] = "invented"; }),
            Edited(genuine, static record => { StepNode(record, "p35-text")["classification"] = "invented"; }),
            Edited(genuine, static record => record["steps"]!.AsArray().RemoveAt(0)),
            Edited(genuine, static record => record["steps"]!.AsArray().Add(StepNode(record, "p35-text").DeepClone())),
            Edited(genuine, static record => { StepNode(record, "p34-list-100")["outcome"] = null; }),
            Edited(genuine, static record => { StepNode(record, "p36-notes")["classification"] = "observed"; }),
            Edited(genuine, static record => record["stop_reason"] = null),
            Edited(genuine, static record => record.Remove("rerun"))
        };

        Assert.All(cases, json => Assert.Equal(RerunGate.WrongRecord,
            Assert.Throws<ProbeUsageException>(() => RerunGate.Check(json, ProbePhase.PublicApi, [35], RevisionA)).Message));
    }

    [Fact]
    public async Task TheGateNeedsTheSameHarnessRevisionAndAFirstRun()
    {
        var plan = Select(ProbePhase.PublicApi, FullManifest(), 34, 35, 36, 37);
        var json = await StoppedRecordAsync(plan, "p34-list-100");
        var rerun = await StoppedRecordAsync(plan, "p34-list-100", rerun: true);

        Assert.Equal(RerunGate.RevisionMismatch, Assert.Throws<ProbeUsageException>(() => RerunGate.Check(json, ProbePhase.PublicApi, [35], RevisionB)).Message);
        Assert.Equal(RerunGate.RevisionMismatch, Assert.Throws<ProbeUsageException>(() => RerunGate.Check(json, ProbePhase.PublicApi, [35], null)).Message);
        Assert.Equal(RerunGate.AlreadyRerun, Assert.Throws<ProbeUsageException>(() => RerunGate.Check(rerun, ProbePhase.PublicApi, [35], RevisionA)).Message);
        Assert.All(new[] { RerunGate.WrongRecord, RerunGate.RevisionMismatch, RerunGate.AlreadyRerun },
            static message => Assert.DoesNotContain(RevisionA, message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheProgramEnforcesTheRerunRuleBeforeAnyPlanRuns()
    {
        var root = Directory.CreateTempSubdirectory("briosa-rerun-");
        try
        {
            var plan = Select(ProbePhase.PublicApi, FullManifest(), 34, 35, 36, 37);
            var previous = Path.Combine(root.FullName, "previous.json");
            await File.WriteAllTextAsync(previous, await StoppedRecordAsync(plan, "p34-list-100"));
            var error = new StringWriter();

            var code = await LicensedProbeProgram.RunAsync(["--phase", "public-api", "--dry-run", "--probe", "34", RerunGate.Option, previous,
                    "--harness-revision", RevisionA],
                new StringWriter(), error, static _ => throw new InvalidOperationException("no transport"), FakeOperator.Factory, Clock, CancellationToken.None);
            var otherBuild = new StringWriter();
            var revision = await LicensedProbeProgram.RunAsync(["--phase", "public-api", "--dry-run", "--probe", "35", RerunGate.Option, previous,
                    "--harness-revision", RevisionB],
                new StringWriter(), otherBuild, static _ => throw new InvalidOperationException("no transport"), FakeOperator.Factory, Clock, CancellationToken.None);
            var allowed = await LicensedProbeProgram.RunAsync(["--phase", "public-api", "--dry-run", "--probe", "35", RerunGate.Option, previous,
                    "--harness-revision", RevisionA],
                new StringWriter(), new StringWriter(), static _ => throw new InvalidOperationException("no transport"), FakeOperator.Factory, Clock,
                CancellationToken.None);

            Assert.Equal((2, 2, 0), (code, revision, allowed));
            Assert.Contains(RerunGate.StartedSelected, error.ToString(), StringComparison.Ordinal);
            Assert.Contains(RerunGate.RevisionMismatch, otherBuild.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain(root.FullName, error.ToString() + otherBuild, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task ALicensePromptDuringAnUnknownSharedStepSupersedesItsStopReason()
    {
        // The review's case: the signal is raised inside s-collection, which returns unknown.
        var plan = Select(ProbePhase.PublicApi, FullManifest(), 34);
        var console = new FakeOperator();
        var record = await ProbeSession.RunAsync(plan, new FakeTransport(plan.Phase, step =>
        {
            if (step.Id != "s-collection")
            {
                return TestSupport.Succeeding(step);
            }

            console.LicenseAbortRaised = true;
            return ProbeOutcome.Unknown("grpc:DeadlineExceeded", "untyped-rpc-failure");
        }), Clock, new SessionIdentity(), console, CancellationToken.None);

        Assert.Equal((ProbeSession.LicensePromptStop, ProbeSession.UnknownOutcomeStop, "s-collection"),
            (record.StopReason, record.SupersededStopReason, record.StoppedAt));
    }

    [Fact]
    public async Task TheFrameGateRefusesBeforeTheFirstByteAndNeverLeavesAPartialFrame()
    {
        // The product channel serializes the whole frame, then writes it through the stream it is
        // given. The gate runs before the first write of each frame: a refused frame writes nothing.
        using var pipe = new MemoryStream();
        var refuse = false;
        await using var counter = new CountingStream(pipe, () =>
        {
            if (refuse)
            {
                throw new InvalidOperationException("refused");
            }
        });
        using var channel = new WorkerControlChannel(counter, leaveOpen: true);

        await channel.SendAsync(WorkerControlMessage.Stop(Guid.NewGuid()));
        var written = pipe.Length;
        refuse = true;
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await channel.SendAsync(WorkerControlMessage.Stop(Guid.NewGuid())));

        Assert.True(written > 4);
        Assert.Equal(written, pipe.Length);
    }

    [Fact]
    public async Task AWorkerFileThatCannotBeLaunchedIsAFixedRefusalWithoutItsPath()
    {
        // An empty file is not a program: the launch fails without starting any process.
        var root = Directory.CreateTempSubdirectory("briosa-launch-");
        try
        {
            var worker = Path.Combine(root.FullName, "Briosa.Worker.exe");
            await File.WriteAllBytesAsync(worker, []);
            await using var transport = new WorkerTransport(worker, TimeSpan.FromSeconds(5), new OneSpatialAnalyzer());
            var plan = Select(ProbePhase.Worker, FullManifest(), 34);

            var refusal = await Assert.ThrowsAsync<ProbeRefusedException>(() => transport.StartAsync(plan, LicenseAbortSignal.Never, CancellationToken.None));

            Assert.Equal("worker-start-failed", refusal.DiagnosticCode);
            Assert.DoesNotContain(root.FullName, refusal.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task TheCommandBoundaryPrintsOnlyTheExceptionType()
    {
        var error = new StringWriter();

        var code = await CommandBoundary.RunAsync(static () => throw new InvalidOperationException(@"C:\SENTINEL\private\path"), error);

        Assert.Equal(CommandBoundary.UnexpectedFailureExit, code);
        Assert.Contains("InvalidOperationException", error.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(TestSupport.Sentinel, error.ToString(), StringComparison.Ordinal);
        Assert.Equal(0, await CommandBoundary.RunAsync(static () => Task.FromResult(0), new StringWriter()));
    }

    [Theory]
    [InlineData("{\"fixture\":{\"collection\":\"SENTINEL-JOB\",\"name\":\"SENTINEL-TCP\"},\"measurements\":[null]}")]
    [InlineData("{\"fixture\":{\"collection\":\"SENTINEL-JOB\",\"name\":\"SENTINEL-TCP\"},\"measurements\":null}")]
    public void AManifestWithANullCollectionOrEntryIsRefusedNeverDereferenced(string tcp)
    {
        var manifest = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(new
        {
            schema_version = 1,
            spatial_analyzer_target = ProbeTarget.SpatialAnalyzerTarget,
            usmn_instruments = new[] { new { collection = "SENTINEL-M", instrument_id = 0 }, new { collection = "SENTINEL-M", instrument_id = 1 } },
            usmn_nominals_group = new { collection = "SENTINEL-M", name = "SENTINEL-NOMINALS" },
            template_chart_name = "SENTINEL-CHART",
            ui_profile_file = @"C:\SENTINEL\profile.xml"
        }))!.AsObject();
        manifest["tcp_fixture"] = System.Text.Json.Nodes.JsonNode.Parse(tcp);
        var instruments = manifest.DeepClone().AsObject();
        instruments["usmn_instruments"] = System.Text.Json.Nodes.JsonNode.Parse("[null, null]");
        instruments.Remove("tcp_fixture");

        var exception = Assert.Throws<InvalidDataException>(() => FixtureManifest.Parse(manifest.ToJsonString()));
        Assert.Throws<InvalidDataException>(() => FixtureManifest.Parse(instruments.ToJsonString()));
        Assert.DoesNotContain(TestSupport.Sentinel, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnAbandonedWorkerTransportSendsNoStepAndNoStop()
    {
        var transport = new WorkerTransport(@"C:\SENTINEL\Briosa.Worker.exe", TimeSpan.FromSeconds(5), new NoProcesses());
        var plan = Select(ProbePhase.Worker, FullManifest(), 34);

        transport.Abandon();
        var outcome = await transport.ExecuteAsync(plan.Steps[0], CancellationToken.None);

        Assert.Equal(ProbeOutcomeKind.RefusedByHarness, outcome.Kind);
        Assert.False(transport.WouldSendStopOnDispose);
        await transport.DisposeAsync();
    }

    private sealed class NoProcesses : IProcessCensus
    {
        public int Count(string processName) => 0;
    }

    [Fact]
    public void AMessageOverThePublishedClientLimitIsFlagged()
    {
        Assert.True(ResponseSize.FromPublicMessage(ResponseSize.PublishedClientReceiveLimitBytes + 1).ExceedsPublishedClientLimit);
        Assert.False(ResponseSize.FromPublicMessage(ResponseSize.PublishedClientReceiveLimitBytes).ExceedsPublishedClientLimit);
        Assert.True(PublicApiTransport.MeasurementReceiveLimitBytes > ResponseSize.PublishedClientReceiveLimitBytes);
    }

    [Fact]
    public async Task TheRecordsCarrySizesCountsAndTheBudgetOutcomeButNoName()
    {
        var plan = Select(ProbePhase.Worker, FullManifest(), 34);
        var record = await Run(plan, step => step.Id switch
        {
            "p34-list-100" => TestSupport.Outcome(ProbeOutcomeKind.Succeeded, ("element_count", "100")) with { Size = new ResponseSize(7321, null, false) },
            "p34-list-20000" => TestSupport.Outcome(ProbeOutcomeKind.OutputRetrievalFailed) with { Size = new ResponseSize(412, null, true) },
            _ => TestSupport.Succeeding(step)
        });
        using var document = JsonDocument.Parse(ProbeRecorder.ToJson(record));
        var steps = document.RootElement.GetProperty("steps").EnumerateArray().ToDictionary(static step => step.GetProperty("id").GetString()!);
        var markdown = ProbeRecorder.ToMarkdown(record);

        Assert.True(record.Completed);
        Assert.Equal(StepClassification.Observed, record.Steps.Single(static step => step.Step.Id == "p34-list-20000").Classification);
        Assert.Equal(WorkerResponseBudget.Bytes, document.RootElement.GetProperty("response_limits").GetProperty("worker_response_budget_bytes").GetInt32());
        Assert.Equal(ResponseSize.PublishedClientReceiveLimitBytes,
            document.RootElement.GetProperty("response_limits").GetProperty("published_client_receive_limit_bytes").GetInt32());
        var small = steps["p34-list-100"].GetProperty("outcome").GetProperty("response_size");
        Assert.Equal(7321, small.GetProperty("received_frame_bytes").GetInt32());
        Assert.False(small.GetProperty("encoding_rejected").GetBoolean());
        Assert.Equal("as received", small.GetProperty("original_size").GetString());
        Assert.True(steps["p34-list-20000"].GetProperty("outcome").GetProperty("response_size").GetProperty("encoding_rejected").GetBoolean());
        Assert.True(steps["p34-list-100"].GetProperty("measures_response_size").GetBoolean());
        Assert.Contains("**Result sizes (probe 34).**", markdown, StringComparison.Ordinal);
        Assert.Contains("| `p34-list-100` | 100 | 7321 | - | delivered |", markdown, StringComparison.Ordinal);
        Assert.Contains("| `p34-list-20000` | - | 412 | - | rejected; original size unknown |", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain(TestSupport.Sentinel, markdown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ALadderStepThatTimesOutStopsTheRunAndKeepsTheSmallerRungs()
    {
        var plan = Select(ProbePhase.Worker, FullManifest(), 34);
        var transport = new FakeTransport(plan.Phase, static step => step.Id == "p34-points-10000"
            ? ProbeOutcome.Unknown(WorkerTransport.TimeoutTransport, WorkerTransport.TimeoutDiagnosticCode)
            : TestSupport.Succeeding(step));
        var record = await Run(plan, TestSupport.Succeeding, transport);
        StepClassification Of(string id) => record.Steps.Single(step => step.Step.Id == id).Classification;

        Assert.Equal(ProbeSession.UnknownOutcomeStop, record.StopReason);
        Assert.Equal("p34-points-10000", record.StoppedAt);
        Assert.Equal(StepClassification.Observed, Of("p34-list-2000"));
        Assert.Equal(StepClassification.Unexpected, Of("p34-points-10000"));
        Assert.Equal(StepClassification.NotRun, Of("p34-list-10000"));
        Assert.Equal(StepClassification.NotRun, Of("p34-points-20000"));
        Assert.DoesNotContain("p34-list-10000", transport.Executed);
        Assert.False(record.Completed);
    }

    [Fact]
    public async Task AGetterFaultIsAnAnswerThatTheHypothesisRejects()
    {
        var plan = Select(ProbePhase.PublicApi, FullManifest(), 34, 36);
        var faulted = TestSupport.Outcome(ProbeOutcomeKind.OutputRetrievalFailed) with
        {
            Outputs = [new OutputObservation("Solution Valid", true), new OutputObservation("Result Notes", false)],
            DiagnosticCode = "sdk-output-getter-faulted"
        };
        var record = await Run(plan, step => step.Id is "p36-notes" or "p34-list-500" ? faulted : TestSupport.Succeeding(step));
        var notes = record.Steps.Single(static step => step.Step.Id == "p36-notes");

        Assert.Equal(StepClassification.Observed, notes.Classification);
        Assert.False(Assert.Single(notes.Hypotheses).Matched);
        Assert.Equal(StepClassification.Observed, record.Steps.Single(static step => step.Step.Id == "p34-list-500").Classification);
        Assert.True(record.Completed);
        Assert.Contains("MP code `3`, but at least one getter failed. Outputs retrieved: 1 of 2.", ProbeRecorder.ToMarkdown(record), StringComparison.Ordinal);
    }

    private static WorkerExecutionResponse Completed(WorkerMpExecutionResult execution) => new(
        WorkerExecutionResponseStatus.Completed, execution,
        new WorkerConnectionSnapshot(WorkerConnectionState.Connected, WorkerExecutionReadinessState.ExecutionReady, 0, 1, 1,
            "connect-ex-connected", DateTimeOffset.UnixEpoch),
        null);

    private static WorkerRetrievedOutput PointNames(int count) => new("Resultant Point Name List", WorkerMpValueKind.PointNameList,
        new WorkerPointNameListValue([.. Enumerable.Range(1, count).Select(static index =>
            new WorkerPointNameValue(FixtureNames.Collection, "SZ", $"P{index}"))]));

    // Writes one frame with the real channel, then reads it back through the harness's counter.
    private static async Task<(int Frame, long BytesOnWire, WorkerControlMessage Received)> RoundTripAsync(WorkerControlMessage message)
    {
        using var wire = new MemoryStream();
        using (var writer = new WorkerControlChannel(wire, leaveOpen: true))
        {
            await writer.SendAsync(message);
        }

        wire.Position = 0;
        using var counter = new CountingStream(wire);
        using var reader = new WorkerControlChannel(counter, leaveOpen: true);
        var before = counter.BytesRead;
        var received = await reader.ReceiveAsync();
        return (WorkerTransport.FrameBytes(before, counter.BytesRead), wire.Length, received);
    }

    // Answers one unary call with a fixed message or error, as a server would over HTTP/2.
    private sealed class ReplyingInvoker : CallInvoker
    {
        private readonly object? _response;
        private readonly RpcException? _error;

        public ReplyingInvoker(IMessage response) => _response = response;

        public ReplyingInvoker(RpcException error) => _error = error;

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) =>
            new(_error is null ? Task.FromResult((TResponse)_response!) : Task.FromException<TResponse>(_error),
                Task.FromResult(new Metadata()), static () => Status.DefaultSuccess, static () => [], static () => { });

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) =>
            throw new NotSupportedException();

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) => throw new NotSupportedException();

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options) => throw new NotSupportedException();

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options) => throw new NotSupportedException();
    }
}
