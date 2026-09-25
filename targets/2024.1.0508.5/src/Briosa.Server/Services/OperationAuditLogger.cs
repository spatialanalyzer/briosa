using Briosa.Server.Security;

namespace Briosa.Server.Services;

internal sealed partial class OperationAuditLogger(ILogger<OperationAuditLogger> logger)
{
    private readonly ILogger<OperationAuditLogger> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public void PolicyLoaded(OperationPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        LogPolicyLoaded(
            policy.AllowCount,
            policy.DenyCount,
            policy.TargetIsolationMode,
            policy.Fingerprint);
    }

    public void RequestStarted(
        Guid correlationId,
        OperationDescriptor operation,
        string actorCategory)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (!_logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var riskFlags = FormatRiskFlags(operation.RiskFlags);
        LogRequestStarted(
            correlationId,
            actorCategory,
            operation.FullyQualifiedMethod,
            operation.OperationId,
            operation.Effect,
            operation.ExecutionScope,
            riskFlags);
    }

    public void PolicyEvaluated(Guid correlationId, OperationPolicyDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        var operationId = decision.Operation?.OperationId ?? "unsupported";
        var effect = decision.Operation?.Effect ?? "unknown";
        var executionScope = decision.Operation?.ExecutionScope ??
            global::Briosa.OperationExecutionScope.Unspecified;
        if (decision.Kind == OperationPolicyDecisionKind.Allowed)
        {
            if (!_logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            var allowedRiskFlags = FormatRiskFlags(decision.Operation?.RiskFlags ?? []);
            LogPolicyAllowed(
                correlationId,
                operationId,
                effect,
                executionScope,
                allowedRiskFlags,
                decision.DiagnosticCode);
            return;
        }

        if (!_logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        var rejectedRiskFlags = FormatRiskFlags(decision.Operation?.RiskFlags ?? []);
        LogPolicyRejected(
            correlationId,
            operationId,
            decision.Kind,
            effect,
            executionScope,
            rejectedRiskFlags,
            decision.DiagnosticCode);
    }

    public void OperationCompleted(
        Guid correlationId,
        string operationId,
        int generation,
        long requestDurationMilliseconds,
        OperationAuditSummary summary) =>
        LogOperationCompleted(
            correlationId,
            operationId,
            generation,
            requestDurationMilliseconds,
            summary.SdkDurationMilliseconds,
            summary.ExecutionDisposition,
            summary.MpOutcome,
            summary.OutputRetrievalOutcome,
            summary.MpResultCode);

    public void OperationFailed(
        Guid correlationId,
        string operationId,
        int generation,
        long requestDurationMilliseconds,
        OperationAuditSummary summary,
        Grpc.Core.StatusCode grpcStatus,
        string diagnosticCode) =>
        LogOperationFailed(
            correlationId,
            operationId,
            generation,
            requestDurationMilliseconds,
            summary.SdkDurationMilliseconds,
            summary.ExecutionDisposition,
            summary.MpOutcome,
            summary.OutputRetrievalOutcome,
            summary.MpResultCode,
            grpcStatus,
            diagnosticCode);

    private static string FormatRiskFlags(IReadOnlyList<string> riskFlags) =>
        riskFlags.Count == 0 ? "none" : string.Join(',', riskFlags);

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Information,
        Message = "Operation policy loaded with {AllowCount} allowed and {DenyCount} denied IDs for target isolation mode {TargetIsolationMode}; fingerprint {PolicyFingerprint}.")]
    private partial void LogPolicyLoaded(
        int allowCount,
        int denyCount,
        global::Briosa.TargetIsolationMode targetIsolationMode,
        string policyFingerprint);

    [LoggerMessage(
        EventId = 2001,
        SkipEnabledCheck = true,
        Level = LogLevel.Debug,
        Message = "Request {CorrelationId} received from {ActorCategory} for endpoint {Endpoint}, operation {OperationId}, effect {Effect}, execution scope {ExecutionScope}, risk flags {RiskFlags}.")]
    private partial void LogRequestStarted(
        Guid correlationId,
        string actorCategory,
        string endpoint,
        string operationId,
        string effect,
        global::Briosa.OperationExecutionScope executionScope,
        string riskFlags);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Debug,
        SkipEnabledCheck = true,
        Message = "Request {CorrelationId} policy allowed operation {OperationId}, effect {Effect}, execution scope {ExecutionScope}, risk flags {RiskFlags}, diagnostic {DiagnosticCode}.")]
    private partial void LogPolicyAllowed(
        Guid correlationId,
        string operationId,
        string effect,
        global::Briosa.OperationExecutionScope executionScope,
        string riskFlags,
        string diagnosticCode);

    [LoggerMessage(
        EventId = 2003,
        SkipEnabledCheck = true,
        Level = LogLevel.Warning,
        Message = "Request {CorrelationId} policy rejected operation {OperationId} as {PolicyDecision}, effect {Effect}, execution scope {ExecutionScope}, risk flags {RiskFlags}, diagnostic {DiagnosticCode}.")]
    private partial void LogPolicyRejected(
        Guid correlationId,
        string operationId,
        OperationPolicyDecisionKind policyDecision,
        string effect,
        global::Briosa.OperationExecutionScope executionScope,
        string riskFlags,
        string diagnosticCode);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Information,
        Message = "Request {CorrelationId} completed operation {OperationId} on worker generation {Generation} in {RequestDurationMilliseconds} ms; SDK duration {SdkDurationMilliseconds} ms, execution disposition {ExecutionDisposition}, MP outcome {MpOutcome}, output retrieval {OutputRetrievalOutcome}, MP result {MpResultCode}.")]
    private partial void LogOperationCompleted(
        Guid correlationId,
        string operationId,
        int generation,
        long requestDurationMilliseconds,
        long? sdkDurationMilliseconds,
        string executionDisposition,
        string mpOutcome,
        string outputRetrievalOutcome,
        int? mpResultCode);

    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Warning,
        Message = "Request {CorrelationId} failed operation {OperationId} on worker generation {Generation} in {RequestDurationMilliseconds} ms; SDK duration {SdkDurationMilliseconds} ms, execution disposition {ExecutionDisposition}, MP outcome {MpOutcome}, output retrieval {OutputRetrievalOutcome}, MP result {MpResultCode}, gRPC status {GrpcStatus}, diagnostic {DiagnosticCode}.")]
    private partial void LogOperationFailed(
        Guid correlationId,
        string operationId,
        int generation,
        long requestDurationMilliseconds,
        long? sdkDurationMilliseconds,
        string executionDisposition,
        string mpOutcome,
        string outputRetrievalOutcome,
        int? mpResultCode,
        Grpc.Core.StatusCode grpcStatus,
        string diagnosticCode);
}
