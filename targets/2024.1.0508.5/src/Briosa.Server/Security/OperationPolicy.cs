using System.Collections.Frozen;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Briosa.Worker.Control;
using Google.Protobuf;

namespace Briosa.Server.Security;

/// <summary>
/// Runtime operation admission decided on #242 and implemented by #293: a named
/// profile, per-flag settings, and per-operation overrides, applied to the
/// reviewed <see cref="OperationClassification"/> rows and, for each request, to
/// the reviewed <see cref="OperationConditionalOptions"/> the request enables.
/// </summary>
/// <remarks>
/// Precedence, first match wins:
/// <list type="number">
/// <item>An unregistered operation or binding mismatch is unsupported.</item>
/// <item>Unreviewed metadata (no complete classification row, unknown effect,
/// unspecified or unknown replay safety, or an unreviewed execution scope) is
/// denied, and so is a request that cannot be read against the conditional
/// option table.</item>
/// <item>An exclusive workflow is denied, including a request whose options leave
/// device work running after the call returns. Nothing can override it
/// (invariant 13).</item>
/// <item>A per-operation <c>deny</c> override denies.</item>
/// <item>A per-operation <c>allow</c> override admits.</item>
/// <item>Any effective risk flag set to <c>deny</c> denies.</item>
/// <item>The profile, widened by flags set to <c>allow</c>, admits or denies the
/// effective risks.</item>
/// </list>
/// A request that turns on operator UI gains <see cref="OperationRisks.InteractiveUi"/>
/// and the <see cref="OperationDurationClass.Interactive"/> duration class.
/// <see cref="Evaluate(string)"/> decides the static row only, for discovery;
/// enforcement uses <see cref="EvaluateRequest(string, IMessage)"/>. Validation
/// status never affects admission.
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
    internal const string FlagRulePrefix = "flag.";
    internal const string ProfileRulePrefix = "profile.";
    internal const string OptionRulePrefix = "option.";

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
    private readonly OperationRequestClassifier _requestClassifier;
    private readonly FrozenDictionary<OperationRisks, bool> _flags;
    private readonly FrozenDictionary<string, bool> _overrides;
    private readonly OperationRisks _allowedFlags;
    private readonly OperationRisks _deniedFlags;
    private readonly global::Briosa.TargetIsolationMode _targetIsolationMode =
        global::Briosa.TargetIsolationMode.SingleTenant;

    private OperationPolicy(
        IReadOnlyList<OperationDescriptor> operations,
        Func<string, OperationClassificationLookup> classify,
        OperationRequestClassifier requestClassifier,
        OperationAdmissionProfile profile,
        IReadOnlyDictionary<OperationRisks, bool> flags,
        IReadOnlyDictionary<string, bool> overrides)
    {
        Profile = profile;
        _requestClassifier = requestClassifier;
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
        Operations = _operations.Values
            .OrderBy(operation => operation.OperationId, StringComparer.Ordinal)
            .ToArray();
        AllowedOperations = Operations
            .Where(operation => Evaluate(operation.OperationId).Kind == OperationPolicyDecisionKind.Allowed)
            .ToArray();
        ClassificationFingerprint = CreateClassificationFingerprint(_classification, _requestClassifier);
        Fingerprint = CreateFingerprint(profile, _flags, _overrides, ClassificationFingerprint);
    }

    /// <summary>
    /// The operations whose reviewed row is admitted, ordered by operation ID. A
    /// request can still be denied when its options enable operator UI or leave
    /// work running.
    /// </summary>
    public IReadOnlyList<OperationDescriptor> AllowedOperations { get; }

    /// <summary>
    /// Every registered operation, admitted or not, with its effective execution
    /// scope, ordered by operation ID.
    /// </summary>
    public IReadOnlyList<OperationDescriptor> Operations { get; }

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
    /// classification rows and conditional options of every registered operation.
    /// </summary>
    public string Fingerprint { get; }

    /// <summary>
    /// A SHA-256 over the classification rows and conditional options of every
    /// registered operation.
    /// </summary>
    public string ClassificationFingerprint { get; }

    public static OperationPolicy Create(
        IConfiguration configuration,
        IReadOnlyList<OperationDescriptor> operations,
        Func<string, OperationClassificationLookup>? classify = null,
        OperationRequestClassifier? requestClassifier = null)
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
            requestClassifier ?? OperationRequestClassifier.Default,
            profile,
            flags,
            overrides);
    }

    /// <summary>Decides the static row of a command's operation and checks its MP binding.</summary>
    public OperationPolicyDecision Evaluate(WorkerMpCommand command) =>
        CheckBinding(command, Evaluate(command?.OperationId!));

    /// <summary>Decides one request and checks the mapped command's MP binding.</summary>
    public OperationPolicyDecision EvaluateRequest(WorkerMpCommand command, IMessage? request) =>
        CheckBinding(command, EvaluateRequest(command?.OperationId!, request));

    /// <summary>
    /// Decides the operation's reviewed row alone, as discovery reports it. It
    /// never classifies a request; enforcement uses <see cref="EvaluateRequest(string, IMessage)"/>.
    /// </summary>
    public OperationPolicyDecision Evaluate(string operationId) =>
        EvaluateCore(operationId, classification: null);

    /// <summary>
    /// Returns the classification lookup this policy decides a registered
    /// operation from, or <see cref="OperationClassificationLookup.Unreviewed"/>
    /// for an unregistered operation.
    /// </summary>
    public OperationClassificationLookup FindClassification(string operationId)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        return _classification.TryGetValue(operationId, out var lookup)
            ? lookup
            : OperationClassificationLookup.Unreviewed;
    }

    /// <summary>
    /// Decides one typed request before mapping or dispatch. The request's
    /// enabled conditional options change its effective risks, duration, and
    /// isolation. An operation with reviewed options fails closed when the request
    /// is missing or cannot be read.
    /// </summary>
    public OperationPolicyDecision EvaluateRequest(string operationId, IMessage? request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        return EvaluateCore(operationId, _requestClassifier.Classify(operationId, request));
    }

    private static OperationPolicyDecision CheckBinding(WorkerMpCommand command, OperationPolicyDecision decision)
    {
        ArgumentNullException.ThrowIfNull(command);
        return decision.Operation is { } operation &&
            !string.Equals(command.StepName, operation.MpStep, StringComparison.Ordinal)
            ? new OperationPolicyDecision(OperationPolicyDecisionKind.Unsupported,
                "operation-binding-mismatch", operation, RegistryRule)
            : decision;
    }

    private OperationPolicyDecision EvaluateCore(
        string operationId,
        OperationRequestClassification? classification)
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

        // 2. Reviewed metadata, then the request's conditional options.
        var lookup = _classification[operationId];
        if (lookup.Row is not { } row ||
            !lookup.IsReviewed ||
            operation.Effect == global::Briosa.OperationEffect.Unknown ||
            operation.ReplaySafety is global::Briosa.ReplaySafety.Unspecified or
                global::Briosa.ReplaySafety.Unknown ||
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

        if (classification is { IsClassified: false })
        {
            return new OperationPolicyDecision(
                OperationPolicyDecisionKind.Denied,
                "operation-request-unclassified",
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

        // A request that leaves device or operator work running after the call
        // returns needs cross-RPC ownership: it is an exclusive workflow.
        if (classification?.First(OperationOptionEffect.BackgroundWork) is { } background)
        {
            return new OperationPolicyDecision(
                OperationPolicyDecisionKind.Denied,
                "operation-option-isolation-unsupported",
                operation,
                OptionRule(background));
        }

        var ui = classification?.First(OperationOptionEffect.InteractiveUi);
        var risks = ui is null ? row.Risks : row.Risks | OperationRisks.InteractiveUi;
        var duration = ui is null ? row.Duration : OperationDurationClass.Interactive;

        // 4 and 5. Per-operation overrides.
        if (_overrides.TryGetValue(operationId, out var overrideAllows))
        {
            return overrideAllows
                ? Allowed(operation, WithOption(ui, OverrideRule), duration)
                : Denied(operation, OverrideRule);
        }

        // 6. Flag denials of the effective risks.
        var deniedFlags = risks & _deniedFlags;
        if (deniedFlags != OperationRisks.None)
        {
            var first = OperationRiskVocabulary.Members.First(risk => deniedFlags.HasFlag(risk));
            var flagRule = FlagRulePrefix + OperationRiskVocabulary.GetName(first);
            return (row.Risks & _deniedFlags) == OperationRisks.None
                ? OptionDenied(operation, ui!, flagRule)
                : Denied(operation, flagRule);
        }

        // 7. Profile, widened by flags set to allow, over the effective risks.
        var profileRule = ProfileRulePrefix + Profile.Name;
        if (Profile.Admits(risks, operation.Effect, _allowedFlags))
        {
            return Allowed(operation, WithOption(ui, profileRule), duration);
        }

        return ui is not null && Profile.Admits(row.Risks, operation.Effect, _allowedFlags)
            ? OptionDenied(operation, ui, profileRule)
            : Denied(operation, profileRule);
    }

    private static string OptionRule(OperationConditionalOption option) =>
        OptionRulePrefix + option.Field;

    // The option field name and the deciding rule, never the option's value.
    private static string WithOption(OperationConditionalOption? option, string rule) =>
        option is null ? rule : $"{OptionRule(option)}/{rule}";

    private static OperationPolicyDecision OptionDenied(
        OperationDescriptor operation,
        OperationConditionalOption option,
        string rule) =>
        new(OperationPolicyDecisionKind.Denied, "operation-option-denied", operation, WithOption(option, rule));

    private static OperationPolicyDecision Allowed(
        OperationDescriptor operation,
        string rule,
        OperationDurationClass duration) =>
        new(OperationPolicyDecisionKind.Allowed, "operation-policy-allowed", operation, rule, duration);

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
        IReadOnlyDictionary<string, OperationClassificationLookup> classification,
        OperationRequestClassifier requestClassifier)
    {
        var canonical = string.Join(
            '\n',
            classification
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => (pair.Value.Row is { } row && pair.Value.IsReviewed
                    ? string.Create(CultureInfo.InvariantCulture,
                        $"{pair.Key}|{(int)row.Risks}|{row.Duration}|{row.ValidationStatus}|{row.Isolation}")
                    : $"{pair.Key}|unreviewed") +
                    string.Concat(requestClassifier.OptionsFor(pair.Key).Select(option =>
                        $"|option:{option.Field},{option.Effect},{option.Condition},{option.Absence}," +
                        string.Join(',', option.EnablingValues)))));
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
