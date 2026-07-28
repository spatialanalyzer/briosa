using Briosa.Core.V1Alpha1;

namespace Briosa.Server.Security;

internal sealed record CatalogOperationDescriptor(
    string OperationId,
    string MpStep,
    string GrpcService,
    string Rpc,
    string FullyQualifiedMethod,
    string Effect,
    OperationExecutionScope ExecutionScope,
    ReplaySafety ReplaySafety,
    IReadOnlyList<string> RiskFlags);
