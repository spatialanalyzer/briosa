using Briosa.Worker.Control;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

public sealed class ActivatedSdkIdentityTests
{
    [Theory]
    [InlineData("2024, 1, 0508, 5", "2024.1.0508.5")]
    [InlineData("2026.1.0529.7", "2026.1.0529.7")]
    [InlineData(null, null)]
    [InlineData("2026.1.0529", null)]
    [InlineData("2024.1.0508.5 debug", null)]
    [InlineData("2026..0529.7", null)]
    public void ObservedVersionPreservesVendorBuildPaddingAndRejectsUncertainText(string? value, string? expected) =>
        Assert.Equal(expected, SpatialAnalyzerSdkProcessMonitor.NormalizeVersion(value));

    [Theory]
    [InlineData("2026.1.0529.7")]
    [InlineData(null)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The connection manager owns and disposes the test adapter on its STA.")]
    public async Task ActualSdkEvidenceCrossesTheWorkerBoundaryWithoutCopyingTheTarget(string? version)
    {
        var sdk = new IdentitySdk(version);
        var manager = new SdkConnectionManager("localhost", new(1, TimeSpan.Zero), () => sdk);
        await using var lifetime = manager.ConfigureAwait(false);
        var started = await manager.StartAsync();
        var connected = await manager.ConnectAsync();
        foreach (var snapshot in new[] { started, connected, await manager.ProbeLivenessAsync() })
        {
            var wire = WorkerControlHost.ToControlSnapshot(snapshot);
            Assert.Equal(version, wire.RuntimeIdentity!.ActivatedSdk.Version);
            Assert.Equal(version is null ? WorkerRuntimeIdentityEvidenceSource.Unavailable : WorkerRuntimeIdentityEvidenceSource.RuntimeVerified,
                wire.RuntimeIdentity.ActivatedSdk.Source);
            Assert.Null(wire.RuntimeIdentity.ConnectedSpatialAnalyzer.Version);
        }
        Assert.Equal(ApartmentState.STA, sdk.IdentityApartment);
        Assert.False(sdk.Executed);
    }

    private sealed class IdentitySdk(string? version) : ISpatialAnalyzerSdk
    {
        public ApartmentState IdentityApartment { get; private set; }
        public bool Executed { get; private set; }
        public string? GetActivatedSdkVersion() { IdentityApartment = Thread.CurrentThread.GetApartmentState(); return version; }
        public SdkLivenessStatus GetLiveness() => SdkLivenessStatus.Alive;
        public SdkConnectionResult Connect(string host) => new(SdkConnectionStatus.Connected, 0, null);
        public WorkerMpExecutionResult Execute(SdkCommand command) { Executed = true; throw new InvalidOperationException("No probe is authorized by identity observation."); }
        public void Dispose() { }
    }
}
