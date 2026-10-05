using global::Briosa;
using Briosa.Server.Operations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Briosa.Server.Tests;

public sealed class OperationPolicyTests
{
    private const string OperationId = "file_operations.get_working_directory";
    private const string WorkingDirectoryOverride = "Overrides:file_operations:get_working_directory";
    private const string ExclusiveOperationId = "instrument_operations.start_instrument_interface";
    private const string InteractiveOperationId = "construction_operations.make_point_name_runtime_select";
    private const string ExternalReadOperationId = "utility_operations.get_opc_da_tag_value_string";
    private const string FileWriteMutationId = "analysis_operations.best_fit_transformation_group_to_group";
    private const string CodeExecutionOperationId = "mp_subroutines.run_subroutine";
    private const string FixturePendingOperationId = "analysis_operations.get_object_reporting_frame";

    private static readonly string[] RuntimeSelectors =
        [
            "construction_operations.make_collection_instrument_id_runtime_select",
            "construction_operations.make_collection_instrument_ref_list_runtime_select",
            "construction_operations.make_collection_name_runtime_select",
            "construction_operations.make_collection_object_name_ref_list_runtime_select",
            "construction_operations.make_collection_object_name_runtime_select",
            "construction_operations.make_collection_vector_group_name_ref_list_runtime_select",
            "construction_operations.make_picture_name_ref_list_runtime_select",
            "construction_operations.make_point_name_ref_list_runtime_select",
            "construction_operations.make_point_name_runtime_select",
            "construction_operations.make_relationship_ref_list_runtime_select",
            "construction_operations.make_report_ref_list_runtime_select",
            "construction_operations.make_vector_name_ref_list_runtime_select",
            "gdt_operations.make_surface_face_list_runtime_select"
        ];

