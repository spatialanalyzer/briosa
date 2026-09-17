using global::Briosa;

namespace Briosa.Server.Security;

/// <summary>
/// Describes one implemented public MP operation for policy, discovery, and audit.
/// </summary>
internal sealed record OperationDescriptor(
    string OperationId,
    string MpStep,
    string GrpcService,
    string Rpc,
    string FullyQualifiedMethod,
    string Effect,
    OperationExecutionScope ExecutionScope,
    ReplaySafety ReplaySafety,
    IReadOnlyList<string> RiskFlags);
