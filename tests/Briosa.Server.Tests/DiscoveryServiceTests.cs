using Briosa.Core.V1Alpha1;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.AspNetCore.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Briosa.Server.Tests;

public sealed class DiscoveryServiceTests
{
    [Fact]
    public async Task LivenessIsIndependentWhileReadinessRequiresConnectedSdk()
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
        var ready = await health.CheckHealthAsync(registration =>
            registration.Name == WorkerReadinessHealthCheck.ReadinessServiceName);

        Assert.Equal(
            [string.Empty, "briosa.liveness", "briosa.readiness"],
            mappings);
        Assert.Equal(HealthStatus.Healthy, liveness.Status);
        Assert.Equal(HealthStatus.Unhealthy, notReady.Status);
        Assert.Equal(HealthStatus.Healthy, ready.Status);
        Assert.Equal("briosa.liveness", WorkerReadinessHealthCheck.LivenessServiceName);
        Assert.Equal("briosa.readiness", WorkerReadinessHealthCheck.ReadinessServiceName);
    }

    [Fact]
    public void ServerInfoReportsSafeStateWithoutInventingConnectedVersion()
    {
        var service = new ServerDiscoveryService(
            new FakeWorkerStatusProvider(Snapshot(
                WorkerLifecycleState.Ready,
                WorkerConnectionState.Connected)),
            new FakeBuildIdentityProvider());

        var response = service.CreateServerInfo();

        Assert.Equal("0.1.0-test", response.Version.BriosaVersion);
        Assert.Equal("briosa.core.v1alpha1", response.Version.CoreProtocolPackage);
        Assert.Equal("2026.1.0529.7", response.Version.SpatialAnalyzerTarget);
        Assert.Equal(WorkerRuntimeState.Ready, response.WorkerState);
        Assert.Equal(
            SpatialAnalyzerConnectionState.Connected,
            response.SpatialAnalyzerConnectionState);
        Assert.True(response.ReadyForMp);
        Assert.False(response.HasConnectedSpatialAnalyzerVersion);
        Assert.Equal(
            ConnectedSpatialAnalyzerVersionState.Unavailable,
            response.ConnectedSpatialAnalyzerVersionState);
    }

    [Fact]
    public void CapabilitiesComeFromReviewedGeneratedCatalog()
    {
        var response = ServerDiscoveryService.CreateCapabilities();

        Assert.Equal("briosa.sa.2026.1.0529.7", response.CatalogId);
        Assert.Equal("1", response.CatalogRevision);
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
        Assert.Equal("1", coordinates.CatalogRevision);
        Assert.Equal(
            AssemblyServerBuildIdentityProvider.InteropFingerprint,
            coordinates.InteropFingerprint);
    }

    private static WorkerLifecycleSnapshot Snapshot(
        WorkerLifecycleState workerState,
        WorkerConnectionState? connectionState) =>
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
                    "sensitive-hostname",
                    StatusCode: 42,
                    Attempt: 1,
                    MaximumAttempts: 3,
                    "sensitive-connection-diagnostic",
                    DateTimeOffset.UtcNow),
            DateTimeOffset.UtcNow);

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
                CatalogRevision = "1",
                InteropFingerprint = "sha256:test"
            };
    }
}
