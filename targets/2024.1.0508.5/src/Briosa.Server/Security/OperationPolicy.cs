using System.Collections.Frozen;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Briosa.Worker.Control;

namespace Briosa.Server.Security;

/// <summary>
/// Runtime operation admission decided on #242 and implemented by #293: a named
/// profile, per-flag settings, and per-operation overrides, applied to the
/// reviewed <see cref="OperationClassification"/> rows.
/// </summary>
/// <remarks>
/// Precedence, first match wins:
/// <list type="number">
/// <item>An unregistered operation or binding mismatch is unsupported.</item>
/// <item>Unreviewed metadata (no complete classification row, unknown effect,
/// unspecified replay safety, or an unreviewed execution scope) is denied.</item>
/// <item>An exclusive workflow is denied. Nothing can override it (invariant 13).</item>
/// <item>A per-operation <c>deny</c> override denies.</item>
/// <item>A per-operation <c>allow</c> override admits.</item>
/// <item>Any risk flag set to <c>deny</c> denies.</item>
/// <item>The profile, widened by flags set to <c>allow</c>, admits or denies.</item>
/// </list>
/// Validation status never affects admission.
/// </remarks>
internal sealed class OperationPolicy
{
    internal const string SectionKey = "Briosa:Security:Operations";
    internal const string ProfileKey = SectionKey + ":Profile";
    internal const string FlagsKey = SectionKey + ":Flags";
    internal const string OverridesKey = SectionKey + ":Overrides";

    internal const string AllowValue = "allow";
    internal const string DenyValue = "deny";

    internal const string RegistryRule = "registry";
    internal const string ClassificationRule = "classification";
    internal const string IsolationRule = "isolation";
    internal const string OverrideRule = "override";

    internal const string MigrationGuide =
        "See docs/development/admission-profile-migration.md.";

