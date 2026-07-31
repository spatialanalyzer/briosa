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
        return Task.FromResult(CreateCapabilities());
    }

    internal Api.GetServerInfoResponse CreateServerInfo()
    {
        var snapshot = _statusProvider.Current;
        var response = new Api.GetServerInfoResponse
        {
            Version = _buildIdentity.CreateVersionCoordinates(),
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

    internal Api.ListCapabilitiesResponse CreateCapabilities()
    {
        var response = new Api.ListCapabilitiesResponse
        {
            SpatialAnalyzerTarget = SpatialAnalyzerApi.TargetVersion,
            ProtocolPackage = SpatialAnalyzerApi.ProtocolPackage
        };
        response.Operations.AddRange(_operationPolicy.AllowedOperations.Select(operation =>
            new Api.OperationCapability
            {
                OperationId = operation.OperationId,
                GrpcService = operation.GrpcService,
                Rpc = operation.Rpc,
                FullyQualifiedMethod = operation.FullyQualifiedMethod,
                Effect = operation.Effect switch
                {
                    "read_only" => Api.OperationEffect.ReadOnly,
                    "mutating" => Api.OperationEffect.Mutating,
                    _ => Api.OperationEffect.Unknown
                },
                ExecutionScope = operation.ExecutionScope,
                ReplaySafety = operation.ReplaySafety
            }));
        return response;
    }

    private static Api.WorkerRuntimeState ToProtocolState(
        WorkerLifecycleState state) =>
        state switch
        {
            WorkerLifecycleState.Stopped => Api.WorkerRuntimeState.Stopped,
            WorkerLifecycleState.Starting => Api.WorkerRuntimeState.Starting,
            WorkerLifecycleState.Ready => Api.WorkerRuntimeState.Ready,
            WorkerLifecycleState.Degraded => Api.WorkerRuntimeState.Degraded,
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
