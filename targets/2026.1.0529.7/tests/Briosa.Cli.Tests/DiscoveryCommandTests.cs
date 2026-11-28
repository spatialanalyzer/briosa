using System.Text.Json.Nodes;
using Briosa.Cli.Engine;

namespace Briosa.Cli.Tests;

/// <summary>
/// <c>status</c> and <c>ops</c> through the real CLI: they report what the server
/// says, send only discovery RPCs, and say what an older server does not report.
/// </summary>
public sealed class DiscoveryCommandTests
{
    [Fact]
    public async Task StatusReportsTheServersClaimsPolicyAndFixedGuidance()
    {
        await using var server = await FakeBriosaServer.StartAsync();

        var run = await CliRun.RunAsync(["status", "--endpoint", server.Endpoint]);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("succeeded", run.Outcome);
        Assert.True(run.Envelope["server"]!["verified"]!.GetValue<bool>());
        Assert.True(run.Envelope["server"]!["execution_supported"]!.GetValue<bool>());
        var info = run.Data["server_info"]!;
        Assert.Equal("CONFIGURED_SPATIAL_ANALYZER_INTERACTION_MODE_SILENT",
            info["configured_spatial_analyzer_interaction_mode"]!.GetValue<string>());
        Assert.Equal("RUNTIME_IDENTITY_MATCH_STATE_EXACT_MATCH",
            info["activated_sdk_identity"]!["match_state"]!.GetValue<string>());
        Assert.Equal("RUNTIME_IDENTITY_EVIDENCE_SOURCE_OPERATOR_ATTESTATION",
            info["connected_spatial_analyzer_identity"]!["source"]!.GetValue<string>());
        Assert.Equal("SPATIAL_ANALYZER_EXECUTION_READINESS_STATE_EXECUTION_READY",
            info["spatial_analyzer_execution_readiness_state"]!.GetValue<string>());
        Assert.Equal("standard", run.Data["admission_profile"]!.GetValue<string>());
        Assert.StartsWith("sha256:", run.Data["policy_fingerprint"]!.GetValue<string>(), StringComparison.Ordinal);
        Assert.Empty(CallOutcomeTests.Warnings(run));
        Assert.Contains(CallOutcomeTests.Guidance(run), text =>
            text.Contains("working frame", StringComparison.Ordinal) &&
            text.Contains("never makes hidden preliminary MP calls", StringComparison.Ordinal));
        Assert.Null(run.Envelope["execution"]);
        Assert.Equal(CallOutcomeTests.EnvelopeMembers, run.Envelope.Select(member => member.Key).ToArray());
        Assert.Empty(server.MpCalls);
        CallOutcomeTests.AssertOnlyKnownRpcs(server);
    }

    [Theory]
    [InlineData(ConfiguredSpatialAnalyzerInteractionMode.Unchanged)]
    [InlineData(ConfiguredSpatialAnalyzerInteractionMode.Manual)]
    [InlineData(ConfiguredSpatialAnalyzerInteractionMode.Unspecified)]
    [InlineData((ConfiguredSpatialAnalyzerInteractionMode)42)]
    public async Task StatusWarnsWhenTheInteractionModeIsNotSilent(ConfiguredSpatialAnalyzerInteractionMode mode)
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var info = FakeBriosaServer.DefaultServerInfo();
        info.ConfiguredSpatialAnalyzerInteractionMode = mode;
        info.ReadyForMp = false;
        server.ServerInfo = info;

        var run = await CliRun.RunAsync(["status", "--endpoint", server.Endpoint]);

