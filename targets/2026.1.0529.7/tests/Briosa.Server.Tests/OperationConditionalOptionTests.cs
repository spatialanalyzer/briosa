using System.Reflection;
using Briosa.Server.Operations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

/// <summary>
/// The request-level guard of #293: reviewed caller options that open operator
/// UI or leave device work running change a request's effective classification
/// before mapping or dispatch.
/// </summary>
public sealed class OperationConditionalOptionTests
{
    private const string MoveToFrame = "robot_operations.move_robot_machine_to_frame";
    private const string QueryPointsToObjects = "analysis_operations.query_points_to_objects";

    private static readonly Api.CollectionMachineId Machine = new() { CollectionName = "Robots", MachineId = 12 };
    private static readonly Api.CollectionObjectName Frame = new() { CollectionName = "Frames", ObjectName = "Approach" };

    [Fact]
    public void EveryEntryIsCompleteAndNamesARegisteredRequestFieldWithPresence()
    {
        var operations = SpatialAnalyzerApi.Operations.ToDictionary(
            operation => operation.OperationId, StringComparer.Ordinal);
        Assert.NotEmpty(OperationConditionalOptions.Entries);
        Assert.Equal(
            OperationConditionalOptions.Entries.Count,
            OperationConditionalOptions.Entries.Select(entry => (entry.OperationId, entry.Field)).Distinct().Count());

        foreach (var entry in OperationConditionalOptions.Entries)
        {
            Assert.True(entry.IsComplete, entry.OperationId);
            Assert.True(operations.ContainsKey(entry.OperationId), entry.OperationId);
            var row = OperationClassification.Find(entry.OperationId).Row!;
            if (entry.Effect == OperationOptionEffect.BackgroundWork)
            {
                // Rows stay admissible and are classified by their waiting behavior.
                Assert.Equal(OperationIsolationClass.Admissible, row.Isolation);
            }

            var field = OperationBuilder.For(entry.OperationId).Request.FindFieldByName(entry.Field);
            Assert.NotNull(field);
            Assert.True(field.HasPresence, $"{entry.OperationId}.{entry.Field}");
            Assert.False(field.IsRepeated);
            var expectedType = entry.Condition switch
            {
                OperationOptionCondition.WhenTrue or OperationOptionCondition.WhenFalse => FieldType.Bool,
                OperationOptionCondition.WhenPresent => FieldType.Message,
                OperationOptionCondition.WhenNonEmpty => FieldType.String,
                OperationOptionCondition.WhenOneOf => FieldType.Enum,
                _ => throw new InvalidOperationException()
            };
            Assert.Equal(expectedType, field.FieldType);
            Assert.All(entry.EnablingValues, value => Assert.NotNull(field.EnumType.FindValueByName(value)));
        }
    }

    /// <summary>
    /// For every entry, builds the real MP command for an absent, an enabling, and
    /// a non-enabling request value, and requires the classifier's verdict to match
    /// what the mapped MP argument actually does. The guard cannot drift from the
    /// mapping.
    /// </summary>
    [Fact]
    public void ClassifierAgreesWithTheMpArgumentEachRequestSends()
    {
        foreach (var entry in OperationConditionalOptions.Entries)
        {
            var checkedCases = 0;
            var builder = OperationBuilder.For(entry.OperationId);
            var field = builder.Request.FindFieldByName(entry.Field);
            foreach (var (name, value) in Cases(entry, field))
            {
                var request = RequestPopulator.Create(builder.Request);
                if (value is null)
                {
                    field.Accessor.Clear(request);
                }
                else
                {
                    field.Accessor.SetValue(request, value);
                }

                var label = $"{entry.OperationId}.{entry.Field} ({name})";
                var classified = OperationRequestClassifier.IsEnabled(entry, request);
                Assert.True(classified.HasValue, label);
                if (value is null && entry.Absence == OperationOptionAbsence.Rejected)
                {
                    Assert.Throws<ArgumentException>(() => builder.Create(request));
                    Assert.False(classified.Value, label);
                    checkedCases++;
                    continue;
                }

                var command = builder.Create(request);
                var sent = command.InputArguments.Where(argument => argument.Name == entry.MpArgument).ToArray();
                Assert.True(sent.Length <= 1, label);
                Assert.True(
                    sent.Length == 1 || entry.Condition == OperationOptionCondition.WhenPresent ||
                    (value is null && entry.Absence == OperationOptionAbsence.SendsNothing),
                    $"{label}: MP argument '{entry.MpArgument}' was not sent.");
                Assert.Equal(EnabledBySentArgument(entry, sent.SingleOrDefault()), classified.Value);
                if (value is null)
                {
                    AssertAbsentValueIsSent(entry, sent.SingleOrDefault(), label);
                }

                checkedCases++;
            }

            // Absent plus at least one explicit value for every entry.
            Assert.True(checkedCases >= 2, entry.OperationId);
        }
    }

