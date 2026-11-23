namespace Briosa.Server.Security;

/// <summary>
/// The precedence step of <see cref="OperationPolicy"/> that denied a request or
/// an operation's reviewed row (#397). <see cref="OperationPolicyDenial.ToAdmission"/>
/// and <see cref="OperationPolicyDenial.ToDetails"/> map each reason by name, never
/// by number.
/// </summary>
internal enum OperationPolicyDenialReason
{
    /// <summary>The profile, widened by flags set to allow, does not admit the effective risks.</summary>
    Profile,

    /// <summary>An effective risk flag is set to deny.</summary>
    Flag,

    /// <summary>A per-operation override is set to deny.</summary>
    Override,

    /// <summary>An exclusive workflow, statically or because a request option leaves work running.</summary>
    Exclusive,

    /// <summary>Unreviewed metadata, or a request that cannot be read against its reviewed options.</summary>
    Unreviewed
}

/// <summary>
/// The one structured, value-free record of a policy denial (#397). The audit
/// rule string, the discovery admission value, and the public
/// <see cref="global::Briosa.PolicyDenialDetails"/> are all derived from it, so
/// they cannot disagree.
/// </summary>
/// <remarks>
/// It carries a risk flag only for <see cref="OperationPolicyDenialReason.Flag"/>,
/// a profile name only for <see cref="OperationPolicyDenialReason.Profile"/>, and a
/// request field name only when the policy attributed the denial to that field.
/// It never carries a request value or a configuration key.
/// </remarks>
internal sealed record OperationPolicyDenial
{
    private OperationPolicyDenial(
        OperationPolicyDenialReason reason,
        OperationRisks? deniedFlag,
        string? admissionProfile,
        string? requestOptionField)
    {
        Reason = reason;
        DeniedFlag = deniedFlag;
        AdmissionProfile = admissionProfile;
        RequestOptionField = requestOptionField;
        PolicyRule = FormatPolicyRule();
    }

    /// <summary>
    /// The denial of the execution-budget producer, which has no
    /// <see cref="OperationPolicyDecision"/>: an operation without a reviewed
    /// duration class never executes.
    /// </summary>
    public static OperationPolicyDenial DurationUnreviewed { get; } = Unreviewed();

    public OperationPolicyDenialReason Reason { get; }

    /// <summary>The deciding risk flag, exactly one vocabulary member; set only for a flag denial.</summary>
    public OperationRisks? DeniedFlag { get; }

    /// <summary>The active admission profile; set only for a profile denial.</summary>
    public string? AdmissionProfile { get; }

    /// <summary>The request field the policy attributed the denial to, if any.</summary>
    public string? RequestOptionField { get; }

    /// <summary>
    /// The audit label of the deciding step: <c>classification</c>, <c>isolation</c>,
    /// <c>override</c>, <c>flag.&lt;name&gt;</c>, or <c>profile.&lt;name&gt;</c>, and
    /// <c>option.&lt;field&gt;</c> or <c>option.&lt;field&gt;/&lt;rule&gt;</c> when a
    /// request option decided.
    /// </summary>
    public string PolicyRule { get; }

    public static OperationPolicyDenial Unreviewed() =>
        new(OperationPolicyDenialReason.Unreviewed, deniedFlag: null, admissionProfile: null, requestOptionField: null);

    public static OperationPolicyDenial Exclusive(string? requestOptionField = null) =>
        new(OperationPolicyDenialReason.Exclusive, deniedFlag: null, admissionProfile: null,
            OptionalField(requestOptionField));

    public static OperationPolicyDenial Override() =>
        new(OperationPolicyDenialReason.Override, deniedFlag: null, admissionProfile: null, requestOptionField: null);

    public static OperationPolicyDenial Flag(OperationRisks deniedFlag, string? requestOptionField = null)
    {
        // Throws unless the value is exactly one vocabulary member.
        _ = OperationRiskVocabulary.GetName(deniedFlag);
        return new(OperationPolicyDenialReason.Flag, deniedFlag, admissionProfile: null,
            OptionalField(requestOptionField));
    }

