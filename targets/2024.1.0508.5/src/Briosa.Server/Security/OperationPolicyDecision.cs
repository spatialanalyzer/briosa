namespace Briosa.Server.Security;

/// <summary>
/// The outcome of <see cref="OperationPolicy.Evaluate(string)"/> or
/// <see cref="OperationPolicy.EvaluateRequest(string, Google.Protobuf.IMessage)"/>.
/// The diagnostic code is value-free and stable; <see cref="PolicyRule"/>
/// names the precedence step that decided, for audit (for example
/// <c>override</c>, <c>flag.filesystem_write</c>, <c>profile.standard</c>, or
/// <c>option.show_results_dialog/flag.interactive_ui</c>), and never a request
/// value. <see cref="DurationClass"/> is the effective duration class of an
/// admitted request. A denied decision carries its structured
/// <see cref="Denial"/>, and its audit rule is derived from that record.
/// </summary>
internal sealed record OperationPolicyDecision
{
    /// <summary>Creates an admitted or unsupported decision; use <see cref="Deny"/> for a denial.</summary>
    public OperationPolicyDecision(
        OperationPolicyDecisionKind Kind,
        string DiagnosticCode,
        OperationDescriptor? Operation,
        string PolicyRule = OperationPolicy.RegistryRule,
        OperationDurationClass? DurationClass = null)
    {
        if (Kind == OperationPolicyDecisionKind.Denied)
        {
            throw new ArgumentException(
                "A denied decision carries its structured denial; create it with Deny.", nameof(Kind));
        }

        this.Kind = Kind;
        this.DiagnosticCode = DiagnosticCode;
        this.Operation = Operation;
        this.PolicyRule = PolicyRule;
        this.DurationClass = DurationClass;
    }

    private OperationPolicyDecision(
        string diagnosticCode,
        OperationDescriptor? operation,
        OperationPolicyDenial denial)
    {
        Kind = OperationPolicyDecisionKind.Denied;
        DiagnosticCode = diagnosticCode;
        Operation = operation;
        Denial = denial;
        PolicyRule = denial.PolicyRule;
    }

    public OperationPolicyDecisionKind Kind { get; }

    public string DiagnosticCode { get; }

    public OperationDescriptor? Operation { get; }

    public string PolicyRule { get; }

    public OperationDurationClass? DurationClass { get; }

    /// <summary>The structured denial; present exactly when <see cref="Kind"/> is denied.</summary>
    public OperationPolicyDenial? Denial { get; }

    /// <summary>
    /// The caller restriction the call carried (#443), whether or not it decided;
    /// null when the call carried none. It never changes any other member of a
    /// decision that the server's configuration made.
    /// </summary>
    public OperationCallerRestriction? CallerRestriction { get; private init; }

    /// <summary>What the caller restriction decided, for audit.</summary>
    public OperationCallerRestrictionOutcome CallerRestrictionOutcome => CallerRestriction is null
        ? OperationCallerRestrictionOutcome.Absent
        : Denial is { IsCallerRestriction: true }
            ? OperationCallerRestrictionOutcome.Denied
            : Kind == OperationPolicyDecisionKind.Allowed
                ? OperationCallerRestrictionOutcome.Admitted
                : OperationCallerRestrictionOutcome.NotEvaluated;

    /// <summary>The same decision, recording the caller restriction the call carried.</summary>
    internal OperationPolicyDecision WithCallerRestriction(OperationCallerRestriction callerRestriction) =>
        this with { CallerRestriction = callerRestriction ?? throw new ArgumentNullException(nameof(callerRestriction)) };

    /// <summary>Creates a denied decision whose audit rule is derived from <paramref name="denial"/>.</summary>
    public static OperationPolicyDecision Deny(
        string diagnosticCode,
        OperationDescriptor? operation,
        OperationPolicyDenial denial)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(diagnosticCode);
        ArgumentNullException.ThrowIfNull(denial);
        return new(diagnosticCode, operation, denial);
    }
}
