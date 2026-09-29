namespace Briosa.Server.Services;

internal sealed partial class LifecycleAuditLogger(ILogger<LifecycleAuditLogger> logger)
{
    private readonly ILogger<LifecycleAuditLogger> _logger = logger;

    [LoggerMessage(EventId = 1302, Level = LogLevel.Warning,
        Message = "Lifecycle RPC {Rpc} rejected with gRPC status {GrpcStatus}, diagnostic {DiagnosticCode}.")]
    public partial void Rejected(string rpc, Grpc.Core.StatusCode grpcStatus, string diagnosticCode);
}
