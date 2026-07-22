namespace Briosa.Server.Security;

internal sealed record CatalogOperationDescriptor(
    string OperationId,
    string MpStep,
    string GrpcService,
    string Rpc,
    string FullyQualifiedMethod,
    string Effect,
    IReadOnlyList<string> RiskFlags);
