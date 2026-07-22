using Briosa.Server.Generated.Sa.V2026_1_0529_7.V1Alpha1;
using Briosa.Server.Security;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using CoreProtocol = Briosa.Core.V1Alpha1;

namespace Briosa.Server.Services;

internal sealed class ServerDiscoveryService(
    IWorkerStatusProvider statusProvider,
    IServerBuildIdentityProvider buildIdentity,
    OperationPolicy operationPolicy) :
    CoreProtocol.DiscoveryService.DiscoveryServiceBase
{
    private readonly IServerBuildIdentityProvider _buildIdentity =
        buildIdentity ?? throw new ArgumentNullException(nameof(buildIdentity));
    private readonly IWorkerStatusProvider _statusProvider =
        statusProvider ?? throw new ArgumentNullException(nameof(statusProvider));
    private readonly OperationPolicy _operationPolicy =
        operationPolicy ?? throw new ArgumentNullException(nameof(operationPolicy));

    public override Task<CoreProtocol.GetServerInfoResponse> GetServerInfo(
        CoreProtocol.GetServerInfoRequest request,
        ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(CreateServerInfo());
    }

    public override Task<CoreProtocol.ListCapabilitiesResponse> ListCapabilities(
        CoreProtocol.ListCapabilitiesRequest request,
        ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(CreateCapabilities());
    }

    internal CoreProtocol.GetServerInfoResponse CreateServerInfo()
    {
        var snapshot = _statusProvider.Current;
        return new CoreProtocol.GetServerInfoResponse
        {
            Version = _buildIdentity.CreateVersionCoordinates(),
            WorkerState = ToProtocolState(snapshot.State),
            SpatialAnalyzerConnectionState = ToProtocolState(snapshot.Connection?.State),
            ReadyForMp = WorkerReadinessHealthCheck.IsReady(snapshot),
            ConnectedSpatialAnalyzerVersionState =
                CoreProtocol.ConnectedSpatialAnalyzerVersionState.Unavailable
        };
    }

    internal CoreProtocol.ListCapabilitiesResponse CreateCapabilities()
    {
        var response = new CoreProtocol.ListCapabilitiesResponse
        {
            CatalogId = TargetCatalogMetadata.CatalogId,
            CatalogRevision = TargetCatalogMetadata.CatalogRevision,
            SpatialAnalyzerTarget = TargetCatalogMetadata.SpatialAnalyzerTarget,
            TargetProtocolPackage = TargetCatalogMetadata.TargetProtocolPackage
        };
        response.Operations.AddRange(_operationPolicy.AllowedOperations.Select(operation =>
            new CoreProtocol.OperationCapability
            {
                OperationId = operation.OperationId,
                GrpcService = operation.GrpcService,
                Rpc = operation.Rpc,
                FullyQualifiedMethod = operation.FullyQualifiedMethod,
                Effect = operation.Effect switch
                {
                    "read_only" => CoreProtocol.OperationEffect.ReadOnly,
                    "mutating" => CoreProtocol.OperationEffect.Mutating,
                    _ => CoreProtocol.OperationEffect.Unknown
                }
            }));
        return response;
    }

    private static CoreProtocol.WorkerRuntimeState ToProtocolState(
        WorkerLifecycleState state) =>
        state switch
        {
            WorkerLifecycleState.Stopped => CoreProtocol.WorkerRuntimeState.Stopped,
            WorkerLifecycleState.Starting => CoreProtocol.WorkerRuntimeState.Starting,
            WorkerLifecycleState.Ready => CoreProtocol.WorkerRuntimeState.Ready,
            WorkerLifecycleState.Degraded => CoreProtocol.WorkerRuntimeState.Degraded,
            _ => CoreProtocol.WorkerRuntimeState.Unspecified
        };

    private static CoreProtocol.SpatialAnalyzerConnectionState ToProtocolState(
        WorkerConnectionState? state) =>
        state switch
        {
            WorkerConnectionState.Disconnected =>
                CoreProtocol.SpatialAnalyzerConnectionState.Disconnected,
            WorkerConnectionState.Connecting =>
                CoreProtocol.SpatialAnalyzerConnectionState.Connecting,
            WorkerConnectionState.Connected =>
                CoreProtocol.SpatialAnalyzerConnectionState.Connected,
            WorkerConnectionState.Faulted =>
                CoreProtocol.SpatialAnalyzerConnectionState.Faulted,
            WorkerConnectionState.Stopping =>
                CoreProtocol.SpatialAnalyzerConnectionState.Stopping,
            _ => CoreProtocol.SpatialAnalyzerConnectionState.Unspecified
        };
}