        Assert.Equal(0, run.ExitCode);
        Assert.Contains("interaction-mode-not-silent", CallOutcomeTests.Warnings(run));
        Assert.Contains("not-ready-for-mp", CallOutcomeTests.Warnings(run));
        Assert.False(run.Data["server_info"]!["ready_for_mp"]!.GetValue<bool>());
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    public async Task OlderServersStillAnswerDiscoveryAndSayWhatIsMissing(uint revision)
    {
        await using var server = await FakeBriosaServer.StartAsync();
        server.ServerInfo = FakeBriosaServer.DefaultServerInfo(revision);
        var capabilities = ServerProjection.AllCapabilities;
        foreach (var operation in capabilities.Operations)
        {
            operation.MpStep = string.Empty;
            if (revision == 0)
            {
                operation.RequestOptions = null;
                operation.PolicyDenial = null;
            }
        }

        server.AllCapabilities = capabilities;

        var status = await CliRun.RunAsync(["status", "--endpoint", server.Endpoint]);
        var search = await CliRun.RunAsync(["ops", "search", "--query", "working", "--endpoint", server.Endpoint]);
        var describe = await CliRun.RunAsync(["ops", "describe", "file_operations.get_working_directory",
            "--endpoint", server.Endpoint]);

        Assert.Equal(0, status.ExitCode);
        Assert.False(status.Data["call_available"]!.GetValue<bool>());
        Assert.Contains("server-predates-caller-restriction", CallOutcomeTests.Warnings(status));
        Assert.Equal(revision == 0, CallOutcomeTests.Warnings(status).Contains("server-predates-request-options"));
        Assert.Equal(0, search.ExitCode);
        Assert.Contains("mp-step-not-reported", CallOutcomeTests.Warnings(search));
        Assert.All(search.Data["operations"]!.AsArray(), operation => Assert.Null(operation!["mp_step"]));
        Assert.Equal(0, describe.ExitCode);
        Assert.False(describe.Data["operation"]!["served_by_call"]!.GetValue<bool>());
        Assert.Equal(revision != 0, describe.Data["request_options"]!["described"]!.GetValue<bool>());
        Assert.Equal(revision == 0, CallOutcomeTests.Warnings(describe).Contains("request-options-not-described"));
        Assert.Empty(server.MpCalls);
    }

    [Fact]
    public async Task CategoriesCountEveryOperationOnceWithItsAdmissionAndTier()
    {
        await using var server = await FakeBriosaServer.StartAsync();

        var run = await CliRun.RunAsync(["ops", "categories", "--endpoint", server.Endpoint]);

        Assert.Equal(0, run.ExitCode);
        var categories = run.Data["categories"]!.AsArray();
        Assert.Equal(20, categories.Count);
        Assert.Equal(ServerProjection.Registry.Count, categories.Sum(category => category!["operations"]!.GetValue<int>()));
        foreach (var category in categories)
        {
            var operations = category!["operations"]!.GetValue<int>();
            Assert.Equal(operations, category["admitted"]!.GetValue<int>() + category["denied"]!.GetValue<int>());
            Assert.Equal(operations, category["tiers"]!.AsObject().Sum(tier => tier.Value!.GetValue<int>()));
        }

        Assert.Empty(server.MpCalls);
    }