    // .NET configuration keys are case-insensitive, so the section names are
    // too. Profile, risk flag, service, and operation names and the allow/deny
    // values are exact.
    private static readonly FrozenSet<string> SectionChildren =
        new[] { "Profile", "Flags", "Overrides" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> LegacyChildren =
        new[] { "Allow", "Deny" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private readonly FrozenDictionary<string, OperationDescriptor> _operations;
    private readonly FrozenDictionary<string, OperationClassificationLookup> _classification;
    private readonly FrozenDictionary<OperationRisks, bool> _flags;
    private readonly FrozenDictionary<string, bool> _overrides;
    private readonly OperationRisks _allowedFlags;
    private readonly OperationRisks _deniedFlags;
    private readonly global::Briosa.TargetIsolationMode _targetIsolationMode =
        global::Briosa.TargetIsolationMode.SingleTenant;

    private OperationPolicy(
        IReadOnlyList<OperationDescriptor> operations,
        Func<string, OperationClassificationLookup> classify,
        OperationAdmissionProfile profile,
        IReadOnlyDictionary<OperationRisks, bool> flags,
        IReadOnlyDictionary<string, bool> overrides)
    {
        Profile = profile;
        _classification = operations.ToFrozenDictionary(
            operation => operation.OperationId,
            operation => classify(operation.OperationId),
            StringComparer.Ordinal);
        _operations = operations.ToFrozenDictionary(
            operation => operation.OperationId,
            operation => WithEffectiveScope(operation, _classification[operation.OperationId]),
            StringComparer.Ordinal);
        _flags = flags.ToFrozenDictionary();
        _overrides = overrides.ToFrozenDictionary(StringComparer.Ordinal);
        _allowedFlags = flags.Where(flag => flag.Value)
            .Aggregate(OperationRisks.None, (all, flag) => all | flag.Key);
        _deniedFlags = flags.Where(flag => !flag.Value)
            .Aggregate(OperationRisks.None, (all, flag) => all | flag.Key);
        AllowedOperations = _operations.Values
            .Where(operation => Evaluate(operation.OperationId).Kind == OperationPolicyDecisionKind.Allowed)
            .OrderBy(operation => operation.OperationId, StringComparer.Ordinal)
            .ToArray();
        ClassificationFingerprint = CreateClassificationFingerprint(_classification);
        Fingerprint = CreateFingerprint(profile, _flags, _overrides, ClassificationFingerprint);
    }

    /// <summary>The admitted operations, ordered by operation ID.</summary>
    public IReadOnlyList<OperationDescriptor> AllowedOperations { get; }

    /// <summary>The resolved admission profile.</summary>
    public OperationAdmissionProfile Profile { get; }

    public global::Briosa.TargetIsolationMode TargetIsolationMode =>
        _targetIsolationMode;

    /// <summary>The number of configured <c>Flags</c> settings.</summary>
    public int FlagSettingCount => _flags.Count;

    /// <summary>The number of configured per-operation <c>Overrides</c>.</summary>
    public int OverrideCount => _overrides.Count;

    /// <summary>
    /// A value-free SHA-256 over the resolved profile, flags, overrides, and the
    /// classification rows of every registered operation.
    /// </summary>
    public string Fingerprint { get; }

    /// <summary>A SHA-256 over the classification rows of every registered operation.</summary>
    public string ClassificationFingerprint { get; }

    public static OperationPolicy Create(
        IConfiguration configuration,
        IReadOnlyList<OperationDescriptor> operations,
        Func<string, OperationClassificationLookup>? classify = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(operations);

        var duplicateOperation = operations
            .GroupBy(operation => operation.OperationId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateOperation is not null)
        {
            throw new InvalidOperationException(
                $"The supported operation registry contains duplicate operation ID '{duplicateOperation.Key}'.");
        }

        var section = configuration.GetSection(SectionKey);
        RejectLegacyAndUnknownKeys(section);
        var profile = ReadProfile(configuration);
        var flags = ReadFlags(configuration);
        var overrides = ReadOverrides(configuration, operations);
        return new OperationPolicy(
            operations,
            classify ?? OperationClassification.Find,
            profile,
            flags,
            overrides);
    }

    public OperationPolicyDecision Evaluate(WorkerMpCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var decision = Evaluate(command.OperationId);
        return decision.Operation is { } operation &&
            !string.Equals(command.StepName, operation.MpStep, StringComparison.Ordinal)
            ? new OperationPolicyDecision(OperationPolicyDecisionKind.Unsupported,
                "operation-binding-mismatch", operation, RegistryRule)
            : decision;
    }

    public OperationPolicyDecision Evaluate(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);

        // 1. Registration.
        if (!_operations.TryGetValue(operationId, out var operation))
        {
            return new OperationPolicyDecision(
                OperationPolicyDecisionKind.Unsupported,
                "operation-unsupported",
                Operation: null,
                RegistryRule);
        }

        // 2. Reviewed metadata.
        var classification = _classification[operationId];
        if (classification.Row is not { } row ||
            !classification.IsReviewed ||
            operation.Effect == global::Briosa.OperationEffect.Unknown ||
            operation.ReplaySafety == global::Briosa.ReplaySafety.Unspecified ||
            operation.RiskFlags.Contains("unknown", StringComparer.Ordinal))
        {
            return new OperationPolicyDecision(
                OperationPolicyDecisionKind.Denied,
                "operation-risk-unreviewed",
                operation,
                ClassificationRule);
        }

        if (operation.ExecutionScope is
            global::Briosa.OperationExecutionScope.Unspecified or
            global::Briosa.OperationExecutionScope.Unknown)
        {
            return new OperationPolicyDecision(
                OperationPolicyDecisionKind.Denied,
                "operation-isolation-unreviewed",
                operation,
                ClassificationRule);
        }

        // 3. Isolation. An exclusive workflow cannot be admitted by any setting.
        if (!IsExecutionScopeSupported(operation.ExecutionScope))
        {
            return new OperationPolicyDecision(
                OperationPolicyDecisionKind.Denied,
                "operation-isolation-unsupported",
                operation,
                IsolationRule);
        }

        // 4 and 5. Per-operation overrides.
        if (_overrides.TryGetValue(operationId, out var overrideAllows))
        {
            return overrideAllows
                ? Allowed(operation, OverrideRule)
                : Denied(operation, OverrideRule);
        }

        // 6. Flag denials.
        var deniedFlags = row.Risks & _deniedFlags;
        if (deniedFlags != OperationRisks.None)
        {
            var first = OperationRiskVocabulary.Members.First(risk => deniedFlags.HasFlag(risk));
            return Denied(operation, $"flag.{OperationRiskVocabulary.GetName(first)}");
        }

        // 7. Profile, widened by flags set to allow.
        var profileRule = $"profile.{Profile.Name}";
        return Profile.Admits(row.Risks, operation.Effect, _allowedFlags)
            ? Allowed(operation, profileRule)
            : Denied(operation, profileRule);
    }

    private static OperationPolicyDecision Allowed(OperationDescriptor operation, string rule) =>
        new(OperationPolicyDecisionKind.Allowed, "operation-policy-allowed", operation, rule);

    private static OperationPolicyDecision Denied(OperationDescriptor operation, string rule) =>
        new(OperationPolicyDecisionKind.Denied, "operation-policy-denied", operation, rule);

    private static OperationDescriptor WithEffectiveScope(
        OperationDescriptor operation,
        OperationClassificationLookup classification) =>
        classification.Row is { Isolation: OperationIsolationClass.ExclusiveWorkflow }
            ? operation with { ExecutionScope = global::Briosa.OperationExecutionScope.ExclusiveWorkflow }
            : operation;

    private static bool IsExecutionScopeSupported(
        global::Briosa.OperationExecutionScope executionScope) =>
        executionScope is
            global::Briosa.OperationExecutionScope.SelfContained or
            global::Briosa.OperationExecutionScope.GlobalStateRead or
            global::Briosa.OperationExecutionScope.GlobalStateMutation;

    private static void RejectLegacyAndUnknownKeys(IConfigurationSection section)
    {
        if (!string.IsNullOrEmpty(section.Value))
        {
            throw new InvalidOperationException(
                $"Configuration value '{SectionKey}' must be a section with 'Profile', 'Flags', and 'Overrides'.");
        }

        foreach (var child in section.GetChildren())
        {
            if (LegacyChildren.Contains(child.Key))
            {
                throw new InvalidOperationException(
                    $"Configuration key '{SectionKey}:{child.Key}' is no longer supported. " +
                    "The indexed Allow and Deny arrays were replaced by 'Profile' " +
                    "(read-only, standard, device, or full), 'Flags:<risk_flag>', and " +
                    $"'Overrides:<service>:<operation>' set to 'allow' or 'deny'. {MigrationGuide}");
            }

            if (!SectionChildren.Contains(child.Key))
            {
                throw new InvalidOperationException(
                    $"Configuration key '{SectionKey}:{child.Key}' is not recognized. " +
                    $"Use 'Profile', 'Flags', or 'Overrides'. {MigrationGuide}");
            }
        }
    }

    private static OperationAdmissionProfile ReadProfile(IConfiguration configuration)
    {
        var section = configuration.GetSection(ProfileKey);
        if (section.GetChildren().Any())
        {
            throw new InvalidOperationException(
                $"Configuration value '{ProfileKey}' must be one profile name.");
        }

        var name = section.Value;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                $"Configuration value '{ProfileKey}' is required. Set it to " +
                $"{FormatProfiles()}; the packaged default is '{OperationAdmissionProfile.DefaultName}'. {MigrationGuide}");
        }