    public static OperationPolicyDenial Profile(string admissionProfile, string? requestOptionField = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(admissionProfile);
        return new(OperationPolicyDenialReason.Profile, deniedFlag: null, admissionProfile,
            OptionalField(requestOptionField));
    }

    /// <summary>
    /// The discovery admission for this denial. With <see cref="ToRule"/> it is the
    /// one mapping from internal reasons to public values.
    /// </summary>
    public global::Briosa.OperationAdmission ToAdmission() => Map(Reason).Admission;

    /// <summary>The public rule for this denial.</summary>
    public global::Briosa.PolicyDenialRule ToRule() => Map(Reason).Rule;

    /// <summary>The value-free public detail: a new message on every call.</summary>
    public global::Briosa.PolicyDenialDetails ToDetails()
    {
        var details = new global::Briosa.PolicyDenialDetails { Rule = ToRule() };
        if (DeniedFlag is { } flag)
        {
            details.DeniedFlag = OperationRiskVocabulary.GetName(flag);
        }

        if (RequestOptionField is { } field)
        {
            details.RequestOptionField = field;
        }

        if (AdmissionProfile is { } profile)
        {
            details.AdmissionProfile = profile;
        }

        return details;
    }

    /// <summary>
    /// The exhaustive mapping of each reason to its denied
    /// <see cref="global::Briosa.OperationAdmission"/> value and its
    /// <see cref="global::Briosa.PolicyDenialRule"/>. The two public enums number
    /// the same steps differently, so nothing may cast between them.
    /// </summary>
    internal static (global::Briosa.OperationAdmission Admission, global::Briosa.PolicyDenialRule Rule) Map(
        OperationPolicyDenialReason reason) => reason switch
    {
        OperationPolicyDenialReason.Profile =>
            (global::Briosa.OperationAdmission.DeniedProfile, global::Briosa.PolicyDenialRule.Profile),
        OperationPolicyDenialReason.Flag =>
            (global::Briosa.OperationAdmission.DeniedFlag, global::Briosa.PolicyDenialRule.Flag),
        OperationPolicyDenialReason.Override =>
            (global::Briosa.OperationAdmission.DeniedOverride, global::Briosa.PolicyDenialRule.Override),
        OperationPolicyDenialReason.Exclusive =>
            (global::Briosa.OperationAdmission.DeniedExclusive, global::Briosa.PolicyDenialRule.Exclusive),
        OperationPolicyDenialReason.Unreviewed =>
            (global::Briosa.OperationAdmission.DeniedUnreviewed, global::Briosa.PolicyDenialRule.Unreviewed),
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "The policy denial reason is not reviewed.")
    };

    private static string? OptionalField(string? field)
    {
        if (field is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(field);
        }

        return field;
    }

    // The audit strings predate the structured record and keep their exact form.
    private string FormatPolicyRule()
    {
        var rule = Reason switch
        {
            OperationPolicyDenialReason.Unreviewed => OperationPolicy.ClassificationRule,
            OperationPolicyDenialReason.Exclusive => OperationPolicy.IsolationRule,
            OperationPolicyDenialReason.Override => OperationPolicy.OverrideRule,
            OperationPolicyDenialReason.Flag =>
                OperationPolicy.FlagRulePrefix + OperationRiskVocabulary.GetName(DeniedFlag!.Value),
            OperationPolicyDenialReason.Profile => OperationPolicy.ProfileRulePrefix + AdmissionProfile,
            _ => throw new ArgumentOutOfRangeException(nameof(Reason), Reason, "The policy denial reason is not reviewed.")
        };
        if (RequestOptionField is not { } field)
        {
            return rule;
        }

        var option = OperationPolicy.OptionRulePrefix + field;
        return Reason == OperationPolicyDenialReason.Exclusive ? option : $"{option}/{rule}";
    }
}