    [Fact]
    public void MissingProfileFailsStartup()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreatePolicy(profile: null));

        Assert.Contains("'Briosa:Security:Operations:Profile' is required", exception.Message, StringComparison.Ordinal);
        Assert.Contains("'standard'", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Allow:0", OperationId)]
    [InlineData("Deny:0", OperationId)]
    [InlineData("Allow", "")]
    [InlineData("deny:0", OperationId)]
    public void LegacyAllowAndDenyKeysFailStartupWithAMigrationMessage(string key, string value)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreatePolicy(settings: new() { [key] = value }));

        Assert.Contains("is no longer supported", exception.Message, StringComparison.Ordinal);
        Assert.Contains("'Profile'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("'Overrides:<service>:<operation>'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("admission-profile-migration.md", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacyKeysFailStartupEvenWithoutAProfile()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreatePolicy(profile: null, settings: new() { ["Allow:0"] = OperationId }));

        Assert.Contains("is no longer supported", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Profile", "unknown", "unknown profile")]
    [InlineData("Profile", "Standard", "unknown profile")]
    [InlineData("Profile", " ", "is required")]
    [InlineData("Profiles", "standard", "is not recognized")]
    [InlineData("Flags:filesystem-write", "deny", "unknown risk flag")]
    [InlineData("Flags:unknown", "allow", "unknown risk flag")]
    [InlineData("Flags:filesystem_write", "Deny", "must be 'allow' or 'deny'")]
    [InlineData("Flags:filesystem_write", "", "must be 'allow' or 'deny'")]
    [InlineData("Flags:filesystem_write:extra", "deny", "must be 'allow' or 'deny'")]
    [InlineData("Flags", "deny", "must be a section")]
    [InlineData("Overrides:unknown_operations:get_working_directory", "deny", "unknown service")]
    [InlineData("Overrides:file_operations:unknown", "deny", "unknown operation")]
    [InlineData("Overrides:file_operations:get_working_directory", "true", "must be 'allow' or 'deny'")]
    [InlineData("Overrides:file_operations", "deny", "must be a section")]
    [InlineData("Overrides", "deny", "must be a section")]
    public void UnknownNamesAndInvalidValuesFailStartup(string key, string value, string message)
    {
        var settings = new Dictionary<string, string?>(StringComparer.Ordinal) { [key] = value };
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreatePolicy(profile: key == "Profile" ? value : "standard",
                settings: key == "Profile" ? null : settings));

        Assert.Contains(message, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FullProfileWithTheInteractiveOptInAdmitsExactlyTheNonExclusiveSurface()
    {
        var policy = CreatePolicy(profile: "full", settings: new() { ["Flags:interactive_ui"] = "allow" });

        var expected = SpatialAnalyzerApi.Operations
            .Where(operation => OperationClassification.Find(operation.OperationId).Row!.Isolation ==
                OperationIsolationClass.Admissible)
            .Select(operation => operation.OperationId)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expected, policy.AllowedOperations.Select(operation => operation.OperationId));
        foreach (var operation in SpatialAnalyzerApi.Operations.Where(operation => !expected.Contains(operation.OperationId)))
        {
            var decision = policy.Evaluate(Command(operation.OperationId, operation.MpStep));
            Assert.Equal(OperationPolicyDecisionKind.Denied, decision.Kind);
            Assert.Equal("operation-isolation-unsupported", decision.DiagnosticCode);
            Assert.Equal(OperationExecutionScope.ExclusiveWorkflow, decision.Operation!.ExecutionScope);
        }
    }

    [Fact]
    public void ExclusiveWorkflowCannotBeOverriddenEvenWithAnExplicitAllow()
    {
        var policy = CreatePolicy(profile: "full", settings: new()
        {
            ["Overrides:instrument_operations:start_instrument_interface"] = "allow",
            ["Flags:device_session"] = "allow",
            ["Flags:external_io"] = "allow"
        });

        var decision = policy.Evaluate(ExclusiveOperationId);

        Assert.Equal(OperationPolicyDecisionKind.Denied, decision.Kind);
        Assert.Equal("operation-isolation-unsupported", decision.DiagnosticCode);
        Assert.Equal(OperationPolicy.IsolationRule, decision.PolicyRule);
        Assert.Equal(OperationExecutionScope.ExclusiveWorkflow, decision.Operation!.ExecutionScope);
        Assert.DoesNotContain(policy.AllowedOperations, operation => operation.OperationId == ExclusiveOperationId);
    }

    [Fact]
    public void OperationDenyOverrideWinsOverTheProfile()
    {
        var policy = CreatePolicy(settings: new() { [WorkingDirectoryOverride] = "deny" });

        AssertDecision(policy, OperationId, OperationPolicyDecisionKind.Denied, "operation-policy-denied", "override");
        Assert.DoesNotContain(policy.AllowedOperations, operation => operation.OperationId == OperationId);
        Assert.Equal(CreatePolicy().AllowedOperations.Count - 1, policy.AllowedOperations.Count);
    }

    [Fact]
    public void OperationAllowOverrideWinsOverAFlagDenyAndTheProfile()
    {
        var policy = CreatePolicy(profile: "read-only", settings: new()
        {
            ["Flags:filesystem_metadata"] = "deny",
            [WorkingDirectoryOverride] = "allow",
            ["Overrides:mp_subroutines:run_subroutine"] = "allow"
        });

        AssertDecision(policy, OperationId, OperationPolicyDecisionKind.Allowed, "operation-policy-allowed", "override");
        AssertDecision(policy, CodeExecutionOperationId, OperationPolicyDecisionKind.Allowed,
            "operation-policy-allowed", "override");
    }

    [Fact]
    public void FlagDenyWinsOverTheProfile()
    {
        var policy = CreatePolicy(profile: "full", settings: new() { ["Flags:filesystem_metadata"] = "deny" });

        AssertDecision(policy, OperationId, OperationPolicyDecisionKind.Denied, "operation-policy-denied",
            "flag.filesystem_metadata");
        Assert.DoesNotContain(policy.AllowedOperations, operation =>
            OperationClassification.Find(operation.OperationId).Row!.Risks.HasFlag(OperationRisks.FilesystemMetadata));
    }

    [Fact]
    public void FlagAllowWidensTheProfileButNotTheReadOnlyEffectFilter()
    {
        var standard = CreatePolicy();
        AssertDecision(standard, ExternalReadOperationId, OperationPolicyDecisionKind.Denied,
            "operation-policy-denied", "profile.standard");

        var widened = CreatePolicy(settings: new() { ["Flags:external_io"] = "allow" });
        AssertDecision(widened, ExternalReadOperationId, OperationPolicyDecisionKind.Allowed,
            "operation-policy-allowed", "profile.standard");

        var readOnly = CreatePolicy(profile: "read-only", settings: new() { ["Flags:filesystem_write"] = "allow" });
        AssertDecision(readOnly, FileWriteMutationId, OperationPolicyDecisionKind.Denied,
            "operation-policy-denied", "profile.read-only");
    }

    [Fact]
    public void ProfileDeniesOperationsWithRisksOutsideItsSet()
    {
        var policy = CreatePolicy();

        AssertDecision(policy, CodeExecutionOperationId, OperationPolicyDecisionKind.Denied,
            "operation-policy-denied", "profile.standard");
        AssertDecision(policy, OperationId, OperationPolicyDecisionKind.Allowed,
            "operation-policy-allowed", "profile.standard");
    }

    [Fact]
    public void InteractiveOperationsNeedAnExplicitOptIn()
    {
        Assert.Equal(OperationPolicyDecisionKind.Denied, CreatePolicy(profile: "full").Evaluate(InteractiveOperationId).Kind);

        var flag = CreatePolicy(settings: new() { ["Flags:interactive_ui"] = "allow" });
        AssertDecision(flag, InteractiveOperationId, OperationPolicyDecisionKind.Allowed,
            "operation-policy-allowed", "profile.standard");
        Assert.All(
            SpatialAnalyzerApi.Operations.Where(operation =>
                OperationClassification.Find(operation.OperationId).Row!.Risks == OperationRisks.InteractiveUi &&
                OperationClassification.Find(operation.OperationId).Row!.Isolation == OperationIsolationClass.Admissible),
            operation => Assert.Equal(OperationPolicyDecisionKind.Allowed, flag.Evaluate(operation.OperationId).Kind));

        var single = CreatePolicy(settings: new()
        {
            ["Overrides:construction_operations:make_point_name_runtime_select"] = "allow"
        });
        AssertDecision(single, InteractiveOperationId, OperationPolicyDecisionKind.Allowed,
            "operation-policy-allowed", "override");
        Assert.Equal(CreatePolicy().AllowedOperations.Count + 1, single.AllowedOperations.Count);
    }

    [Fact]
    public void UnreviewedClassificationIsDeniedEvenWithAnExplicitAllow()
    {
        var policy = CreatePolicy(
            profile: "full",
            settings: new() { [WorkingDirectoryOverride] = "allow" },
            classify: operationId => operationId == OperationId
                ? OperationClassificationLookup.Unreviewed
                : OperationClassification.Find(operationId));

        AssertDecision(policy, OperationId, OperationPolicyDecisionKind.Denied, "operation-risk-unreviewed",
            OperationPolicy.ClassificationRule);
        Assert.DoesNotContain(policy.AllowedOperations, operation => operation.OperationId == OperationId);
    }

    [Fact]
    public void ValidationStatusDoesNotAffectAdmission()
    {
        Assert.Equal(
            OperationValidationStatus.FixturePending,
            OperationClassification.Find(FixturePendingOperationId).Row!.ValidationStatus);

        AssertDecision(CreatePolicy(), FixturePendingOperationId, OperationPolicyDecisionKind.Allowed,
            "operation-policy-allowed", "profile.standard");
    }

    [Fact]
    public void FingerprintCoversProfileFlagsOverridesAndClassification()
    {
        var baseline = CreatePolicy().Fingerprint;
        Assert.Matches("^sha256:[0-9A-F]{64}$", baseline);
        Assert.Equal(baseline, CreatePolicy().Fingerprint);

        var variants = new[]
        {
            CreatePolicy(profile: "full").Fingerprint,
            CreatePolicy(settings: new() { ["Flags:filesystem_write"] = "deny" }).Fingerprint,
            CreatePolicy(settings: new() { ["Flags:filesystem_write"] = "allow" }).Fingerprint,
            CreatePolicy(settings: new() { [WorkingDirectoryOverride] = "deny" }).Fingerprint,
            CreatePolicy(settings: new() { [WorkingDirectoryOverride] = "allow" }).Fingerprint,
            CreatePolicy(classify: operationId => operationId == OperationId
                ? OperationClassificationLookup.Unreviewed
                : OperationClassification.Find(operationId)).Fingerprint
        };
        Assert.DoesNotContain(baseline, variants);
        Assert.Equal(variants.Length, variants.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void InteractiveRuntimeSelectorsAreNotReplaySafe()
    {
        var operations = SpatialAnalyzerApi.Operations.ToDictionary(
            operation => operation.OperationId, StringComparer.Ordinal);

        foreach (var operationId in RuntimeSelectors)
        {
            Assert.Equal(ReplaySafety.Unsafe, operations[operationId].ReplaySafety);
            Assert.True(OperationClassification.Find(operationId).Row!.Risks.HasFlag(OperationRisks.InteractiveUi));
        }

        Assert.DoesNotContain(SpatialAnalyzerApi.Operations, operation =>
            operation.ReplaySafety == ReplaySafety.Safe &&
            OperationClassification.Find(operation.OperationId).Row!.Risks.HasFlag(OperationRisks.InteractiveUi));
    }

    [Theory]
    [InlineData("unsupported.operation", "Get Working Directory", "operation-unsupported")]
    [InlineData(OperationId, "Different MP Step", "operation-binding-mismatch")]
    public void UnsupportedOrMismatchedBindingsNeverBecomeAllowed(
        string operationId,
        string stepName,
        string diagnosticCode)
    {
        var policy = CreatePolicy(profile: "full", settings: new() { [WorkingDirectoryOverride] = "allow" });

        var decision = policy.Evaluate(Command(operationId, stepName));

        Assert.Equal(OperationPolicyDecisionKind.Unsupported, decision.Kind);
        Assert.Equal(diagnosticCode, decision.DiagnosticCode);
    }

    [Theory]
    [InlineData(OperationExecutionScope.Unspecified, "operation-isolation-unreviewed")]
    [InlineData(OperationExecutionScope.Unknown, "operation-isolation-unreviewed")]
    [InlineData(OperationExecutionScope.ExclusiveWorkflow, "operation-isolation-unsupported")]
    public void UnsupportedIsolationScopesFailClosed(
        OperationExecutionScope executionScope,
        string diagnosticCode)
    {
        var operation = WorkingDirectoryOperation() with
        {
            ExecutionScope = executionScope
        };
        var policy = CreatePolicy(
            settings: new() { [WorkingDirectoryOverride] = "allow" },
            operations: [operation]);

        var decision = policy.Evaluate(Command());

        Assert.Equal(OperationPolicyDecisionKind.Denied, decision.Kind);
        Assert.Equal(diagnosticCode, decision.DiagnosticCode);
        Assert.Empty(policy.AllowedOperations);
    }

    [Fact]
    public async Task CompetingOrdinaryCallersCannotEnterAnExclusiveWorkflow()
    {
        var factory = new CountingProcessFactory();
        var supervisor = new WorkerProcessSupervisor(factory, RestartPolicy());
        await using var configuredSupervisor = supervisor.ConfigureAwait(true);
        var operation = WorkingDirectoryOperation() with
        {
            ExecutionScope = OperationExecutionScope.ExclusiveWorkflow
        };
        var executor = new PolicyEnforcingWorkerCommandExecutor(
            supervisor, supervisor,
            CreatePolicy(settings: new() { [WorkingDirectoryOverride] = "allow" }, operations: [operation]),
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance));

        var outcomes = await Task.WhenAll(
            executor.ExecuteAsync(Command(), Guid.NewGuid()),
            executor.ExecuteAsync(Command(), Guid.NewGuid())).ConfigureAwait(true);

        Assert.All(outcomes, outcome =>
        {
            Assert.Equal(WorkerExecutionStatus.PolicyDenied, outcome.Status);
            Assert.Equal("operation-isolation-unsupported", outcome.DiagnosticCode);
            Assert.Equal(WorkerExecutionDisposition.NotStarted, outcome.ExecutionDisposition);
        });
        Assert.Equal(0, factory.StartCount);
    }

    [Fact]
    public async Task DeniedOperationNeverMapsTheRequest()
    {
        var factory = new CountingProcessFactory();
        var supervisor = new WorkerProcessSupervisor(factory, RestartPolicy());
        await using var configuredSupervisor = supervisor.ConfigureAwait(true);
        var executor = new PolicyEnforcingWorkerCommandExecutor(supervisor, supervisor,
            CreatePolicy(settings: new() { [WorkingDirectoryOverride] = "deny" }),
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance));
        var outcome = await executor.ExecuteAsync(new WorkerCommandSubmission(OperationId,
            () => throw new InvalidOperationException("Denied request was mapped.")), Guid.NewGuid());
        Assert.Equal(WorkerExecutionStatus.PolicyDenied, outcome.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, outcome.ExecutionDisposition);
    }

    [Fact]
    public async Task RejectionOccursBeforeTheWorkerSupervisorIsStarted()
    {
        var factory = new CountingProcessFactory();
        var supervisor = new WorkerProcessSupervisor(factory, RestartPolicy());
        await using var configuredSupervisor = supervisor.ConfigureAwait(true);
        var executor = new PolicyEnforcingWorkerCommandExecutor(
            supervisor, supervisor,
            CreatePolicy(settings: new() { [WorkingDirectoryOverride] = "deny" }),
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance));
        var correlationId = Guid.NewGuid();

        var outcome = await executor.ExecuteAsync(Command(), correlationId)
            .ConfigureAwait(true);

        Assert.Equal(WorkerExecutionStatus.PolicyDenied, outcome.Status);
        Assert.Equal("operation-policy-denied", outcome.DiagnosticCode);
        Assert.Equal(correlationId, outcome.CorrelationId);
        Assert.Equal(0, factory.StartCount);
    }

    [Fact]
    public void UnrecognizedOperationEffectCannotEnterTheRegistry()
    {
        var operation = WorkingDirectoryOperation();
        Assert.Throws<ArgumentOutOfRangeException>(() => new OperationDescriptor(
            operation.OperationId, operation.MpStep, operation.GrpcService,
            operation.Rpc, operation.FullyQualifiedMethod, "misspelled",
            operation.ExecutionScope, operation.ReplaySafety, operation.RiskFlags));
    }

    /// <summary>
    /// Builds a policy from <c>Briosa:Security:Operations</c> settings. Keys in
    /// <paramref name="settings"/> are relative to that section.
    /// </summary>
    internal static OperationPolicy CreatePolicy(
        string? profile = "standard",
        Dictionary<string, string?>? settings = null,
        IReadOnlyList<OperationDescriptor>? operations = null,
        Func<string, OperationClassificationLookup>? classify = null)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (profile is not null)
        {
            values.Add(OperationPolicy.ProfileKey, profile);
        }

        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
        {
            values.Add($"{OperationPolicy.SectionKey}:{key}", value);
        }

        return OperationPolicy.Create(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build(),
            operations ?? SpatialAnalyzerApi.Operations,
            classify);
    }

    private static void AssertDecision(
        OperationPolicy policy,
        string operationId,
        OperationPolicyDecisionKind kind,
        string diagnosticCode,
        string rule)
    {
        var decision = policy.Evaluate(operationId);
        Assert.Equal(kind, decision.Kind);
        Assert.Equal(diagnosticCode, decision.DiagnosticCode);
        Assert.Equal(rule, decision.PolicyRule);
        Assert.Equal(
            kind == OperationPolicyDecisionKind.Allowed,
            policy.AllowedOperations.Any(operation => operation.OperationId == operationId));
    }

    private static OperationDescriptor WorkingDirectoryOperation() =>
        GetWorkingDirectoryOperation.Descriptor;

    private static WorkerMpCommand Command(
        string operationId = OperationId,
        string stepName = "Get Working Directory") =>
        new(operationId, stepName, [], []);

    private static WorkerLifecyclePolicy RestartPolicy() =>
        new(
            heartbeatInterval: TimeSpan.FromSeconds(1),
            heartbeatTimeout: TimeSpan.FromSeconds(1),
            startupTimeout: TimeSpan.FromSeconds(1),
            shutdownTimeout: TimeSpan.FromSeconds(1));

    private sealed class CountingProcessFactory : IWorkerProcessFactory
    {
        public int StartCount { get; private set; }

        public ValueTask<IWorkerProcess> StartAsync(
            int generation,
            CancellationToken cancellationToken = default)
        {
            StartCount++;
            throw new InvalidOperationException("The policy test must not start a worker.");
        }
    }
}