        return OperationAdmissionProfile.Find(name) ??
            throw new InvalidOperationException(
                $"Configuration value '{ProfileKey}' names unknown profile '{name}'. Use {FormatProfiles()}.");
    }

    private static Dictionary<OperationRisks, bool> ReadFlags(IConfiguration configuration)
    {
        var section = configuration.GetSection(FlagsKey);
        if (!string.IsNullOrEmpty(section.Value))
        {
            throw new InvalidOperationException(
                $"Configuration value '{FlagsKey}' must be a section of '<risk_flag>' settings.");
        }

        var flags = new Dictionary<OperationRisks, bool>();
        foreach (var child in section.GetChildren())
        {
            var risk = OperationRiskVocabulary.Members.Cast<OperationRisks?>().FirstOrDefault(member =>
                string.Equals(OperationRiskVocabulary.GetName(member!.Value), child.Key, StringComparison.Ordinal));
            if (risk is null)
            {
                throw new InvalidOperationException(
                    $"Configuration key '{FlagsKey}:{child.Key}' names an unknown risk flag. Use one of " +
                    $"{string.Join(", ", OperationRiskVocabulary.Members.Select(OperationRiskVocabulary.GetName))}.");
            }

            flags.Add(risk.Value, ReadSetting(child, $"{FlagsKey}:{child.Key}"));
        }

        return flags;
    }

    private static Dictionary<string, bool> ReadOverrides(
        IConfiguration configuration,
        IReadOnlyList<OperationDescriptor> operations)
    {
        var section = configuration.GetSection(OverridesKey);
        if (!string.IsNullOrEmpty(section.Value))
        {
            throw new InvalidOperationException(
                $"Configuration value '{OverridesKey}' must be a section of '<service>:<operation>' settings.");
        }

        var registered = operations
            .Select(operation => operation.OperationId)
            .ToFrozenSet(StringComparer.Ordinal);
        var services = registered
            .Select(operationId => operationId[..operationId.IndexOf('.', StringComparison.Ordinal)])
            .ToFrozenSet(StringComparer.Ordinal);
        var overrides = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var service in section.GetChildren())
        {
            var serviceKey = $"{OverridesKey}:{service.Key}";
            if (!services.Contains(service.Key))
            {
                throw new InvalidOperationException(
                    $"Configuration key '{serviceKey}' names an unknown service.");
            }

            if (!string.IsNullOrEmpty(service.Value))
            {
                throw new InvalidOperationException(
                    $"Configuration value '{serviceKey}' must be a section of '<operation>' settings.");
            }

            foreach (var operation in service.GetChildren())
            {
                var operationKey = $"{serviceKey}:{operation.Key}";
                var operationId = $"{service.Key}.{operation.Key}";
                if (!registered.Contains(operationId))
                {
                    throw new InvalidOperationException(
                        $"Configuration key '{operationKey}' names an unknown operation.");
                }

                overrides.Add(operationId, ReadSetting(operation, operationKey));
            }
        }

        return overrides;
    }

    private static bool ReadSetting(IConfigurationSection setting, string key) =>
        setting.GetChildren().Any() ? throw InvalidSetting(key) : setting.Value switch
        {
            AllowValue => true,
            DenyValue => false,
            _ => throw InvalidSetting(key)
        };

    private static InvalidOperationException InvalidSetting(string key) =>
        new($"Configuration value '{key}' must be '{AllowValue}' or '{DenyValue}'.");

    private static string FormatProfiles() =>
        string.Join(", ", OperationAdmissionProfile.All.Select(profile => $"'{profile.Name}'"));

    private static string CreateClassificationFingerprint(
        IReadOnlyDictionary<string, OperationClassificationLookup> classification)
    {
        var canonical = string.Join(
            '\n',
            classification
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => pair.Value.Row is { } row && pair.Value.IsReviewed
                    ? string.Create(CultureInfo.InvariantCulture,
                        $"{pair.Key}|{(int)row.Risks}|{row.Duration}|{row.ValidationStatus}|{row.Isolation}")
                    : $"{pair.Key}|unreviewed"));
        return Hash(canonical);
    }

    private static string CreateFingerprint(
        OperationAdmissionProfile profile,
        IReadOnlyDictionary<OperationRisks, bool> flags,
        IReadOnlyDictionary<string, bool> overrides,
        string classificationFingerprint)
    {
        var canonical = string.Join(
            '\n',
            new[] { $"profile:{profile.Name}" }
                .Concat(flags
                    .Select(flag => $"flag:{OperationRiskVocabulary.GetName(flag.Key)}={Format(flag.Value)}")
                    .Order(StringComparer.Ordinal))
                .Concat(overrides
                    .Select(item => $"override:{item.Key}={Format(item.Value)}")
                    .Order(StringComparer.Ordinal))
                .Append($"classification:{classificationFingerprint}"));
        return Hash(canonical);
    }

    private static string Format(bool allow) => allow ? AllowValue : DenyValue;

    private static string Hash(string canonical) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))}";
}
