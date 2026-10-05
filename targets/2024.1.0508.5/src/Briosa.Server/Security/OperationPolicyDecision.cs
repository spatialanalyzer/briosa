namespace Briosa.Server.Security;

/// <summary>
/// The outcome of <see cref="OperationPolicy.Evaluate(string)"/> or
/// <see cref="OperationPolicy.EvaluateRequest(string, Google.Protobuf.IMessage)"/>.
/// The diagnostic code is value-free and stable; <paramref name="PolicyRule"/>
/// names the precedence step that decided, for audit (for example
/// <c>override</c>, <c>flag.filesystem_write</c>, <c>profile.standard</c>, or
/// <c>option.show_results_dialog/flag.interactive_ui</c>), and never a request
/// value. <paramref name="DurationClass"/> is the effective duration class of an
/// admitted request.
/// </summary>
internal sealed record OperationPolicyDecision(
    OperationPolicyDecisionKind Kind,
    string DiagnosticCode,
    OperationDescriptor? Operation,
    string PolicyRule = OperationPolicy.RegistryRule,
    OperationDurationClass? DurationClass = null);
