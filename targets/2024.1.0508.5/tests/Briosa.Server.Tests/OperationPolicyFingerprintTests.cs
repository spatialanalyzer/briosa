using System.Reflection;
using global::Briosa;
using Briosa.Server.Operations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Security;
using Briosa.Worker.Control;

namespace Briosa.Server.Tests;

/// <summary>
/// The policy fingerprint promises that equal fingerprints mean equal static
/// admission decisions, and that it changes whenever an admission input
/// changes. These tests change one input at a time and pin the set of inputs.
/// </summary>
public sealed class OperationPolicyFingerprintTests
{
    private const string OperationId = "file_operations.get_working_directory";
    private const string StepName = "Get Working Directory";

    public static TheoryData<string> DescriptorVariants => new(DescriptorVariantFactories.Keys);

    [Theory]
    [MemberData(nameof(DescriptorVariants))]
    public void EveryDescriptorAdmissionInputChangesTheFingerprint(string variant)
    {
        var baseline = SingleOperationPolicy(Baseline());
        var changed = SingleOperationPolicy(DescriptorVariantFactories[variant](Baseline()));

        Assert.NotEqual(baseline.Fingerprint, changed.Fingerprint);
    }

    [Fact]
    public void DescriptorVariantsThatChangeTheDecisionNeverShareAFingerprint()
    {
        // The review reproduction: one classification row, the read-only profile,
        // no flags, overrides, or options, and three descriptors with three
        // different static decisions.
        var admitted = SingleOperationPolicy(Baseline());
        var mutating = SingleOperationPolicy(Variant(Baseline(), effect: "state_mutation",
            scope: OperationExecutionScope.GlobalStateMutation));
        var exclusive = SingleOperationPolicy(Variant(Baseline(),
            scope: OperationExecutionScope.ExclusiveWorkflow));

        Assert.Equal(OperationAdmission.Admitted, Admission(admitted));
        Assert.Equal(OperationAdmission.DeniedProfile, Admission(mutating));
        Assert.Equal(OperationAdmission.DeniedExclusive, Admission(exclusive));
        Assert.Equal(3, new[] { admitted.Fingerprint, mutating.Fingerprint, exclusive.Fingerprint }
            .Distinct(StringComparer.Ordinal).Count());

        var policies = DescriptorVariantFactories.Values
            .Select(factory => SingleOperationPolicy(factory(Baseline())))
            .Append(admitted)
            .ToArray();
        Assert.Equal(policies.Length, policies.Select(policy => policy.Fingerprint).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void TheMpBindingIsAnAdmissionInput()
    {
        var command = new WorkerMpCommand(OperationId, StepName, [], []);
        var bound = SingleOperationPolicy(Baseline());
        var rebound = SingleOperationPolicy(Variant(Baseline(), mpStep: "Get Working Directory Elsewhere"));

        Assert.Equal(OperationPolicyDecisionKind.Allowed, bound.Evaluate(command).Kind);
        Assert.Equal(OperationPolicyDecisionKind.Unsupported, rebound.Evaluate(command).Kind);
        Assert.NotEqual(bound.Fingerprint, rebound.Fingerprint);
    }

    [Fact]
    public void TheRegisteredOperationSetIsAnAdmissionInput()
    {
        var other = SpatialAnalyzerApi.Operations.Single(operation =>
            operation.OperationId == "utility_operations.get_active_units");
        var one = SingleOperationPolicy(Baseline());
        var two = OperationPolicyTests.CreatePolicy(
            profile: "read-only",
            operations: [Baseline(), other],
            requestClassifier: new OperationRequestClassifier([]));
        var none = OperationPolicyTests.CreatePolicy(
            profile: "read-only",
            operations: [],
            requestClassifier: new OperationRequestClassifier([]));

        Assert.Equal(OperationPolicyDecisionKind.Unsupported, none.Evaluate(OperationId).Kind);
        Assert.Equal(3, new[] { one.Fingerprint, two.Fingerprint, none.Fingerprint }
            .Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void DescriptorOrderDoesNotChangeTheFingerprint()
    {
        var operations = SpatialAnalyzerApi.Operations;

        Assert.Equal(
            OperationPolicyTests.CreatePolicy(operations: operations).Fingerprint,
            OperationPolicyTests.CreatePolicy(operations: operations.Reverse().ToArray()).Fingerprint);
    }

    [Fact]
    public void AuthorRiskFlagOrderDoesNotChangeTheFingerprint()
    {
        var first = SingleOperationPolicy(Variant(Baseline(), riskFlags: ["filesystem_metadata", "destructive"]));
        var second = SingleOperationPolicy(Variant(Baseline(), riskFlags: ["destructive", "filesystem_metadata"]));

        Assert.Equal(first.Fingerprint, second.Fingerprint);
    }

    // A new descriptor, row, option, profile, or policy member must be classified
    // here: either it reaches the canonical fingerprint (and the variant theory or
    // a fingerprint test covers it), or it is recorded as not deciding admission.
    [Fact]
    public void EveryPolicyDecisionInputIsCoveredOrExplicitlyExcluded()
    {
        AssertMembers<OperationDescriptor>(
            covered: ["OperationId", "MpStep", "Effect", "ExecutionScope", "ReplaySafety", "RiskFlags"],
            // Discovery and audit identity only; enforcement keys on the operation ID.
            excluded: ["GrpcService", "Rpc", "FullyQualifiedMethod", "EffectLabel"]);
        AssertMembers<OperationClassificationRow>(
            covered: ["OperationId", "Risks", "Duration", "ValidationStatus", "Isolation"],
            excluded: ["IsComplete"]);
        AssertMembers<OperationConditionalOption>(
            covered: ["OperationId", "Field", "Effect", "Condition", "Absence", "EnablingValues"],
            // The SDK argument and how the entry arose do not change which requests enable it.
            excluded: ["MpArgument", "Trigger", "IsComplete"]);
        AssertMembers<OperationAdmissionProfile>(
            covered: ["Name", "AdmittedRisks", "ReadOnlyEffectOnly"],
            excluded: []);

        var policyFields = typeof(OperationPolicy)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Select(field => field.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(PolicyFields.Order(StringComparer.Ordinal), policyFields);
    }

    private static readonly string[] PolicyFields =
        [
            // Hashed inputs.
            "_classification", "_flags", "_operations", "_overrides", "_requestClassifier",
            "_targetIsolationMode", "<Profile>k__BackingField",
            // Derived from the hashed inputs, or the digests themselves.
            "<AllowedOperations>k__BackingField", "<ClassificationFingerprint>k__BackingField",
            "<Fingerprint>k__BackingField", "<Operations>k__BackingField",
            "<RegistryFingerprint>k__BackingField", "_allowedFlags", "_deniedFlags"
        ];

    private static readonly IReadOnlyDictionary<string, Func<OperationDescriptor, OperationDescriptor>>
        DescriptorVariantFactories = new Dictionary<string, Func<OperationDescriptor, OperationDescriptor>>(StringComparer.Ordinal)
        {
            ["effect-mutating"] = descriptor => Variant(descriptor, effect: "state_mutation"),
            ["effect-unknown"] = descriptor => Variant(descriptor, effect: "unknown"),
            ["scope-self-contained"] = descriptor => Variant(descriptor, scope: OperationExecutionScope.SelfContained),
            ["scope-mutation"] = descriptor => Variant(descriptor, scope: OperationExecutionScope.GlobalStateMutation),
            ["scope-exclusive"] = descriptor => Variant(descriptor, scope: OperationExecutionScope.ExclusiveWorkflow),
            ["scope-unknown"] = descriptor => Variant(descriptor, scope: OperationExecutionScope.Unknown),
            ["scope-unspecified"] = descriptor => Variant(descriptor, scope: OperationExecutionScope.Unspecified),
            ["replay-unsafe"] = descriptor => Variant(descriptor, replaySafety: ReplaySafety.Unsafe),
            ["replay-unknown"] = descriptor => Variant(descriptor, replaySafety: ReplaySafety.Unknown),
            ["replay-unspecified"] = descriptor => Variant(descriptor, replaySafety: ReplaySafety.Unspecified),
            ["risk-flag-unknown"] = descriptor => Variant(descriptor, riskFlags: ["filesystem_metadata", "unknown"]),
            ["risk-flags-empty"] = descriptor => Variant(descriptor, riskFlags: []),
            ["mp-step"] = descriptor => Variant(descriptor, mpStep: "Get Working Directory Elsewhere")
        };

    private static OperationDescriptor Baseline() => GetWorkingDirectoryOperation.Descriptor;

    private static OperationDescriptor Variant(
        OperationDescriptor descriptor,
        string? mpStep = null,
        string? effect = null,
        OperationExecutionScope? scope = null,
        ReplaySafety? replaySafety = null,
        IReadOnlyList<string>? riskFlags = null) =>
        new(
            descriptor.OperationId,
            mpStep ?? descriptor.MpStep,
            descriptor.GrpcService,
            descriptor.Rpc,
            descriptor.FullyQualifiedMethod,
            effect ?? descriptor.EffectLabel,
            scope ?? descriptor.ExecutionScope,
            replaySafety ?? descriptor.ReplaySafety,
            riskFlags ?? descriptor.RiskFlags);

    // The same reviewed row, profile, flags, overrides, and (no) options for every
    // variant, so only the descriptor differs.
    private static OperationPolicy SingleOperationPolicy(OperationDescriptor descriptor) =>
        OperationPolicyTests.CreatePolicy(
            profile: "read-only",
            operations: [descriptor],
            classify: OperationClassification.Find,
            requestClassifier: new OperationRequestClassifier([]));

    private static OperationAdmission Admission(OperationPolicy policy) =>
        Briosa.Server.Services.ServerDiscoveryService.ToAdmission(policy.Evaluate(OperationId));

    private static void AssertMembers<T>(IReadOnlyCollection<string> covered, IReadOnlyCollection<string> excluded)
    {
        var properties = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.Name != "EqualityContract")
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal);
        Assert.Equal(covered.Concat(excluded).Order(StringComparer.Ordinal), properties);
    }
}
