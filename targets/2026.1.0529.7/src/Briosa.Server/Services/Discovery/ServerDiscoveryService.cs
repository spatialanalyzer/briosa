using Briosa.Server.Operations;
using Briosa.Server.Security;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;
using WorkerIdentityEvidence = Briosa.Server.Workers.RuntimeIdentityEvidence;
using WorkerIdentityMatchState = Briosa.Server.Workers.RuntimeIdentityMatchState;
using WorkerIdentitySource = Briosa.Server.Workers.RuntimeIdentityEvidenceSource;

namespace Briosa.Server.Services;

internal sealed class ServerDiscoveryService(
    IWorkerStatusProvider statusProvider,
    IServerBuildIdentityProvider buildIdentity,
    OperationPolicy operationPolicy) :
    Api.DiscoveryService.DiscoveryServiceBase
{
    private readonly IServerBuildIdentityProvider _buildIdentity =
        buildIdentity ?? throw new ArgumentNullException(nameof(buildIdentity));
    private readonly IWorkerStatusProvider _statusProvider =
        statusProvider ?? throw new ArgumentNullException(nameof(statusProvider));
    private readonly OperationPolicy _operationPolicy =
        operationPolicy ?? throw new ArgumentNullException(nameof(operationPolicy));

    // Policy is fixed for the process lifetime, so both views are built once.
    private readonly Api.ListCapabilitiesResponse _capabilities =
        BuildCapabilities(operationPolicy, includeDenied: false);
    private readonly Api.ListCapabilitiesResponse _capabilitiesIncludingDenied =
        BuildCapabilities(operationPolicy, includeDenied: true);

    public override Task<Api.GetServerInfoResponse> GetServerInfo(
        Api.GetServerInfoRequest request,
        ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(CreateServerInfo());
    }

    public override Task<Api.ListCapabilitiesResponse> ListCapabilities(
        Api.ListCapabilitiesRequest request,
        ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(CreateCapabilities(request.IncludeDenied));
    }

    internal Api.GetServerInfoResponse CreateServerInfo()
    {
        var snapshot = _statusProvider.Current;
        var response = new Api.GetServerInfoResponse
        {
            Version = _buildIdentity.CreateVersionCoordinates(),
            Compatibility = ServerCompatibility.Create(),
            WorkerState = ToProtocolState(snapshot.State),
            SpatialAnalyzerConnectionState = ToProtocolState(snapshot.Connection?.State),
            SpatialAnalyzerExecutionReadinessState =
                ToProtocolState(snapshot.Connection?.ExecutionReadinessState),
            ReadyForMp = WorkerReadinessHealthCheck.IsReady(snapshot),
            TargetIsolationMode = _operationPolicy.TargetIsolationMode,
            ActivatedSdkIdentity = ToProtocolIdentity(
                snapshot.RuntimeIdentity?.ActivatedSdk),
            ConnectedSpatialAnalyzerIdentity = ToProtocolIdentity(
                snapshot.RuntimeIdentity?.ConnectedSpatialAnalyzer)
        };
        PopulateLegacyConnectedIdentity(
            response,
            snapshot.RuntimeIdentity?.ConnectedSpatialAnalyzer);
        return response;
    }

    internal Api.ListCapabilitiesResponse CreateCapabilities(bool includeDenied = false) =>
        (includeDenied ? _capabilitiesIncludingDenied : _capabilities).Clone();

    /// <summary>
    /// Maps a static <see cref="OperationPolicy.Evaluate(string)"/> decision to
    /// the admission discovery reports, from the precedence step that decided.
    /// </summary>
    internal static Api.OperationAdmission ToAdmission(OperationPolicyDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        return decision.Kind switch
        {
            OperationPolicyDecisionKind.Allowed => Api.OperationAdmission.Admitted,
            OperationPolicyDecisionKind.Denied => decision.PolicyRule switch
            {
                OperationPolicy.ClassificationRule => Api.OperationAdmission.DeniedUnreviewed,
                OperationPolicy.IsolationRule => Api.OperationAdmission.DeniedExclusive,
                OperationPolicy.OverrideRule => Api.OperationAdmission.DeniedOverride,
                var rule when rule.StartsWith(OperationPolicy.FlagRulePrefix, StringComparison.Ordinal) =>
                    Api.OperationAdmission.DeniedFlag,
                var rule when rule.StartsWith(OperationPolicy.ProfileRulePrefix, StringComparison.Ordinal) =>
                    Api.OperationAdmission.DeniedProfile,
                _ => throw UndescribedDecision(decision)
            },
            _ => throw UndescribedDecision(decision)
        };
    }

    private static InvalidOperationException UndescribedDecision(OperationPolicyDecision decision) =>
        new($"Discovery has no admission for a {decision.Kind} decision by policy rule '{decision.PolicyRule}'.");

    private static Api.ListCapabilitiesResponse BuildCapabilities(OperationPolicy policy, bool includeDenied)
    {
        var response = new Api.ListCapabilitiesResponse
        {
            SpatialAnalyzerTarget = SpatialAnalyzerApi.TargetVersion,
            ProtocolPackage = SpatialAnalyzerApi.ProtocolPackage,
            AdmissionProfile = policy.Profile.Name,
            PolicyFingerprint = policy.Fingerprint
        };
        foreach (var operation in policy.Operations)
        {
            var admission = ToAdmission(policy.Evaluate(operation.OperationId));
            if (includeDenied || admission == Api.OperationAdmission.Admitted)
            {
                response.Operations.Add(ToCapability(
                    operation,
                    policy.FindClassification(operation.OperationId),
                    admission));
            }
        }

        return response;
    }

    // Discovery reports the reviewed row. A request option can still add
    // interactive_ui or make a request an exclusive workflow at call time.
    private static Api.OperationCapability ToCapability(
        OperationDescriptor operation,
        OperationClassificationLookup classification,
        Api.OperationAdmission admission)
    {
        var capability = new Api.OperationCapability
        {
            OperationId = operation.OperationId,
            GrpcService = operation.GrpcService,
            Rpc = operation.Rpc,
            FullyQualifiedMethod = operation.FullyQualifiedMethod,
            Effect = operation.Effect,
            ExecutionScope = operation.ExecutionScope,
            ReplaySafety = operation.ReplaySafety,
            Admission = admission
        };
        if (classification is { IsReviewed: true, Row: { } row })
        {
            capability.RiskFlags.AddRange(OperationRiskVocabulary.GetWireFlags(row.Risks));
            capability.DurationClass = row.Duration;
            capability.ValidationStatus = row.ValidationStatus;
        }

        return capability;
    }

    private static Api.WorkerRuntimeState ToProtocolState(
        WorkerLifecycleState state) =>
        state switch
        {
            WorkerLifecycleState.Stopped => Api.WorkerRuntimeState.Stopped,
            WorkerLifecycleState.Starting => Api.WorkerRuntimeState.Starting,
            WorkerLifecycleState.Ready => Api.WorkerRuntimeState.Ready,
            WorkerLifecycleState.Degraded => Api.WorkerRuntimeState.Degraded,
            WorkerLifecycleState.Stopping => Api.WorkerRuntimeState.Stopping,
            _ => Api.WorkerRuntimeState.Unspecified
        };

    private static Api.SpatialAnalyzerConnectionState ToProtocolState(
        WorkerConnectionState? state) =>
        state switch
        {
            WorkerConnectionState.Disconnected =>
                Api.SpatialAnalyzerConnectionState.Disconnected,
            WorkerConnectionState.Connecting =>
                Api.SpatialAnalyzerConnectionState.Connecting,
            WorkerConnectionState.Connected =>
                Api.SpatialAnalyzerConnectionState.Connected,
            WorkerConnectionState.Faulted =>
                Api.SpatialAnalyzerConnectionState.Faulted,
            WorkerConnectionState.Stopping =>
                Api.SpatialAnalyzerConnectionState.Stopping,
            _ => Api.SpatialAnalyzerConnectionState.Unspecified
        };

    private static Api.SpatialAnalyzerExecutionReadinessState ToProtocolState(
        WorkerExecutionReadinessState? state) =>
        state switch
        {
            WorkerExecutionReadinessState.Unverified =>
                Api.SpatialAnalyzerExecutionReadinessState.Unverified,
            WorkerExecutionReadinessState.Verifying =>
                Api.SpatialAnalyzerExecutionReadinessState.Verifying,
            WorkerExecutionReadinessState.ExecutionReady =>
                Api.SpatialAnalyzerExecutionReadinessState.ExecutionReady,
            WorkerExecutionReadinessState.CompetingClientSuspected =>
                Api.SpatialAnalyzerExecutionReadinessState.CompetingClientSuspected,
            WorkerExecutionReadinessState.OperatorRecoveryRequired =>
                Api.SpatialAnalyzerExecutionReadinessState.OperatorRecoveryRequired,
            _ => Api.SpatialAnalyzerExecutionReadinessState.Unspecified
        };

    private static Api.RuntimeIdentityEvidence ToProtocolIdentity(
        WorkerIdentityEvidence? evidence)
    {
        var response = new Api.RuntimeIdentityEvidence
        {
            Source = evidence?.Source switch
            {
                WorkerIdentitySource.RuntimeVerification =>
                    Api.RuntimeIdentityEvidenceSource.RuntimeVerification,
                WorkerIdentitySource.OperatorAttestation =>
                    Api.RuntimeIdentityEvidenceSource.OperatorAttestation,
                _ => Api.RuntimeIdentityEvidenceSource.Unavailable
            },
            MatchState = evidence?.MatchState switch
            {
                WorkerIdentityMatchState.ExactMatch =>
                    Api.RuntimeIdentityMatchState.ExactMatch,
                WorkerIdentityMatchState.Mismatch =>
                    Api.RuntimeIdentityMatchState.Mismatch,
                _ => Api.RuntimeIdentityMatchState.Unavailable
            }
        };
        if (evidence?.Version is not null)
        {
            response.Version = evidence.Version;
        }

        return response;
    }

    private static void PopulateLegacyConnectedIdentity(
        Api.GetServerInfoResponse response,
        WorkerIdentityEvidence? evidence)
    {
        if (evidence?.Version is not null)
        {
            response.ConnectedSpatialAnalyzerVersion = evidence.Version;
        }

        response.ConnectedSpatialAnalyzerVersionState =
            (evidence?.Source, evidence?.MatchState) switch
            {
                (WorkerIdentitySource.RuntimeVerification,
                    WorkerIdentityMatchState.ExactMatch) =>
                    Api.ConnectedSpatialAnalyzerVersionState.VerifiedMatch,
                (WorkerIdentitySource.RuntimeVerification,
                    WorkerIdentityMatchState.Mismatch) =>
                    Api.ConnectedSpatialAnalyzerVersionState.VerifiedMismatch,
                (WorkerIdentitySource.OperatorAttestation,
                    WorkerIdentityMatchState.ExactMatch) =>
                    Api.ConnectedSpatialAnalyzerVersionState.OperatorAttestedMatch,
                (WorkerIdentitySource.OperatorAttestation,
                    WorkerIdentityMatchState.Mismatch) =>
                    Api.ConnectedSpatialAnalyzerVersionState.OperatorAttestedMismatch,
                _ => Api.ConnectedSpatialAnalyzerVersionState.Unavailable
            };
    }
}
