namespace Briosa.Server.Security;

/// <summary>
/// The outcome of <see cref="OperationPolicy.Evaluate(string)"/>. The diagnostic
/// code is value-free and stable; <paramref name="PolicyRule"/> names the
/// precedence step that decided, for audit (for example <c>override</c>,
/// <c>flag.filesystem_write</c>, or <c>profile.standard</c>).
/// </summary>
internal sealed record OperationPolicyDecision(
    OperationPolicyDecisionKind Kind,
    string DiagnosticCode,
    OperationDescriptor? Operation,
    string PolicyRule = OperationPolicy.RegistryRule);