    [Fact]
    public void BreakingReleaseDefaultsDoNotEnableTheirOption()
    {
        var flips = OperationConditionalOptions.Entries
            .Where(entry => entry.Trigger == OperationOptionTrigger.DefaultFlip)
            .Select(entry => $"{entry.OperationId}.{entry.Field}")
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [
                "file_operations.direct_cad_access.prompt_on_missing_components",
                "instrument_operations.auto_measure_points.auto_start",
                "instrument_operations.auto_measure_specified_geometry.wait_for_complete",
                "instrument_operations.configure_and_measure.measure_immediately",
                "instrument_operations.measure_existing_single_point.measure_immediately",
                "instrument_operations.measure_existing_single_point_and_compare.measure_immediately",
                "instrument_operations.measure_single_point_here.measure_immediately",
                "robot_operations.move_robot_machine_to_frame.acknowledge_arrival",
                "robot_operations.move_robot_machine_to_named_destination.acknowledge_arrival"
            ],
            flips);
        foreach (var entry in OperationConditionalOptions.Entries.Where(entry => entry.Trigger == OperationOptionTrigger.DefaultFlip))
        {
            // The waiting, immediate, or non-prompting value is the default.
            Assert.Equal(
                entry.Condition == OperationOptionCondition.WhenFalse
                    ? OperationOptionAbsence.SendsTrue
                    : OperationOptionAbsence.SendsFalse,
                entry.Absence);
            var builder = OperationBuilder.For(entry.OperationId);
            var request = RequestPopulator.Create(builder.Request);
            builder.Request.FindFieldByName(entry.Field).Accessor.Clear(request);
            var sent = Assert.Single(builder.Create(request).InputArguments, argument => argument.Name == entry.MpArgument);
            Assert.False(EnabledBySentArgument(entry, sent), entry.OperationId);
            Assert.False(OperationRequestClassifier.IsEnabled(entry, request), entry.OperationId);
        }
    }

    // Review finding 1: non-waiting robot motion reached dispatch under `device`.
    [Fact]
    public async Task DeviceProfileDeniesMoveToFrameThatDoesNotAcknowledgeArrival()
    {
        foreach (var settings in new Dictionary<string, string?>[]
        {
            new(),
            new() { ["Overrides:robot_operations:move_robot_machine_to_frame"] = "allow" },
            new() { ["Flags:interactive_ui"] = "allow" }
        })
        {
            var harness = new Harness(OperationPolicyTests.CreatePolicy(profile: "device", settings: settings));
            var request = new Api.MoveRobotMachineToFrameRequest
            {
                MachineId = Machine, DestinationFrame = Frame, AcknowledgeArrival = false
            };

            var outcome = await harness.SubmitAsync(MoveToFrame, request).ConfigureAwait(true);

            Assert.Equal(WorkerExecutionStatus.PolicyDenied, outcome.Status);
            Assert.Equal(WorkerExecutionDisposition.NotStarted, outcome.ExecutionDisposition);
            Assert.Equal("operation-option-isolation-unsupported", outcome.DiagnosticCode);
            Assert.Null(harness.Dispatched);
            var decision = harness.Policy.EvaluateRequest(MoveToFrame, request);
            Assert.Equal("option.acknowledge_arrival", decision.PolicyRule);
        }
    }

    [Fact]
    public async Task DeviceProfileAdmitsMoveToFrameWhenAcknowledgeArrivalIsOmittedAndSendsIt()
    {
        var harness = new Harness(OperationPolicyTests.CreatePolicy(profile: "device"));
        var request = new Api.MoveRobotMachineToFrameRequest { MachineId = Machine, DestinationFrame = Frame };

        await harness.SubmitAsync(MoveToFrame, request).ConfigureAwait(true);

        var submission = Assert.IsType<WorkerCommandSubmission>(harness.Dispatched);
        Assert.Equal(OperationDurationClass.LongRunning, submission.DurationClass);
        var acknowledge = Assert.Single(submission.CreateCommand().InputArguments,
            argument => argument.Name == "Acknowledge Arrival");
        Assert.True(acknowledge.RequireValue<WorkerBooleanValue>().Value);

        var explicitWait = new Harness(OperationPolicyTests.CreatePolicy(profile: "device"));
        request.AcknowledgeArrival = true;
        await explicitWait.SubmitAsync(MoveToFrame, request).ConfigureAwait(true);
        Assert.NotNull(explicitWait.Dispatched);
    }

    // Review finding 2: an optional dialog bypassed an explicit interactive denial.
    [Theory]
    [InlineData("read-only")]
    [InlineData("standard")]
    [InlineData("full")]
    public async Task ExplicitInteractiveDenialRejectsAnOptedInResultsDialog(string profile)
    {
        var harness = new Harness(OperationPolicyTests.CreatePolicy(
            profile: profile, settings: new() { ["Flags:interactive_ui"] = "deny" }));
        var request = new Api.QueryPointsToObjectsRequest { ShowResultsDialog = true };

        var outcome = await harness.SubmitAsync(QueryPointsToObjects, request).ConfigureAwait(true);

        Assert.Equal(WorkerExecutionStatus.PolicyDenied, outcome.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, outcome.ExecutionDisposition);
        Assert.Equal("operation-option-denied", outcome.DiagnosticCode);
        Assert.Null(harness.Dispatched);
        Assert.Equal(
            "option.show_results_dialog/flag.interactive_ui",
            harness.Policy.EvaluateRequest(QueryPointsToObjects, request).PolicyRule);
    }

    [Fact]
    public async Task ProfilesWithoutTheInteractiveOptInRejectAnOptedInResultsDialog()
    {
        var harness = new Harness(OperationPolicyTests.CreatePolicy(profile: "standard"));
        var request = new Api.QueryPointsToObjectsRequest { ShowResultsDialog = true };

        var outcome = await harness.SubmitAsync(QueryPointsToObjects, request).ConfigureAwait(true);

        Assert.Equal("operation-option-denied", outcome.DiagnosticCode);
        Assert.Null(harness.Dispatched);
        Assert.Equal(
            "option.show_results_dialog/profile.standard",
            harness.Policy.EvaluateRequest(QueryPointsToObjects, request).PolicyRule);
        // Discovery still advertises the operation: its reviewed row is admitted.
        Assert.Contains(harness.Policy.AllowedOperations, operation => operation.OperationId == QueryPointsToObjects);
    }

    [Fact]
    public async Task InteractiveFlagOptInAdmitsAResultsDialogWithTheInteractiveDuration()
    {
        var harness = new Harness(OperationPolicyTests.CreatePolicy(
            profile: "standard", settings: new() { ["Flags:interactive_ui"] = "allow" }));
        var request = new Api.QueryPointsToObjectsRequest { ShowResultsDialog = true };

        await harness.SubmitAsync(QueryPointsToObjects, request).ConfigureAwait(true);

        var submission = Assert.IsType<WorkerCommandSubmission>(harness.Dispatched);
        Assert.Equal(OperationDurationClass.Interactive, submission.DurationClass);
        Assert.Equal(
            "option.show_results_dialog/profile.standard",
            harness.Policy.EvaluateRequest(QueryPointsToObjects, request).PolicyRule);
    }

    [Fact]
    public async Task PerOperationAllowAdmitsAResultsDialogWithTheInteractiveDuration()
    {
        var harness = new Harness(OperationPolicyTests.CreatePolicy(
            profile: "read-only",
            settings: new()
            {
                ["Flags:interactive_ui"] = "deny",
                ["Overrides:analysis_operations:query_points_to_objects"] = "allow"
            }));
        var request = new Api.QueryPointsToObjectsRequest { ShowResultsDialog = true };

        await harness.SubmitAsync(QueryPointsToObjects, request).ConfigureAwait(true);

        var submission = Assert.IsType<WorkerCommandSubmission>(harness.Dispatched);
        Assert.Equal(OperationDurationClass.Interactive, submission.DurationClass);
        Assert.Equal(
            "option.show_results_dialog/override",
            harness.Policy.EvaluateRequest(QueryPointsToObjects, request).PolicyRule);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    public async Task DefaultRequestOfAnOperationWithOptInUiKeepsItsTableDuration(bool? showResultsDialog)
    {
        var harness = new Harness(OperationPolicyTests.CreatePolicy(
            profile: "standard", settings: new() { ["Flags:interactive_ui"] = "deny" }));
        var request = new Api.QueryPointsToObjectsRequest();
        if (showResultsDialog is { } value)
        {
            request.ShowResultsDialog = value;
        }

        await harness.SubmitAsync(QueryPointsToObjects, request).ConfigureAwait(true);

        var submission = Assert.IsType<WorkerCommandSubmission>(harness.Dispatched);
        Assert.Equal(OperationClassification.Find(QueryPointsToObjects).Row!.Duration, submission.DurationClass);
        Assert.Equal(OperationDurationClass.Quick, submission.DurationClass);
        Assert.Equal("profile.standard", harness.Policy.EvaluateRequest(QueryPointsToObjects, request).PolicyRule);
    }

    [Fact]
    public void DirectCadAccessPromptsOnlyWhenTheCallerOptsIn()
    {
        // Maintainer decision 2026-10-05: prompt_on_missing_components defaults to false.
        const string directCadAccess = "file_operations.direct_cad_access";
        var builder = OperationBuilder.For(directCadAccess);
        var omitted = (Api.DirectCadAccessRequest)RequestPopulator.Create(builder.Request);
        omitted.ClearPromptOnMissingComponents();
        var prompt = Assert.Single(builder.Create(omitted).InputArguments,
            argument => argument.Name == "Prompt on Missing Components");
        Assert.False(prompt.RequireValue<WorkerBooleanValue>().Value);

        var standard = OperationPolicyTests.CreatePolicy(profile: "standard");
        var admitted = standard.EvaluateRequest(directCadAccess, new Api.DirectCadAccessRequest());
        Assert.Equal(OperationPolicyDecisionKind.Allowed, admitted.Kind);
        Assert.Equal(OperationClassification.Find(directCadAccess).Row!.Duration, admitted.DurationClass);
        Assert.Equal("profile.standard", admitted.PolicyRule);

        var optedIn = new Api.DirectCadAccessRequest { PromptOnMissingComponents = true };
        var denied = standard.EvaluateRequest(directCadAccess, optedIn);
        Assert.Equal("operation-option-denied", denied.DiagnosticCode);
        Assert.Equal("option.prompt_on_missing_components/profile.standard", denied.PolicyRule);
        var interactive = OperationPolicyTests.CreatePolicy(
            profile: "standard", settings: new() { ["Flags:interactive_ui"] = "allow" })
            .EvaluateRequest(directCadAccess, optedIn);
        Assert.Equal(OperationPolicyDecisionKind.Allowed, interactive.Kind);
        Assert.Equal(OperationDurationClass.Interactive, interactive.DurationClass);
    }

    [Fact]
    public async Task EveryAdmittedRequestCarriesItsEffectiveDurationClass()
    {
        var harness = new Harness(OperationPolicyTests.CreatePolicy(profile: "standard"));

        await harness.SubmitAsync("file_operations.get_working_directory", new Api.GetWorkingDirectoryRequest())
            .ConfigureAwait(true);

        Assert.Equal(OperationDurationClass.Quick, harness.Dispatched!.DurationClass);
    }

    [Fact]
    public async Task ConditionalOperationsFailClosedWithoutATypedRequest()
    {
        var harness = new Harness(OperationPolicyTests.CreatePolicy(profile: "full",
            settings: new() { ["Flags:interactive_ui"] = "allow" }));
        var command = OperationBuilder.For(QueryPointsToObjects).Create(RequestPopulator.Create<Api.QueryPointsToObjectsRequest>());

        var outcome = await harness.Executor.ExecuteAsync(command, Guid.NewGuid()).ConfigureAwait(true);

        Assert.Equal(WorkerExecutionStatus.PolicyDenied, outcome.Status);
        Assert.Equal("operation-request-unclassified", outcome.DiagnosticCode);
        Assert.Null(harness.Dispatched);
    }

    [Fact]
    public void UnreadableOptionFieldsFailClosed()
    {
        var entry = new OperationConditionalOption(
            QueryPointsToObjects, "no_such_field", "Show Results Dialog?",
            OperationOptionEffect.InteractiveUi, OperationOptionTrigger.CallerOption,
            OperationOptionCondition.WhenTrue, OperationOptionAbsence.SendsFalse);
        var wrongType = entry with { Field = "rms_tolerance" };
        foreach (var option in new[] { entry, wrongType })
        {
            var policy = OperationPolicyTests.CreatePolicy(
                profile: "full",
                settings: new() { ["Overrides:analysis_operations:query_points_to_objects"] = "allow" },
                requestClassifier: new OperationRequestClassifier([option]));

            var decision = policy.EvaluateRequest(QueryPointsToObjects, new Api.QueryPointsToObjectsRequest());

            Assert.Equal(OperationPolicyDecisionKind.Denied, decision.Kind);
            Assert.Equal("operation-request-unclassified", decision.DiagnosticCode);
        }
    }

    [Fact]
    public void IncompleteOrDuplicateEntriesFailStartup()
    {
        var entry = OperationConditionalOptions.Entries[0];
        Assert.Throws<InvalidOperationException>(() => new OperationRequestClassifier([entry, entry]));
        Assert.Throws<InvalidOperationException>(() => new OperationRequestClassifier(
            [entry with { Absence = OperationOptionAbsence.Unspecified }]));
        Assert.Throws<InvalidOperationException>(() => new OperationRequestClassifier(
            [entry with { Condition = OperationOptionCondition.WhenOneOf }]));
    }

    [Fact]
    public void PolicyFingerprintCoversTheConditionalOptionTable()
    {
        var baseline = OperationPolicyTests.CreatePolicy().Fingerprint;
        var changed = OperationPolicyTests.CreatePolicy(requestClassifier: new OperationRequestClassifier(
            OperationConditionalOptions.Entries.Skip(1).ToArray())).Fingerprint;

        Assert.NotEqual(baseline, changed);
    }

    private static IEnumerable<(string Name, object? Value)> Cases(OperationConditionalOption entry, FieldDescriptor field)
    {
        yield return ("absent", null);
        switch (entry.Condition)
        {
            case OperationOptionCondition.WhenTrue:
            case OperationOptionCondition.WhenFalse:
                yield return ("true", true);
                yield return ("false", false);
                break;
            case OperationOptionCondition.WhenPresent:
                yield return ("file", new Api.FileReference { Path = @"C:\fixture\prompt.html" });
                break;
            case OperationOptionCondition.WhenNonEmpty:
                yield return ("text", "Camera View");
                yield return ("empty", string.Empty);
                break;
            case OperationOptionCondition.WhenOneOf:
                foreach (var value in field.EnumType.Values.Where(value => value.Number != 0))
                {
                    yield return (value.Name, Enum.ToObject(field.EnumType.ClrType, value.Number));
                }

                break;
        }
    }

    private static bool EnabledBySentArgument(OperationConditionalOption entry, WorkerMpInputArgument? sent) =>
        entry.Condition switch
        {
            OperationOptionCondition.WhenTrue => sent!.RequireValue<WorkerBooleanValue>().Value,
            OperationOptionCondition.WhenFalse => !sent!.RequireValue<WorkerBooleanValue>().Value,
            OperationOptionCondition.WhenPresent =>
                sent is not null && sent.RequireValue<WorkerFileReferenceValue>().Path.Length > 0,
            OperationOptionCondition.WhenNonEmpty => sent!.RequireValue<WorkerTextValue>().Value.Length > 0,
            OperationOptionCondition.WhenOneOf => entry.EnablingValues
                .Select(value => ClrEnumName(entry, value))
                .Contains(ChoiceName(sent!), StringComparer.OrdinalIgnoreCase),
            _ => throw new InvalidOperationException()
        };

    private static void AssertAbsentValueIsSent(OperationConditionalOption entry, WorkerMpInputArgument? sent, string label)
    {
        switch (entry.Absence)
        {
            case OperationOptionAbsence.SendsTrue:
                Assert.True(sent!.RequireValue<WorkerBooleanValue>().Value, label);
                break;
            case OperationOptionAbsence.SendsFalse:
                Assert.False(sent!.RequireValue<WorkerBooleanValue>().Value, label);
                break;
            case OperationOptionAbsence.SendsNothing:
                Assert.True(sent is null || sent.Value is WorkerTextValue { Value.Length: 0 }, label);
                break;
            default:
                Assert.Fail(label);
                break;
        }
    }

    // The worker choice for a protobuf enum value: SHOW_USMN_DIALOG_YES maps to Yes.
    private static string ClrEnumName(OperationConditionalOption entry, string protobufName)
    {
        var field = OperationBuilder.For(entry.OperationId).Request.FindFieldByName(entry.Field);
        var number = field.EnumType.FindValueByName(protobufName).Number;
        return Enum.GetName(field.EnumType.ClrType, number)!;
    }

    private static string ChoiceName(WorkerMpInputArgument sent) =>
        sent.Value.GetType().GetProperty("Value")!.GetValue(sent.Value)!.ToString()!;

    /// <summary>A policy enforcer whose dispatcher only records what it was given.</summary>
    private sealed class Harness : IWorkerCommandDispatcher, IWorkerStatusProvider
    {
        public Harness(OperationPolicy policy)
        {
            Policy = policy;
            Executor = new PolicyEnforcingWorkerCommandExecutor(this, this, policy,
                new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance));
        }

        public OperationPolicy Policy { get; }

        public PolicyEnforcingWorkerCommandExecutor Executor { get; }

        public WorkerCommandSubmission? Dispatched { get; private set; }

        public WorkerLifecycleSnapshot Current { get; } = new(
            WorkerLifecycleState.Stopped,
            Generation: 0,
            ProcessId: null,
            RecoveryCount: 0,
            WorkerTerminationKind.None,
            "policy-test",
            Connection: null,
            DateTimeOffset.UnixEpoch);

        public Task<WorkerExecutionOutcome> SubmitAsync(string operationId, IMessage request) =>
            Executor.ExecuteAsync(
                new WorkerCommandSubmission(operationId,
                    () => OperationBuilder.For(operationId).Create(request), Request: request),
                Guid.NewGuid());

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerCommandSubmission submission,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            Dispatched = submission;
            return Task.FromResult(new WorkerExecutionOutcome(
                WorkerExecutionStatus.Overloaded, WorkerExecutionDisposition.NotStarted,
                Execution: null, Connection: null, "recorded", Generation: 0, correlationId));
        }
    }

    /// <summary>Finds an operation's typed request and its shipped command builder.</summary>
    internal sealed record OperationBuilder(MessageDescriptor Request, Func<IMessage, WorkerMpCommand> Create)
    {
        private static readonly Dictionary<string, OperationBuilder> Builders = typeof(SpatialAnalyzerApi).Assembly
            .GetTypes()
            .Select(type => (
                Descriptor: type.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                    as OperationDescriptor,
                Create: type.GetMethods(BindingFlags.Public | BindingFlags.Static).SingleOrDefault(method =>
                    method.Name == "CreateCommand" && method.GetParameters() is [var parameter] &&
                    typeof(IMessage).IsAssignableFrom(parameter.ParameterType))))
            .Where(candidate => candidate.Descriptor is not null && candidate.Create is not null)
            .ToDictionary(
                candidate => candidate.Descriptor!.OperationId,
                candidate => new OperationBuilder(
                    (MessageDescriptor)candidate.Create!.GetParameters()[0].ParameterType
                        .GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!,
                    request => Invoke(candidate.Create!, request)),
                StringComparer.Ordinal);

        public static OperationBuilder For(string operationId) =>
            Builders.TryGetValue(operationId, out var builder)
                ? builder
                : throw new InvalidOperationException($"No command builder found for '{operationId}'.");

        private static WorkerMpCommand Invoke(MethodInfo create, IMessage request)
        {
            try
            {
                return (WorkerMpCommand)create.Invoke(null, [request])!;
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
    }

    /// <summary>Fills every field of a request with a plausible, valid value.</summary>
    internal static class RequestPopulator
    {
        public static T Create<T>() where T : IMessage, new() =>
            (T)Create(new T().Descriptor);

        public static IMessage Create(MessageDescriptor descriptor, int depth = 0)
        {
            var message = (IMessage)Activator.CreateInstance(descriptor.ClrType)!;
            foreach (var field in descriptor.Fields.InFieldNumberOrder())
            {
                if (field.RealContainingOneof is not null && field.RealContainingOneof.Fields[0] != field)
                {
                    continue;
                }

                var value = Value(field, depth);
                if (value is null)
                {
                    continue;
                }

                if (field.IsRepeated)
                {
                    ((System.Collections.IList)field.Accessor.GetValue(message)).Add(value);
                }
                else
                {
                    field.Accessor.SetValue(message, value);
                }
            }

            return message;
        }

        private static object? Value(FieldDescriptor field, int depth) => field.FieldType switch
        {
            FieldType.Bool => false,
            FieldType.String => Text(field),
            FieldType.Double => 1d,
            FieldType.Float => 1f,
            FieldType.Int32 or FieldType.SInt32 or FieldType.SFixed32 => 1,
            FieldType.UInt32 or FieldType.Fixed32 => 1u,
            FieldType.Int64 or FieldType.SInt64 or FieldType.SFixed64 => 1L,
            FieldType.UInt64 or FieldType.Fixed64 => 1ul,
            FieldType.Enum => Enum.ToObject(field.EnumType.ClrType,
                field.EnumType.Values.First(value => value.Number != 0).Number),
            FieldType.Message when field.MessageType.FullName == Api.FileReference.Descriptor.FullName =>
                new Api.FileReference { Path = @"C:\fixture\input.txt" },
            FieldType.Message when depth < 4 => Create(field.MessageType, depth + 1),
            _ => null
        };

        private static string Text(FieldDescriptor field) =>
            field.Name.Contains("prompt", StringComparison.Ordinal) ? string.Empty : "Fixture";
    }
}
