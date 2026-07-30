using Briosa.Core.V1Alpha1;
using Briosa.Server.Generated.Sa.V2026_1_0529_7.V1Alpha1;
using Briosa.Server.Security;
using Microsoft.Extensions.Configuration;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.AspNetCore.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ProtocolIdentityMatchState = Briosa.Core.V1Alpha1.RuntimeIdentityMatchState;
using ProtocolIdentitySource = Briosa.Core.V1Alpha1.RuntimeIdentityEvidenceSource;
using ServerIdentityEvidence = Briosa.Server.Workers.RuntimeIdentityEvidence;
using ServerIdentityMatchState = Briosa.Server.Workers.RuntimeIdentityMatchState;
using ServerIdentitySource = Briosa.Server.Workers.RuntimeIdentityEvidenceSource;

namespace Briosa.Server.Tests;

public sealed class DiscoveryServiceTests
{
    [Fact]
    public async Task LivenessIsIndependentWhileReadinessRequiresVerifiedExecution()
    {
        var statusProvider = new FakeWorkerStatusProvider(Snapshot(
            WorkerLifecycleState.Ready,
            WorkerConnectionState.Faulted));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IWorkerStatusProvider>(statusProvider);
        services.AddBriosaHealthAndDiscovery();
        await using var provider = services.BuildServiceProvider();
        var health = provider.GetRequiredService<HealthCheckService>();
        var mappings = provider
            .GetRequiredService<IOptions<GrpcHealthChecksOptions>>()
            .Value.Services.Select(mapping => mapping.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var liveness = await health.CheckHealthAsync(registration =>
            registration.Name == WorkerReadinessHealthCheck.LivenessServiceName);
        var notReady = await health.CheckHealthAsync(registration =>
            registration.Name == WorkerReadinessHealthCheck.ReadinessServiceName);
        statusProvider.Current = Snapshot(
            WorkerLifecycleState.Ready,
            WorkerConnectionState.Connected);
        var connectedButUnverified = await health.CheckHealthAsync(registration =>
            registration.Name == WorkerReadinessHealthCheck.ReadinessServiceName);
        statusProvider.Current = Snapshot(
            WorkerLifecycleState.Ready,
            WorkerConnectionState.Connected,
            WorkerExecutionReadinessState.ExecutionReady);
        var ready = await health.CheckHealthAsync(registration =>
            registration.Name == WorkerReadinessHealthCheck.ReadinessServiceName);

        Assert.Equal(
            [string.Empty, "briosa.liveness", "briosa.readiness"],
            mappings);
        Assert.Equal(HealthStatus.Healthy, liveness.Status);
        Assert.Equal(HealthStatus.Unhealthy, notReady.Status);
        Assert.Equal(HealthStatus.Unhealthy, connectedButUnverified.Status);
        Assert.Equal(HealthStatus.Healthy, ready.Status);
        Assert.Equal("briosa.liveness", WorkerReadinessHealthCheck.LivenessServiceName);
        Assert.Equal("briosa.readiness", WorkerReadinessHealthCheck.ReadinessServiceName);
    }

    [Fact]
    public void ServerInfoReportsSafeStateWithoutInventingConnectedVersion()
    {
        var snapshot = Snapshot(
            WorkerLifecycleState.Ready,
            WorkerConnectionState.Connected,
            WorkerExecutionReadinessState.ExecutionReady) with
        {
            RuntimeIdentity = UnavailableIdentity()
        };
        var service = new ServerDiscoveryService(
            new FakeWorkerStatusProvider(snapshot),
            new FakeBuildIdentityProvider(),
            CreatePolicy());

        var response = service.CreateServerInfo();

        Assert.Equal("0.1.0-test", response.Version.BriosaVersion);
        Assert.Equal("briosa.core.v1alpha1", response.Version.CoreProtocolPackage);
        Assert.Equal("2026.1.0529.7", response.Version.SpatialAnalyzerTarget);
        Assert.Equal(WorkerRuntimeState.Ready, response.WorkerState);
        Assert.Equal(
            SpatialAnalyzerConnectionState.Connected,
            response.SpatialAnalyzerConnectionState);
        Assert.Equal(
            SpatialAnalyzerExecutionReadinessState.ExecutionReady,
            response.SpatialAnalyzerExecutionReadinessState);
        Assert.False(response.ReadyForMp);
        Assert.Equal(TargetIsolationMode.SingleTenant, response.TargetIsolationMode);
        Assert.False(response.HasConnectedSpatialAnalyzerVersion);
        Assert.Equal(
            ConnectedSpatialAnalyzerVersionState.Unavailable,
            response.ConnectedSpatialAnalyzerVersionState);
        Assert.False(response.ActivatedSdkIdentity.HasVersion);
        Assert.Equal(
            ProtocolIdentitySource.Unavailable,
            response.ActivatedSdkIdentity.Source);
        Assert.Equal(
            ProtocolIdentityMatchState.Unavailable,
            response.ActivatedSdkIdentity.MatchState);
        Assert.False(response.ConnectedSpatialAnalyzerIdentity.HasVersion);
    }

    [Fact]
    public void ServerInfoSeparatesConfiguredTargetAttachmentAndExecutionVerification()
    {
        var response = new ServerDiscoveryService(
            new FakeWorkerStatusProvider(Snapshot(
                WorkerLifecycleState.Ready,
                WorkerConnectionState.Connected,
                WorkerExecutionReadinessState.Unverified)),
            new FakeBuildIdentityProvider(),
            CreatePolicy())
            .CreateServerInfo();

        Assert.Equal("2026.1.0529.7", response.Version.SpatialAnalyzerTarget);
        Assert.Equal(
            SpatialAnalyzerConnectionState.Connected,
            response.SpatialAnalyzerConnectionState);
        Assert.Equal(
            SpatialAnalyzerExecutionReadinessState.Unverified,
            response.SpatialAnalyzerExecutionReadinessState);
        Assert.False(response.ReadyForMp);
    }

    [Fact]
    public void DiscoveryPreservesIndependentRuntimeAndAttestedIdentityEvidence()
    {
        var snapshot = Snapshot(
            WorkerLifecycleState.Ready,
            WorkerConnectionState.Connected,
            WorkerExecutionReadinessState.ExecutionReady) with
        {
            RuntimeIdentity = new ExactTargetIdentitySnapshot(
                new ServerIdentityEvidence(
                    "2026.1.0529.7",
                    ServerIdentitySource.RuntimeVerification,
                    ServerIdentityMatchState.ExactMatch),
                new ServerIdentityEvidence(
                    "2026.1.0529.7",
                    ServerIdentitySource.OperatorAttestation,
                    ServerIdentityMatchState.ExactMatch))
        };

        var response = new ServerDiscoveryService(
            new FakeWorkerStatusProvider(snapshot),
            new FakeBuildIdentityProvider(),
            CreatePolicy())
            .CreateServerInfo();

        Assert.True(response.ReadyForMp);
        Assert.Equal(
            ProtocolIdentitySource.RuntimeVerification,
            response.ActivatedSdkIdentity.Source);
        Assert.Equal(
            ProtocolIdentitySource.OperatorAttestation,
            response.ConnectedSpatialAnalyzerIdentity.Source);
        Assert.Equal(
            ConnectedSpatialAnalyzerVersionState.OperatorAttestedMatch,
            response.ConnectedSpatialAnalyzerVersionState);
        Assert.Equal(
            "2026.1.0529.7",
            response.ConnectedSpatialAnalyzerVersion);
    }

    [Fact]
    public void VerifiedMismatchIsDiscoverableAndCannotReportReady()
    {
        var snapshot = Snapshot(
            WorkerLifecycleState.Ready,
            WorkerConnectionState.Connected,
            WorkerExecutionReadinessState.ExecutionReady) with
        {
            RuntimeIdentity = new ExactTargetIdentitySnapshot(
                new ServerIdentityEvidence(
                    "2025.0",
                    ServerIdentitySource.RuntimeVerification,
                    ServerIdentityMatchState.Mismatch),
                new ServerIdentityEvidence(
                    "2026.1.0529.7",
                    ServerIdentitySource.RuntimeVerification,
                    ServerIdentityMatchState.ExactMatch))
        };

        var response = new ServerDiscoveryService(
            new FakeWorkerStatusProvider(snapshot),
            new FakeBuildIdentityProvider(),
            CreatePolicy())
            .CreateServerInfo();

        Assert.False(response.ReadyForMp);
        Assert.Equal("2025.0", response.ActivatedSdkIdentity.Version);
        Assert.Equal(
            ProtocolIdentityMatchState.Mismatch,
            response.ActivatedSdkIdentity.MatchState);
        Assert.Equal(
            ProtocolIdentityMatchState.ExactMatch,
            response.ConnectedSpatialAnalyzerIdentity.MatchState);
    }

    [Fact]
    public void CapabilitiesComeFromReviewedGeneratedCatalog()
    {
        var response = new ServerDiscoveryService(
            new FakeWorkerStatusProvider(Snapshot(WorkerLifecycleState.Stopped, null)),
            new FakeBuildIdentityProvider(),
            CreatePolicy())
            .CreateCapabilities();

        Assert.Equal("briosa.sa.2026.1.0529.7", response.CatalogId);
        Assert.Equal("12", response.CatalogRevision);
        Assert.Equal("2026.1.0529.7", response.SpatialAnalyzerTarget);
        Assert.Equal(
            "briosa.sa.v2026_1_0529_7.v1alpha1",
            response.TargetProtocolPackage);
        var operation = Assert.Single(response.Operations);
        Assert.Equal("file_operations.get_working_directory", operation.OperationId);
        Assert.Equal(
            "briosa.sa.v2026_1_0529_7.v1alpha1.FileOperations",
            operation.GrpcService);
        Assert.Equal("GetWorkingDirectory", operation.Rpc);
        Assert.Equal(
            "/briosa.sa.v2026_1_0529_7.v1alpha1.FileOperations/GetWorkingDirectory",
            operation.FullyQualifiedMethod);
        Assert.Equal(OperationEffect.ReadOnly, operation.Effect);
        Assert.Equal(
            OperationExecutionScope.GlobalStateRead,
            operation.ExecutionScope);
        Assert.Equal(ReplaySafety.Safe, operation.ReplaySafety);
    }

    [Fact]
    public void CapabilitiesExcludeOperationsDeniedByRuntimePolicy()
    {
        var response = new ServerDiscoveryService(
            new FakeWorkerStatusProvider(Snapshot(WorkerLifecycleState.Stopped, null)),
            new FakeBuildIdentityProvider(),
            CreatePolicy(allow: false))
            .CreateCapabilities();

        Assert.Empty(response.Operations);
    }

    [Fact]
    public void AssemblyIdentityUsesReviewedTargetAndInteropCoordinates()
    {
        var provider = new AssemblyServerBuildIdentityProvider(typeof(Program).Assembly);

        var coordinates = provider.CreateVersionCoordinates();

        Assert.True(coordinates.HasBriosaVersion);
        Assert.Equal("briosa.core.v1alpha1", coordinates.CoreProtocolPackage);
        Assert.Equal("2026.1.0529.7", coordinates.SpatialAnalyzerTarget);
        Assert.Equal(
            "briosa.sa.v2026_1_0529_7.v1alpha1",
            coordinates.TargetProtocolPackage);
        Assert.Equal("12", coordinates.CatalogRevision);
        Assert.Equal(
            AssemblyServerBuildIdentityProvider.InteropFingerprint,
            coordinates.InteropFingerprint);
    }

    private static OperationPolicy CreatePolicy(bool allow = true)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (allow)
        {
            values.Add(
                $"{OperationPolicy.AllowKey}:0",
                "file_operations.get_working_directory");
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        return OperationPolicy.Create(configuration, TargetCatalogMetadata.Operations);
    }
    private static WorkerLifecycleSnapshot Snapshot(
        WorkerLifecycleState workerState,
        WorkerConnectionState? connectionState,
        WorkerExecutionReadinessState executionReadinessState =
            WorkerExecutionReadinessState.Unverified) =>
        new(
            workerState,
            Generation: 2,
            ProcessId: 9876,
            RestartCount: 1,
            WorkerTerminationKind.None,
            "sensitive-internal-diagnostic",
            connectionState is null
                ? null
                : new WorkerConnectionSnapshot(
                    connectionState.Value,
                    executionReadinessState,
                    StatusCode: 42,
                    Attempt: 1,
                    MaximumAttempts: 3,
                    "sensitive-connection-diagnostic",
                    DateTimeOffset.UtcNow),
            DateTimeOffset.UtcNow,
            MatchingIdentity());

    private static ExactTargetIdentitySnapshot MatchingIdentity() =>
        new(
            new ServerIdentityEvidence(
                "2026.1.0529.7",
                ServerIdentitySource.OperatorAttestation,
                ServerIdentityMatchState.ExactMatch),
            new ServerIdentityEvidence(
                "2026.1.0529.7",
                ServerIdentitySource.OperatorAttestation,
                ServerIdentityMatchState.ExactMatch));

    private static ExactTargetIdentitySnapshot UnavailableIdentity() =>
        new(
            new ServerIdentityEvidence(
                Version: null,
                ServerIdentitySource.Unavailable,
                ServerIdentityMatchState.Unavailable),
            new ServerIdentityEvidence(
                Version: null,
                ServerIdentitySource.Unavailable,
                ServerIdentityMatchState.Unavailable));

    private sealed class FakeWorkerStatusProvider(WorkerLifecycleSnapshot current) :
        IWorkerStatusProvider
    {
        public WorkerLifecycleSnapshot Current { get; set; } = current;
    }

    private sealed class FakeBuildIdentityProvider : IServerBuildIdentityProvider
    {
        public VersionCoordinates CreateVersionCoordinates() =>
            new()
            {
                BriosaVersion = "0.1.0-test",
                CoreProtocolPackage = "briosa.core.v1alpha1",
                SpatialAnalyzerTarget = "2026.1.0529.7",
                TargetProtocolPackage = "briosa.sa.v2026_1_0529_7.v1alpha1",
                CatalogRevision = "2",
                InteropFingerprint = "sha256:test"
            };
    }
}
