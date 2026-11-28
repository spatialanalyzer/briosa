using Briosa.Server.Operations;
using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Operations.RobotOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

/// <summary>
/// Typed policy denial detail (#397): the step that denied a call travels in the
/// existing error trailer, discovery names the flag of a flag denial, and
/// discovery describes each operation's reviewed request options.
/// </summary>
public sealed class PolicyDenialDetailTests
{
    private const string GetWorkingDirectory = "file_operations.get_working_directory";
    private const string DeleteGeneralFile = "file_operations.delete_general_file";
    private const string SaveAs = "file_operations.save_as";
    private const string DirectCadAccess = "file_operations.direct_cad_access";
    private const string QueryPointsToObjects = "analysis_operations.query_points_to_objects";
    private const string StartInstrumentInterface = "instrument_operations.start_instrument_interface";
    private const string AutoMeasurePoints = "instrument_operations.auto_measure_points";
    private const string MoveToFrame = "robot_operations.move_robot_machine_to_frame";
    private const string DeniedStatusText = "The operation is denied by the Briosa operation policy.";

    // The field numbers of each message as Server 0.10.0 and the 0.5.0 clients know it.
    private static readonly int[] OperationErrorFieldsOfServer0100 = [1, 2, 3, 5, 6, 7, 8, 9, 10];
    private static readonly int[] OperationCapabilityFieldsOfServer0100 = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];

    private static readonly Api.CollectionMachineId Machine = new() { CollectionName = "Robots", MachineId = 12 };
    private static readonly Api.CollectionObjectName Frame = new() { CollectionName = "Frames", ObjectName = "Approach" };

    [Fact]
    public void OneExhaustiveMappingNamesTheSameStepInBothPublicEnums()
    {
        var admissions = Api.DiscoveryReflection.Descriptor.EnumTypes
            .Single(type => type.Name == nameof(Api.OperationAdmission));
        var rules = Api.OperationOutcomesReflection.Descriptor.EnumTypes
            .Single(type => type.Name == nameof(Api.PolicyDenialRule));
        var mapped = Enum.GetValues<OperationPolicyDenialReason>()
            .Select(reason => (Reason: reason, Public: OperationPolicyDenial.Map(reason)))
            .ToArray();
        var configured = mapped.Where(item => item.Public.Admission is not null).ToArray();
        var caller = mapped.Where(item => item.Public.Admission is null).ToArray();

        // Every denied admission and every specified rule appear exactly once.
        Assert.Equal(
            admissions.Values
                .Where(value => value.Name.StartsWith("OPERATION_ADMISSION_DENIED_", StringComparison.Ordinal))
                .Select(value => value.Number)
                .Order(),
            configured.Select(item => (int)item.Public.Admission!.Value).Order());
        Assert.Equal(
            rules.Values.Where(value => value.Number != 0).Select(value => value.Number).Order(),
            mapped.Select(item => (int)item.Public.Rule).Order());
        Assert.All(configured, item =>
        {
            // Matched by name: the two enums number the same steps differently.
            Assert.Equal(
                admissions.FindValueByNumber((int)item.Public.Admission!.Value).Name["OPERATION_ADMISSION_DENIED_".Length..],
                rules.FindValueByNumber((int)item.Public.Rule).Name["POLICY_DENIAL_RULE_".Length..]);
            Assert.NotEqual((int)item.Public.Admission!.Value, (int)item.Public.Rule);
            Assert.Equal(item.Reason.ToString(), item.Public.Rule.ToString());
        });

        // Only a call carries a caller restriction (#443), so its rules have no admission.
        Assert.Equal([OperationPolicyDenialReason.CallerProfile, OperationPolicyDenialReason.CallerFlag],
            caller.Select(item => item.Reason));
        Assert.All(caller, item =>
        {
            Assert.StartsWith("POLICY_DENIAL_RULE_CALLER_", rules.FindValueByNumber((int)item.Public.Rule).Name,
                StringComparison.Ordinal);
            Assert.Equal(item.Reason.ToString(), item.Public.Rule.ToString());
        });
        Assert.Throws<InvalidOperationException>(() =>
            OperationPolicyDenial.CallerFlag(OperationRisks.InteractiveUi).ToAdmission());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OperationPolicyDenial.Map((OperationPolicyDenialReason)mapped.Length));
    }

    [Fact]
    public void AuditRulesKeepTheirFormAndComeFromTheStructuredRecord()
    {
        Assert.Equal("classification", OperationPolicyDenial.Unreviewed().PolicyRule);
        Assert.Equal("isolation", OperationPolicyDenial.Exclusive().PolicyRule);
        Assert.Equal("option.acknowledge_arrival", OperationPolicyDenial.Exclusive("acknowledge_arrival").PolicyRule);
        Assert.Equal("override", OperationPolicyDenial.Override().PolicyRule);
        Assert.Equal("flag.filesystem_write", OperationPolicyDenial.Flag(OperationRisks.FilesystemWrite).PolicyRule);
        Assert.Equal(
            "option.show_results_dialog/flag.interactive_ui",
            OperationPolicyDenial.Flag(OperationRisks.InteractiveUi, "show_results_dialog").PolicyRule);
        Assert.Equal("profile.standard", OperationPolicyDenial.Profile("standard").PolicyRule);
        Assert.Equal(
            "option.show_results_dialog/profile.standard",
            OperationPolicyDenial.Profile("standard", "show_results_dialog").PolicyRule);
        Assert.Equal("classification", OperationPolicyDenial.DurationUnreviewed.PolicyRule);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OperationPolicyDenial.Flag(OperationRisks.FilesystemRead | OperationRisks.FilesystemWrite));
        Assert.Throws<ArgumentException>(() => OperationPolicyDenial.Profile(" "));
        Assert.Throws<ArgumentException>(() => OperationPolicyDenial.Exclusive(" "));
        Assert.Throws<ArgumentException>(() => new OperationPolicyDecision(
            OperationPolicyDecisionKind.Denied, "operation-policy-denied", Operation: null, "override"));
        var decision = OperationPolicyDecision.Deny("operation-policy-denied", null, OperationPolicyDenial.Override());
        Assert.Equal(OperationPolicyDecisionKind.Denied, decision.Kind);
        Assert.Equal("override", decision.PolicyRule);
    }

    [Fact]
    public void OnlyAPolicyDeniedOutcomeCarriesADenial()
    {
        Assert.Throws<ArgumentException>(() => new WorkerExecutionOutcome(
            WorkerExecutionStatus.PolicyDenied, WorkerExecutionDisposition.NotStarted,
            Execution: null, Connection: null, "operation-policy-denied", Generation: 0));
        Assert.Throws<ArgumentException>(() => new WorkerExecutionOutcome(
            WorkerExecutionStatus.Unsupported, WorkerExecutionDisposition.NotStarted,
            Execution: null, Connection: null, "operation-unsupported", Generation: 0,
            PolicyDenial: OperationPolicyDenial.Override()));
        var denied = new WorkerExecutionOutcome(
            WorkerExecutionStatus.PolicyDenied, WorkerExecutionDisposition.NotStarted,
            Execution: null, Connection: null, "operation-policy-denied", Generation: 0,
            PolicyDenial: OperationPolicyDenial.Override());
        Assert.Same(denied.PolicyDenial, denied.WithGeneration(4).PolicyDenial);
    }

    [Fact]
    public async Task ProfileDenialNamesTheProfileAndNoFlagOnTheCall()
    {
        var details = await DenyFileOperationAsync(
            Policy(),
            DeleteGeneralFile,
            "operation-policy-denied",
            client => client.DeleteGeneralFileAsync(new Api.DeleteGeneralFileRequest()).ResponseAsync)
            .ConfigureAwait(true);

        AssertDetails(details, Api.PolicyDenialRule.Profile, profile: "standard");
    }

    [Fact]
    public async Task FlagDenialNamesTheFlagOnTheCall()
    {
        var details = await DenyFileOperationAsync(
            Policy(settings: new() { ["Flags:filesystem_write"] = "deny" }),
            SaveAs,
            "operation-policy-denied",
            client => client.SaveAsAsync(new Api.SaveAsRequest()).ResponseAsync)
            .ConfigureAwait(true);

        AssertDetails(details, Api.PolicyDenialRule.Flag, flag: "filesystem_write");
    }

    [Fact]
    public async Task FlagDenialNamesTheFirstDeniedFlagInDeclarationOrder()
    {
        // delete_general_file carries filesystem_delete and destructive.
        var details = await DenyFileOperationAsync(
            Policy(profile: "full", settings: new()
            {
                ["Flags:destructive"] = "deny",
                ["Flags:filesystem_delete"] = "deny"
            }),
            DeleteGeneralFile,
            "operation-policy-denied",
            client => client.DeleteGeneralFileAsync(new Api.DeleteGeneralFileRequest()).ResponseAsync)
            .ConfigureAwait(true);

        AssertDetails(details, Api.PolicyDenialRule.Flag, flag: "filesystem_delete");
    }

    [Fact]
    public async Task OverrideDenialCarriesNoOtherDetailOnTheCall()
    {
        var details = await DenyFileOperationAsync(
            Policy(settings: new() { ["Overrides:file_operations:get_working_directory"] = "deny" }),
            GetWorkingDirectory,
            "operation-policy-denied",
            client => client.GetWorkingDirectoryAsync(new Api.GetWorkingDirectoryRequest()).ResponseAsync)
            .ConfigureAwait(true);

        AssertDetails(details, Api.PolicyDenialRule.Override);
    }

    [Fact]
    public async Task ExclusiveDenialOfTheReviewedRowNamesNoFieldOnTheCall()
    {
        var details = await DenyAsync<InstrumentOperationsService>(
            Policy(profile: "full", settings: new()
            {
                ["Overrides:instrument_operations:start_instrument_interface"] = "allow"
            }),
            StartInstrumentInterface,
            "operation-isolation-unsupported",
            channel => new Api.InstrumentOperations.InstrumentOperationsClient(channel)
                .StartInstrumentInterfaceAsync(new Api.StartInstrumentInterfaceRequest()).ResponseAsync)
            .ConfigureAwait(true);

        AssertDetails(details, Api.PolicyDenialRule.Exclusive);
    }

    [Fact]
    public async Task ExclusiveDenialByARequestOptionNamesTheFieldOnTheCall()
    {
        var details = await DenyAsync<RobotOperationsService>(
            Policy(profile: "device"),
            MoveToFrame,
            "operation-option-isolation-unsupported",
            channel => new Api.RobotOperations.RobotOperationsClient(channel)
                .MoveRobotMachineToFrameAsync(new Api.MoveRobotMachineToFrameRequest
                {
                    MachineId = Machine, DestinationFrame = Frame, AcknowledgeArrival = false
                }).ResponseAsync)
            .ConfigureAwait(true);

        AssertDetails(details, Api.PolicyDenialRule.Exclusive, field: "acknowledge_arrival");
    }

    [Fact]
    public async Task UnreviewedDenialCarriesNoOtherDetailOnTheCall()
    {
        var details = await DenyFileOperationAsync(
            Policy(profile: "full", classify: candidate => candidate == GetWorkingDirectory
                ? OperationClassificationLookup.Unreviewed
                : OperationClassification.Find(candidate)),
            GetWorkingDirectory,
            "operation-risk-unreviewed",
            client => client.GetWorkingDirectoryAsync(new Api.GetWorkingDirectoryRequest()).ResponseAsync)
            .ConfigureAwait(true);

        AssertDetails(details, Api.PolicyDenialRule.Unreviewed);
    }

    [Fact]
    public async Task UnclassifiableRequestIsUnreviewedAndNamesNoField()
    {
        var unreadable = new OperationConditionalOption(
            QueryPointsToObjects, "no_such_field", "Show Results Dialog?",
            OperationOptionEffect.InteractiveUi, OperationOptionTrigger.CallerOption,
            OperationOptionCondition.WhenTrue, OperationOptionAbsence.SendsFalse);
        var details = await DenyAsync<AnalysisOperationsService>(
            Policy(
                profile: "full",
                settings: new() { ["Overrides:analysis_operations:query_points_to_objects"] = "allow" },
                requestClassifier: new OperationRequestClassifier([unreadable])),
            QueryPointsToObjects,
            "operation-request-unclassified",
            channel => new Api.AnalysisOperations.AnalysisOperationsClient(channel)
                .QueryPointsToObjectsAsync(new Api.QueryPointsToObjectsRequest()).ResponseAsync)
            .ConfigureAwait(true);

        AssertDetails(details, Api.PolicyDenialRule.Unreviewed);
    }

    [Fact]
    public async Task ProfileDenialByARequestOptionNamesTheProfileAndTheFieldButNoFlag()
    {
        var details = await DenyAsync<AnalysisOperationsService>(
            Policy(),
            QueryPointsToObjects,
            "operation-option-denied",
            channel => new Api.AnalysisOperations.AnalysisOperationsClient(channel)
                .QueryPointsToObjectsAsync(new Api.QueryPointsToObjectsRequest { ShowResultsDialog = true })
                .ResponseAsync)
            .ConfigureAwait(true);

        AssertDetails(details, Api.PolicyDenialRule.Profile, profile: "standard", field: "show_results_dialog");
    }

    [Fact]
    public async Task FlagDenialByARequestOptionNamesTheFlagAndTheField()
    {
        var details = await DenyAsync<AnalysisOperationsService>(
            Policy(settings: new() { ["Flags:interactive_ui"] = "deny" }),
            QueryPointsToObjects,
            "operation-option-denied",
            channel => new Api.AnalysisOperations.AnalysisOperationsClient(channel)
                .QueryPointsToObjectsAsync(new Api.QueryPointsToObjectsRequest { ShowResultsDialog = true })
                .ResponseAsync)
            .ConfigureAwait(true);

        AssertDetails(details, Api.PolicyDenialRule.Flag, flag: "interactive_ui", field: "show_results_dialog");
    }

    [Fact]
    public async Task TwoEnabledGuardsNameTheFirstInReviewedTableOrder()
    {
        var both = new Api.DirectCadAccessRequest { PromptOnMissingComponents = true, SelectiveImport = true };
        var fields = OperationRequestClassifier.Default.OptionsFor(DirectCadAccess)
            .Select(option => option.Field)
            .ToArray();
        Assert.Equal(["prompt_on_missing_components", "selective_import"], fields);

        var details = await DenyFileOperationAsync(
            Policy(),
            DirectCadAccess,
            "operation-option-denied",
            client => client.DirectCadAccessAsync(both).ResponseAsync)
            .ConfigureAwait(true);

        AssertDetails(details, Api.PolicyDenialRule.Profile, profile: "standard", field: "prompt_on_missing_components");
        AssertDetails(
            Policy().EvaluateRequest(DirectCadAccess, new Api.DirectCadAccessRequest { SelectiveImport = true })
                .Denial!.ToDetails(),
            Api.PolicyDenialRule.Profile, profile: "standard", field: "selective_import");
        AssertDetails(
            Policy(settings: new() { ["Flags:interactive_ui"] = "deny" }).EvaluateRequest(DirectCadAccess, both)
                .Denial!.ToDetails(),
            Api.PolicyDenialRule.Flag, flag: "interactive_ui", field: "prompt_on_missing_components");
    }

    [Fact]
    public void WorkLeftRunningPreemptsAnEarlierUiGuardAndADenyOverride()
    {
        // auto_start (operator UI) precedes wait_for_completion (work left running) in the table.
        var both = new Api.AutoMeasurePointsRequest { AutoStart = false, WaitForCompletion = false };
        var uiOnly = new Api.AutoMeasurePointsRequest { AutoStart = false, WaitForCompletion = true };
        var denyOverride = new Dictionary<string, string?>
        {
            ["Overrides:instrument_operations:auto_measure_points"] = "deny"
        };

        foreach (var policy in new[]
        {
            Policy(profile: "device", settings: new() { ["Flags:interactive_ui"] = "allow" }),
            Policy(profile: "device", settings: denyOverride)
        })
        {
            var decision = policy.EvaluateRequest(AutoMeasurePoints, both);
            Assert.Equal("operation-option-isolation-unsupported", decision.DiagnosticCode);
            AssertDetails(decision.Denial!.ToDetails(), Api.PolicyDenialRule.Exclusive, field: "wait_for_completion");
        }

        // A deny override is a static decision and names no request option.
        var overridden = Policy(profile: "device", settings: denyOverride).EvaluateRequest(AutoMeasurePoints, uiOnly);
        Assert.Equal("operation-policy-denied", overridden.DiagnosticCode);
        AssertDetails(overridden.Denial!.ToDetails(), Api.PolicyDenialRule.Override);
    }

    [Fact]
    public async Task ExecutionBudgetDenialIsUnreviewedOnTheCall()
    {
        // The policy admits the request; the supervisor finds no reviewed duration class.
        var factory = new RejectingProcessFactory();
        var supervisor = new WorkerProcessSupervisor(
            factory,
            new WorkerLifecyclePolicy(
                heartbeatInterval: TimeSpan.FromSeconds(1),
                heartbeatTimeout: TimeSpan.FromSeconds(1),
                startupTimeout: TimeSpan.FromSeconds(1),
                shutdownTimeout: TimeSpan.FromSeconds(1)),
            new WorkerExecutionPolicy(
                TimeSpan.FromSeconds(3),
                queueCapacity: 4,
                durationClassOf: _ => OperationDurationClass.Unspecified));
        await using var configuredSupervisor = supervisor.ConfigureAwait(true);
        var executor = new PolicyEnforcingWorkerCommandExecutor(
            supervisor, supervisor, Policy(),
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance));
        var host = await GrpcTestHost.StartAsync<FileOperationsService>(executor).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            new Api.FileOperations.FileOperationsClient(host.Channel)
                .GetWorkingDirectoryAsync(new Api.GetWorkingDirectoryRequest()).ResponseAsync)
            .ConfigureAwait(true);

        var details = AssertDeniedCall(exception, GetWorkingDirectory, "operation-duration-unreviewed");
        AssertDetails(details, Api.PolicyDenialRule.Unreviewed);
        Assert.Equal(0, factory.StartCount);
    }

    [Fact]
    public void NoOtherFailureKindCarriesPolicyDenialDetail()
    {
        foreach (var status in Enum.GetValues<WorkerExecutionStatus>()
            .Where(status => status is not (WorkerExecutionStatus.Completed or WorkerExecutionStatus.PolicyDenied)))
        {
            var disposition = status == WorkerExecutionStatus.WatchdogTimeout
                ? WorkerExecutionDisposition.StartedOutcomeUnknown
                : WorkerExecutionDisposition.NotStarted;
            var outcome = new WorkerExecutionOutcome(
                status, disposition, Execution: null, Connection: null, "test-diagnostic", Generation: 1);

            var exception = Assert.Throws<RpcException>(() => GrpcOperationOutcomeMapper.RequireSuccess(
                outcome, GetWorkingDirectory, Api.ReplaySafety.Safe, [], callerDeadlineExceeded: false));

            Assert.Null(ParseError(exception).PolicyDenial);
        }
    }

    [Fact]
    public void DiscoveryNamesTheDenyingStepOfEveryDeniedOperation()
    {
        var policy = Policy(settings: new()
        {
            ["Overrides:file_operations:get_working_directory"] = "deny",
            ["Flags:filesystem_write"] = "deny"
        });

        var all = Discovery(policy).CreateCapabilities(includeDenied: true);
        var operations = all.Operations.ToDictionary(operation => operation.OperationId, StringComparer.Ordinal);

        AssertDetails(operations[GetWorkingDirectory].PolicyDenial, Api.PolicyDenialRule.Override);
        AssertDetails(operations[SaveAs].PolicyDenial, Api.PolicyDenialRule.Flag, flag: "filesystem_write");
        AssertDetails(operations[DeleteGeneralFile].PolicyDenial, Api.PolicyDenialRule.Profile, profile: "standard");
        AssertDetails(operations[StartInstrumentInterface].PolicyDenial, Api.PolicyDenialRule.Exclusive);
        Assert.All(all.Operations, operation =>
        {
            Assert.NotNull(operation.RequestOptions);
            Assert.Equal(operation.Admission != Api.OperationAdmission.Admitted, operation.PolicyDenial is not null);
            if (operation.PolicyDenial is { } denial)
            {
                // The reviewed row decided, so no request field is named.
                Assert.False(denial.HasRequestOptionField, operation.OperationId);
                Assert.Equal($"Denied{denial.Rule}", operation.Admission.ToString());
                Assert.Equal(denial.Rule == Api.PolicyDenialRule.Flag, denial.HasDeniedFlag);
                Assert.Equal(denial.Rule == Api.PolicyDenialRule.Profile, denial.HasAdmissionProfile);
            }
        });

        var unreviewed = Assert.Single(
            Discovery(Policy(profile: "full", classify: candidate => candidate == GetWorkingDirectory
                ? OperationClassificationLookup.Unreviewed
                : OperationClassification.Find(candidate))).CreateCapabilities(includeDenied: true).Operations,
            operation => operation.OperationId == GetWorkingDirectory);
        Assert.Equal(Api.OperationAdmission.DeniedUnreviewed, unreviewed.Admission);
        AssertDetails(unreviewed.PolicyDenial, Api.PolicyDenialRule.Unreviewed);
    }

    [Fact]
    public void DefaultListingShowsTheDeniedOptionOfAnAdmittedOperation()
    {
        var listed = Assert.Single(
            Discovery(Policy()).CreateCapabilities().Operations,
            operation => operation.OperationId == QueryPointsToObjects);

        Assert.Equal(Api.OperationAdmission.Admitted, listed.Admission);
        Assert.Null(listed.PolicyDenial);
        var option = Assert.Single(listed.RequestOptions.Options);
        Assert.Equal("show_results_dialog", option.FieldName);
        Assert.Equal(Api.OperationRequestOptionEffect.InteractiveUi, option.Effect);
        Assert.Equal(Api.OperationRequestOptionCondition.WhenTrue, option.Condition);
        Assert.Empty(option.EnablingEnumValues);
        Assert.Equal(Api.OperationRequestOptionOmission.DoesNotActivate, option.Omission);
        Assert.Equal(Api.OperationAdmission.DeniedProfile, option.Admission);
        AssertDetails(option.PolicyDenial, Api.PolicyDenialRule.Profile, profile: "standard", field: "show_results_dialog");

        var flagDenied = Assert.Single(Assert.Single(
            Discovery(Policy(settings: new() { ["Flags:interactive_ui"] = "deny" })).CreateCapabilities().Operations,
            operation => operation.OperationId == QueryPointsToObjects).RequestOptions.Options);
        Assert.Equal(Api.OperationAdmission.DeniedFlag, flagDenied.Admission);
        AssertDetails(flagDenied.PolicyDenial, Api.PolicyDenialRule.Flag, flag: "interactive_ui", field: "show_results_dialog");

        var admitted = Assert.Single(Assert.Single(
            Discovery(Policy(settings: new() { ["Flags:interactive_ui"] = "allow" })).CreateCapabilities().Operations,
            operation => operation.OperationId == QueryPointsToObjects).RequestOptions.Options);
        Assert.Equal(Api.OperationAdmission.Admitted, admitted.Admission);
        Assert.Null(admitted.PolicyDenial);
    }

    [Fact]
    public void AnOperationWithoutOptionsReportsAPresentEmptyList()
    {
        var listed = Assert.Single(
            Discovery(Policy()).CreateCapabilities().Operations,
            operation => operation.OperationId == GetWorkingDirectory);
        Assert.Empty(OperationRequestClassifier.Default.OptionsFor(GetWorkingDirectory));

        // The empty container is on the wire, so a client can tell "none" from "not described".
        var decoded = Api.OperationCapability.Parser.ParseFrom(listed.ToByteArray());
        Assert.NotNull(decoded.RequestOptions);
        Assert.Empty(decoded.RequestOptions.Options);
        Assert.Null(decoded.PolicyDenial);
    }

    [Fact]
    public void DiscoveryAnswersEachOptionWithTheEvaluatorOfARealRequest()
    {
        foreach (var policy in new[]
        {
            Policy(profile: "read-only"),
            Policy(),
            Policy(settings: new() { ["Flags:interactive_ui"] = "deny" }),
            Policy(profile: "device", settings: new() { ["Flags:interactive_ui"] = "allow" }),
            Policy(profile: "full", settings: new()
            {
                ["Flags:interactive_ui"] = "allow",
                ["Overrides:instrument_operations:auto_measure_points"] = "deny",
                ["Overrides:analysis_operations:query_points_to_objects"] = "deny"
            })
        })
        {
            var listed = Discovery(policy).CreateCapabilities(includeDenied: true).Operations
                .ToDictionary(operation => operation.OperationId, StringComparer.Ordinal);
            var checkedOptions = 0;
            foreach (var operation in policy.Operations)
            {
                var options = OperationRequestClassifier.Default.OptionsFor(operation.OperationId);
                var described = listed[operation.OperationId].RequestOptions.Options;
                Assert.Equal(
                    options.Select(option => option.Field).Order(StringComparer.Ordinal),
                    described.Select(option => option.FieldName).Order(StringComparer.Ordinal));
                foreach (var option in options)
                {
                    var description = Assert.Single(described, candidate => candidate.FieldName == option.Field);
                    var request = EnablingOnly(option);
                    Assert.Equal(
                        [option],
                        OperationRequestClassifier.Default.Classify(option.OperationId, request).EnabledOptions);

                    var decision = policy.EvaluateRequest(option.OperationId, request);

                    var label = $"{policy.Profile.Name} {option.OperationId}.{option.Field}";
                    Assert.True(ServerDiscoveryService.ToAdmission(decision) == description.Admission, label);
                    Assert.True(Equals(decision.Denial?.ToDetails(), description.PolicyDenial), label);
                    Assert.Equal(RequestOptionDescription.ToEffect(option.Effect), description.Effect);
                    Assert.Equal(RequestOptionDescription.ToCondition(option.Condition), description.Condition);
                    Assert.Equal(option.EnablingValues, description.EnablingEnumValues);
                    checkedOptions++;
                }
            }

            Assert.Equal(OperationConditionalOptions.Entries.Count, checkedOptions);
        }
    }

    [Fact]
    public void OmissionAgreesWithTheClassifierAndTheRequestMapping()
    {
        var activates = new List<string>();
        var required = new List<string>();
        foreach (var entry in OperationConditionalOptions.Entries)
        {
            var builder = OperationConditionalOptionTests.OperationBuilder.For(entry.OperationId);
            var request = OperationConditionalOptionTests.RequestPopulator.Create(builder.Request);
            builder.Request.FindFieldByName(entry.Field).Accessor.Clear(request);
            var omission = RequestOptionDescription.ToOmission(entry);
            var label = $"{entry.OperationId}.{entry.Field}";

            Assert.Equal(omission == Api.OperationRequestOptionOmission.Activates,
                OperationRequestClassifier.IsEnabled(entry, request));
            if (omission == Api.OperationRequestOptionOmission.Required)
            {
                Assert.Throws<RequestFieldViolation>(() => builder.Create(request));
                required.Add(label);
            }
            else
            {
                Assert.NotNull(builder.Create(request));
                if (omission == Api.OperationRequestOptionOmission.Activates)
                {
                    activates.Add(label);
                }
            }
        }

        Assert.Equal(["view_control.set_point_of_view_from_instrument_updates.display_view_control"], activates);
        Assert.Equal(
            [
                "instrument_operations.locate_instruments_usmn.show_usmn_dialog",
                "utility_operations.set_interaction_mode.sa_interaction_mode"
            ],
            required.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void AnUnknownEnumNumberTurnsAWhenOneOfOptionOn()
    {
        foreach (var entry in OperationConditionalOptions.Entries
            .Where(entry => entry.Condition == OperationOptionCondition.WhenOneOf))
        {
            var builder = OperationConditionalOptionTests.OperationBuilder.For(entry.OperationId);
            var request = OperationConditionalOptionTests.RequestPopulator.Create(builder.Request);
            var field = builder.Request.FindFieldByName(entry.Field);
            field.Accessor.SetValue(request, Enum.ToObject(field.EnumType.ClrType, 9999));

            Assert.True(OperationRequestClassifier.IsEnabled(entry, request), entry.OperationId);
            var description = RequestOptionDescription.Describe(entry, Policy().EvaluateRequest(entry.OperationId,
                EnablingOnly(entry)));
            Assert.Equal(Api.OperationRequestOptionCondition.WhenOneOf, description.Condition);
            Assert.Equal(entry.EnablingValues, description.EnablingEnumValues);
            Assert.All(description.EnablingEnumValues, name => Assert.NotNull(field.EnumType.FindValueByName(name)));
        }
    }

    [Fact]
    public async Task OlderSchemaReadsTheDeniedCallWithItsKnownFieldsIntact()
    {
        var dispatcher = new RejectingDispatcher();
        var executor = new PolicyEnforcingWorkerCommandExecutor(dispatcher, dispatcher, Policy(),
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance));
        var host = await GrpcTestHost.StartAsync<AnalysisOperationsService>(executor).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            new Api.AnalysisOperations.AnalysisOperationsClient(host.Channel)
                .QueryPointsToObjectsAsync(new Api.QueryPointsToObjectsRequest { ShowResultsDialog = true })
                .ResponseAsync)
            .ConfigureAwait(true);
        var bytes = Assert.Single(exception.Trailers,
            entry => entry.Key == GrpcOperationOutcomeMapper.ErrorTrailerName).ValueBytes;

        var (known, unknown) = ReadWithOlderSchema(bytes, OperationErrorFieldsOfServer0100);
        var expected = Api.OperationError.Parser.ParseFrom(bytes);
        expected.PolicyDenial = null;
        Assert.Equal(expected, Api.OperationError.Parser.ParseFrom(known));
        Assert.Equal([11], unknown);
        Assert.Equal(Api.OperationFailureKind.PolicyDenied, expected.Kind);
    }

    [Fact]
    public async Task OlderSchemaReadsACallerDenialWithItsKnownFieldsIntact()
    {
        // The server admits the dialog; the caller's restriction (#443) denies it.
        var dispatcher = new RejectingDispatcher();
        var executor = new PolicyEnforcingWorkerCommandExecutor(dispatcher, dispatcher,
            Policy(settings: new() { ["Flags:interactive_ui"] = "allow" }),
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance));
        var host = await GrpcTestHost.StartAsync<AnalysisOperationsService>(executor).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            new Api.AnalysisOperations.AnalysisOperationsClient(host.Channel)
                .QueryPointsToObjectsAsync(new Api.QueryPointsToObjectsRequest { ShowResultsDialog = true },
                    new Metadata { { OperationCallerRestriction.HeaderName, "profile=read-only; deny=interactive_ui" } })
                .ResponseAsync)
            .ConfigureAwait(true);

        Assert.Equal(0, dispatcher.DispatchCount);
        var details = AssertDeniedCall(exception, QueryPointsToObjects, "caller-restriction-option-denied");
        Assert.Equal(new Api.PolicyDenialDetails
        {
            Rule = Api.PolicyDenialRule.CallerFlag,
            CallerDeniedFlag = "interactive_ui",
            RequestOptionField = "show_results_dialog"
        }, details);

        // The caller detail travels inside field 11, and a denial carries no field 12.
        var bytes = Assert.Single(exception.Trailers,
            entry => entry.Key == GrpcOperationOutcomeMapper.ErrorTrailerName).ValueBytes;
        var (known, unknown) = ReadWithOlderSchema(bytes, OperationErrorFieldsOfServer0100);
        var expected = Api.OperationError.Parser.ParseFrom(bytes);
        Assert.Null(expected.RequestValidation);
        expected.PolicyDenial = null;
        Assert.Equal(expected, Api.OperationError.Parser.ParseFrom(known));
        Assert.Equal([11], unknown);
    }

    [Fact]
    public void OlderSchemaReadsEveryCapabilityWithItsKnownFieldsIntact()
    {
        var all = Discovery(Policy()).CreateCapabilities(includeDenied: true);
        var decoded = Api.ListCapabilitiesResponse.Parser.ParseFrom(all.ToByteArray());
        Assert.Equal(all, decoded);

        foreach (var capability in all.Operations)
        {
            var (known, unknown) = ReadWithOlderSchema(capability.ToByteArray(), OperationCapabilityFieldsOfServer0100);
            var expected = capability.Clone();
            expected.PolicyDenial = null;
            expected.RequestOptions = null;
            // mp_step (14) is unknown to Server 0.10.0 and set on every capability.
            Assert.NotEmpty(capability.MpStep);
            expected.MpStep = string.Empty;

            int[] unknownFields = capability.PolicyDenial is null ? [13, 14] : [12, 13, 14];
            Assert.Equal(expected, Api.OperationCapability.Parser.ParseFrom(known));
            Assert.Equal(unknownFields, unknown);
        }
    }

    private static Task<Api.PolicyDenialDetails> DenyFileOperationAsync(
        OperationPolicy policy,
        string operationId,
        string diagnosticCode,
        Func<Api.FileOperations.FileOperationsClient, Task> call) =>
        DenyAsync<FileOperationsService>(policy, operationId, diagnosticCode,
            channel => call(new Api.FileOperations.FileOperationsClient(channel)));

    /// <summary>
    /// Calls one operation through a real loopback gRPC server whose worker is the
    /// production policy enforcer, and returns the parsed trailer's denial detail.
    /// </summary>
    private static async Task<Api.PolicyDenialDetails> DenyAsync<TService>(
        OperationPolicy policy,
        string operationId,
        string diagnosticCode,
        Func<GrpcChannel, Task> call)
        where TService : class
    {
        var dispatcher = new RejectingDispatcher();
        var executor = new PolicyEnforcingWorkerCommandExecutor(dispatcher, dispatcher, policy,
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance));
        var host = await GrpcTestHost.StartAsync<TService>(executor).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);

        var exception = await Assert.ThrowsAsync<RpcException>(() => call(host.Channel)).ConfigureAwait(true);

        Assert.Equal(0, dispatcher.DispatchCount);
        return AssertDeniedCall(exception, operationId, diagnosticCode);
    }

    // Everything but the new detail is what Server 0.10.0 sent for the same denial.
    private static Api.PolicyDenialDetails AssertDeniedCall(
        RpcException exception,
        string operationId,
        string diagnosticCode)
    {
        Assert.Equal(StatusCode.PermissionDenied, exception.StatusCode);
        Assert.Equal(DeniedStatusText, exception.Status.Detail);
        var bytes = Assert.Single(exception.Trailers,
            entry => entry.Key == GrpcOperationOutcomeMapper.ErrorTrailerName).ValueBytes;
        var error = Api.OperationError.Parser.ParseFrom(bytes);
        Assert.Equal(operationId, error.OperationId);
        Assert.Equal(Api.OperationFailureKind.PolicyDenied, error.Kind);
        Assert.Equal(diagnosticCode, error.DiagnosticCode);
        Assert.Equal(Api.ExecutionDisposition.NotStarted, error.ExecutionDisposition);
        Assert.Equal(Api.RecoveryGuidance.None, error.RecoveryGuidance);
        Assert.Equal(Api.ReplayGuidance.DoNotReplay, error.ReplayGuidance);
        Assert.Equal(
            SpatialAnalyzerApi.Operations.Single(operation => operation.OperationId == operationId).ReplaySafety,
            error.ReplaySafety);
        Assert.True(error.HasWorkerGeneration);
        Assert.Equal(0, error.WorkerGeneration);
        Assert.Null(error.MpExecution);

        // The earlier fields keep their exact bytes; the detail follows as field 11.
        var details = Assert.IsType<Api.PolicyDenialDetails>(error.PolicyDenial);
        var earlier = error.Clone();
        earlier.PolicyDenial = null;
        var prefix = earlier.ToByteArray();
        Assert.Equal(prefix, bytes[..prefix.Length]);
        using var detail = new CodedInputStream(bytes[prefix.Length..]);
        Assert.Equal(
            WireFormat.MakeTag(Api.OperationError.PolicyDenialFieldNumber, WireFormat.WireType.LengthDelimited),
            detail.ReadTag());
        return details;
    }

    private static void AssertDetails(
        Api.PolicyDenialDetails? details,
        Api.PolicyDenialRule rule,
        string? flag = null,
        string? profile = null,
        string? field = null)
    {
        Assert.NotNull(details);
        Assert.Equal(rule, details.Rule);
        Assert.Equal(flag is not null, details.HasDeniedFlag);
        Assert.Equal(flag ?? string.Empty, details.DeniedFlag);
        Assert.Equal(profile is not null, details.HasAdmissionProfile);
        Assert.Equal(profile ?? string.Empty, details.AdmissionProfile);
        Assert.Equal(field is not null, details.HasRequestOptionField);
        Assert.Equal(field ?? string.Empty, details.RequestOptionField);
    }

    /// <summary>
    /// A populated request that turns on <paramref name="option"/> and leaves every
    /// other reviewed option of its operation explicitly inactive.
    /// </summary>
    private static IMessage EnablingOnly(OperationConditionalOption option)
    {
        var builder = OperationConditionalOptionTests.OperationBuilder.For(option.OperationId);
        var request = OperationConditionalOptionTests.RequestPopulator.Create(builder.Request);
        foreach (var other in OperationRequestClassifier.Default.OptionsFor(option.OperationId))
        {
            Set(builder.Request.FindFieldByName(other.Field), request, other, enabled: false);
        }

        Set(builder.Request.FindFieldByName(option.Field), request, option, enabled: true);
        return request;
    }

    private static void Set(FieldDescriptor field, IMessage request, OperationConditionalOption option, bool enabled)
    {
        switch (option.Condition)
        {
            case OperationOptionCondition.WhenTrue:
                field.Accessor.SetValue(request, enabled);
                break;
            case OperationOptionCondition.WhenFalse:
                field.Accessor.SetValue(request, !enabled);
                break;
            case OperationOptionCondition.WhenPresent when enabled:
                field.Accessor.SetValue(request, OperationConditionalOptionTests.RequestPopulator.Create(field.MessageType));
                break;
            case OperationOptionCondition.WhenPresent:
                field.Accessor.Clear(request);
                break;
            case OperationOptionCondition.WhenNonEmpty:
                field.Accessor.SetValue(request, enabled ? "Camera View" : string.Empty);
                break;
            case OperationOptionCondition.WhenOneOf:
                var value = field.EnumType.Values.First(candidate =>
                    option.EnablingValues.Contains(candidate.Name, StringComparer.Ordinal) == enabled);
                field.Accessor.SetValue(request, Enum.ToObject(field.EnumType.ClrType, value.Number));
                break;
            default:
                throw new InvalidOperationException($"Unreviewed condition for {option.OperationId}.{option.Field}.");
        }
    }

    /// <summary>
    /// Reads wire bytes the way code generated from an older schema does: it parses
    /// the fields that schema knows and keeps every other field as an unknown field.
    /// Returns the known fields' bytes and the unknown field numbers.
    /// </summary>
    private static (byte[] Known, int[] Unknown) ReadWithOlderSchema(byte[] bytes, int[] knownFields)
    {
        var known = new List<byte>();
        var unknownNumbers = new List<int>();
        UnknownFieldSet? unknown = null;
        using var input = new CodedInputStream(bytes);
        while (true)
        {
            var start = (int)input.Position;
            var tag = input.ReadTag();
            if (tag == 0)
            {
                break;
            }

            var number = WireFormat.GetTagFieldNumber(tag);
            if (knownFields.Contains(number))
            {
                input.SkipLastField();
                known.AddRange(bytes[start..(int)input.Position]);
            }
            else
            {
                unknown = UnknownFieldSet.MergeFieldFrom(unknown, input);
                unknownNumbers.Add(number);
            }
        }

        Assert.Equal(unknown is null ? 0 : bytes.Length - known.Count, unknown?.CalculateSize() ?? 0);
        return ([.. known], [.. unknownNumbers]);
    }

    private static Api.OperationError ParseError(RpcException exception) =>
        Api.OperationError.Parser.ParseFrom(Assert.Single(exception.Trailers,
            entry => entry.Key == GrpcOperationOutcomeMapper.ErrorTrailerName).ValueBytes);

    private static ServerDiscoveryService Discovery(OperationPolicy policy) =>
        new(new RejectingDispatcher(), new BuildIdentityProvider(), policy);

    private static OperationPolicy Policy(
        string profile = "standard",
        Dictionary<string, string?>? settings = null,
        Func<string, OperationClassificationLookup>? classify = null,
        OperationRequestClassifier? requestClassifier = null) =>
        OperationPolicyTests.CreatePolicy(
            profile: profile, settings: settings, classify: classify, requestClassifier: requestClassifier);

    /// <summary>A dispatcher that a denied request must never reach.</summary>
    private sealed class RejectingDispatcher : IWorkerCommandDispatcher, IWorkerStatusProvider
    {
        public int DispatchCount { get; private set; }

        public WorkerLifecycleSnapshot Current { get; } = new(
            WorkerLifecycleState.Stopped,
            Generation: 0,
            ProcessId: null,
            RecoveryCount: 0,
            WorkerTerminationKind.None,
            "policy-denial-test",
            Connection: null,
            DateTimeOffset.UnixEpoch);

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerCommandSubmission submission,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            DispatchCount++;
            throw new InvalidOperationException("A denied request reached the worker.");
        }
    }

    private sealed class RejectingProcessFactory : IWorkerProcessFactory
    {
        public int StartCount { get; private set; }

        public ValueTask<IWorkerProcess> StartAsync(
            int generation,
            CancellationToken cancellationToken = default)
        {
            StartCount++;
            throw new InvalidOperationException("The policy denial test must not start a worker.");
        }
    }
}