    [Fact]
    public async Task SearchMatchesMpStepsAndFiltersAndPages()
    {
        await using var server = await FakeBriosaServer.StartAsync();

        var byStep = await Search(server, "--query", "get working directory");
        Assert.Equal("file_operations.get_working_directory",
            Assert.Single(byStep.Data["operations"]!.AsArray())!["operation_id"]!.GetValue<string>());
        Assert.Equal("Get Working Directory",
            byStep.Data["operations"]![0]!["mp_step"]!.GetValue<string>());

        var admitted = await Search(server, "--limit", "100");
        var all = await Search(server, "--include-denied", "--limit", "100");
        Assert.True(all.Data["total_matches"]!.GetValue<int>() > admitted.Data["total_matches"]!.GetValue<int>());
        Assert.Equal(ServerProjection.Registry.Count, all.Data["total_matches"]!.GetValue<int>());
        Assert.Equal(100, all.Data["next_offset"]!.GetValue<int>());
        Assert.All(admitted.Data["operations"]!.AsArray(), operation =>
            Assert.Equal("OPERATION_ADMISSION_ADMITTED", operation!["admission"]!.GetValue<string>()));

        var denied = await Search(server, "--include-denied", "--tier", "excluded", "--risk-flag", "physical_motion",
            "--limit", "100");
        Assert.NotEmpty(denied.Data["operations"]!.AsArray());
        Assert.All(denied.Data["operations"]!.AsArray(), operation =>
        {
            Assert.Equal("excluded", operation!["tier"]!.GetValue<string>());
            Assert.Contains("OPERATION_RISK_FLAG_PHYSICAL_MOTION",
                operation["risk_flags"]!.AsArray().Select(flag => flag!.GetValue<string>()));
            Assert.NotNull(operation["policy_denial"]);
        });

        var last = await Search(server, "--include-denied", "--offset",
            (ServerProjection.Registry.Count - 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Single(last.Data["operations"]!.AsArray());
        Assert.Null(last.Data["next_offset"]);

        var validated = await Search(server, "--include-denied", "--validated-only", "--category", "analysis_operations",
            "--effect", "read_only", "--limit", "100");
        Assert.All(validated.Data["operations"]!.AsArray(), operation =>
        {
            Assert.Equal("OPERATION_VALIDATION_STATUS_NO_RECORDED_GAP", operation!["validation_status"]!.GetValue<string>());
            Assert.Equal("analysis_operations", operation["category"]!.GetValue<string>());
            Assert.Equal("OPERATION_EFFECT_READ_ONLY", operation["effect"]!.GetValue<string>());
        });
        Assert.Empty(server.MpCalls);
    }

    [Fact]
    public async Task DescribeGivesASchemaWithoutRequirednessAndTheReviewedOptions()
    {
        await using var server = await FakeBriosaServer.StartAsync();

        var run = await CliRun.RunAsync(["ops", "describe", CallOutcomeTests.Operation, "--endpoint", server.Endpoint]);

        Assert.Equal(0, run.ExitCode);
        Assert.True(run.Data["operation"]!["served_by_call"]!.GetValue<bool>());
        Assert.Equal("read_only", run.Data["operation"]!["tier"]!.GetValue<string>());
        Assert.Equal(ServerProjection.Capability(CallOutcomeTests.Operation).MpStep,
            run.Data["operation"]!["mp_step"]!.GetValue<string>());
        Assert.Equal(Wire.RequirednessStatement, run.Data["requiredness"]!.GetValue<string>());
        var schema = run.Data["request_schema"]!;
        var request = schema["$defs"]!["briosa.GetIthPointFromGroupRequest"]!;
        Assert.False(request["additionalProperties"]!.GetValue<bool>());
        Assert.Equal(["group_name", "point_index"], request["properties"]!.AsObject().Select(property => property.Key).ToArray());
        Assert.Equal("integer", request["properties"]!["point_index"]!["type"]!.GetValue<string>());
        Assert.Contains("OBJECT_TYPE_POINT_GROUP", schema["$defs"]!["briosa.CollectionObjectName"]!["properties"]!
            ["object_type"]!["enum"]!.AsArray().Select(value => value!.GetValue<string>()));
        Assert.DoesNotContain("\"required\"", schema.ToJsonString(), StringComparison.Ordinal);
        Assert.True(run.Data["request_options"]!["described"]!.GetValue<bool>());
        Assert.Equal(Wire.MaximumInputBytes, run.Data["input_rules"]!["max_input_bytes"]!.GetValue<int>());
        Assert.Equal(Wire.MaximumDepth, run.Data["input_rules"]!["max_nesting_depth"]!.GetValue<int>());
        Assert.Equal(Wire.MaximumEncodedRequestBytes, run.Data["input_rules"]!["max_encoded_request_bytes"]!.GetValue<int>());
        Assert.Empty(server.MpCalls);
    }

    [Fact]
    public async Task DescribeCallsOutAnOptionThatActivatesWhenOmitted()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var operation = ServerProjection.AllCapabilities.Operations.First(candidate =>
            candidate.RequestOptions?.Options.Any(option =>
                option.Omission == OperationRequestOptionOmission.Activates) == true);

        var run = await CliRun.RunAsync(["ops", "describe", operation.OperationId, "--endpoint", server.Endpoint]);

        Assert.Equal(0, run.ExitCode);
        Assert.Contains("option-activates-when-omitted", CallOutcomeTests.Warnings(run));
        var options = run.Data["request_options"]!["options"]!.AsArray();
        Assert.Contains(options, option => option!["activates_when_omitted"]!.GetValue<bool>() &&
            option["omission"]!.GetValue<string>() == "OPERATION_REQUEST_OPTION_OMISSION_ACTIVATES" &&
            option["field_in_request_schema"]!.GetValue<bool>());
    }

    [Fact]
    public async Task DescribeOfAnUnknownOperationIsRefusedLocally()
    {
        await using var server = await FakeBriosaServer.StartAsync();

        var run = await CliRun.RunAsync(["ops", "describe", "file_operations.no_such_operation", "--endpoint", server.Endpoint]);

        Assert.Equal(3, run.ExitCode);
        Assert.Equal("operation-unknown", run.DiagnosticCode);
    }

    [Fact]
    public void EveryRegisteredRequestSchemaUsesExactFieldNamesAndNoRequiredList()
    {
        foreach (var operation in ServerProjection.Registry)
        {
            var method = ProtocolCatalog.Instance.FindMethod(operation.FullyQualifiedMethod)!;
            var schema = RequestSchema.Build(method.InputType);
            var text = schema.ToJsonString();
            Assert.DoesNotContain("\"required\"", text, StringComparison.Ordinal);
            var definition = schema["$defs"]![method.InputType.FullName]!;
            Assert.Equal(method.InputType.Fields.InFieldNumberOrder().Select(field => field.Name).ToArray(),
                definition["properties"]!.AsObject().Select(property => property.Key).ToArray());
        }
    }

    private static Task<CliRun> Search(FakeBriosaServer server, params string[] options) =>
        CliRun.RunAsync(["ops", "search", .. options, "--endpoint", server.Endpoint]);
}
