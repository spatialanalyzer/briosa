using System.Diagnostics;
using Briosa.Server.Services;

namespace Briosa.Server.Tests;

public sealed class SpatialAnalyzerLifecycleCoordinatorTests
{
    [Fact]
    public async Task StateDiscoveryReportsOneExternalExactTargetWithoutOwnership()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        platform.Observations.Add(new SpatialAnalyzerProcessObservation(
            new SpatialAnalyzerProcessIdentity(41, 4200)));
        using var coordinator = CreateCoordinator(executable.Path, platform);

        var state = await coordinator.GetCurrentAsync(CancellationToken.None);

        Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.Running, state.ApplicationState);
        Assert.Equal(global::Briosa.SpatialAnalyzerOwnership.External, state.Ownership);
        Assert.True(state.ApplicationGeneration > 0);
    }

    [Fact]
    public async Task StateDiscoveryFailsHonestWhenSeveralEligibleApplicationsExist()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        platform.Observations.AddRange(
        [
            new SpatialAnalyzerProcessObservation(
                new SpatialAnalyzerProcessIdentity(41, 4200)),
            new SpatialAnalyzerProcessObservation(
                new SpatialAnalyzerProcessIdentity(42, 4300))
        ]);
        using var coordinator = CreateCoordinator(executable.Path, platform);

        var state = await coordinator.GetCurrentAsync(CancellationToken.None);

        Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.Ambiguous, state.ApplicationState);
        Assert.Equal(global::Briosa.SpatialAnalyzerOwnership.None, state.Ownership);
        Assert.False(state.HasApplicationGeneration);
    }

    [Fact]
    public async Task LaunchUsesOnlyDocumentedQuickStartAndMinimizedArguments()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        using var coordinator = CreateCoordinator(executable.Path, platform);

        var state = await coordinator.LaunchAsync(
            new global::Briosa.LaunchSpatialAnalyzerRequest
            {
                QuickStartInstrumentName = "Leica AT960",
                StartMinimized = true
            },
            CancellationToken.None);

        Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.Running, state.ApplicationState);
        Assert.Equal(global::Briosa.SpatialAnalyzerOwnership.ServerLaunched, state.Ownership);
        Assert.Equal(["/QUICK", "Leica AT960", "-MINIMIZE"], platform.LastArguments);
        Assert.False(platform.LastUseShellExecute);
    }

    [Fact]
    public async Task RelativeJobPathIsRejectedAsInvalidArgument()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        using var coordinator = CreateCoordinator(executable.Path, platform);

        var exception = await Assert.ThrowsAsync<SpatialAnalyzerLifecycleException>(
            () => coordinator.LaunchAsync(
                new global::Briosa.LaunchSpatialAnalyzerRequest
                {
                    JobFilePath = "relative-job.xit"
                },
                CancellationToken.None));

        Assert.Equal(Grpc.Core.StatusCode.InvalidArgument, exception.StatusCode);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerLifecycleFailureKind.Validation,
            exception.Detail.Kind);
        Assert.Null(platform.StartedProcess);
    }

    [Fact]
    public async Task MissingAbsoluteJobPathIsReportedAsNotFound()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        using var coordinator = CreateCoordinator(executable.Path, platform);
        var missingJobPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"briosa-missing-{Guid.NewGuid():N}",
            "missing-job.xit");

        var exception = await Assert.ThrowsAsync<SpatialAnalyzerLifecycleException>(
            () => coordinator.LaunchAsync(
                new global::Briosa.LaunchSpatialAnalyzerRequest
                {
                    JobFilePath = missingJobPath
                },
                CancellationToken.None));

        Assert.Equal(Grpc.Core.StatusCode.NotFound, exception.StatusCode);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerLifecycleFailureKind.LaunchFailed,
            exception.Detail.Kind);
        Assert.Null(platform.StartedProcess);
    }

    [Fact]
    public async Task OwnedCloseRequiresStoppedSdkAndExactGeneration()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        var sdk = new FakeSdkStateProvider(global::Briosa.SpatialAnalyzerSdkState.Running);
        using var coordinator = CreateCoordinator(executable.Path, platform, sdk);
        var launched = await coordinator.LaunchAsync(
            new global::Briosa.LaunchSpatialAnalyzerRequest(),
            CancellationToken.None);

        var activeSdk = await Assert.ThrowsAsync<SpatialAnalyzerLifecycleException>(
            () => coordinator.CloseOwnedAsync(
                launched.ApplicationGeneration,
                CancellationToken.None));
        Assert.Equal(
            global::Briosa.SpatialAnalyzerLifecycleFailureKind.SdkNotStopped,
            activeSdk.Detail.Kind);

        sdk.Set(global::Briosa.SpatialAnalyzerSdkState.Stopped);
        var stale = await Assert.ThrowsAsync<SpatialAnalyzerLifecycleException>(
            () => coordinator.CloseOwnedAsync(
                launched.ApplicationGeneration + 1,
                CancellationToken.None));
        Assert.Equal(Grpc.Core.StatusCode.Aborted, stale.StatusCode);

        var closed = await coordinator.CloseOwnedAsync(
            launched.ApplicationGeneration,
            CancellationToken.None);
        Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.NotRunning, closed.ApplicationState);
        Assert.Equal(global::Briosa.SpatialAnalyzerOwnership.None, closed.Ownership);
        Assert.False(closed.HasApplicationGeneration);
        Assert.True(platform.StartedProcess!.CloseRequested);
    }

    [Fact]
    public async Task LaunchNeverClosesOrReplacesAnExternalApplication()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        platform.Observations.Add(new SpatialAnalyzerProcessObservation(
            new SpatialAnalyzerProcessIdentity(41, 4200)));
        using var coordinator = CreateCoordinator(executable.Path, platform);

        var exception = await Assert.ThrowsAsync<SpatialAnalyzerLifecycleException>(
            () => coordinator.LaunchAsync(
                new global::Briosa.LaunchSpatialAnalyzerRequest(),
                CancellationToken.None));

        Assert.Equal(Grpc.Core.StatusCode.FailedPrecondition, exception.StatusCode);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerLifecycleFailureKind.StateConflict,
            exception.Detail.Kind);
        Assert.Null(platform.StartedProcess);
    }

    private static SpatialAnalyzerLifecycleCoordinator CreateCoordinator(
        string executablePath,
        FakeProcessPlatform platform,
        FakeSdkStateProvider? sdk = null) =>
        new(
            new SpatialAnalyzerApplicationOptions(
                executablePath,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1)),
            platform,
            sdk ?? new FakeSdkStateProvider(global::Briosa.SpatialAnalyzerSdkState.Stopped),
            TimeProvider.System);

    private sealed class FakeSdkStateProvider(
        global::Briosa.SpatialAnalyzerSdkState sdkState)
        : ISpatialAnalyzerSdkLifecycleStateProvider
    {
        public global::Briosa.SpatialAnalyzerSdkLifecycleState Current { get; private set; } =
            new()
            {
                SdkState = sdkState,
                ConnectionState = global::Briosa.SpatialAnalyzerConnectionState.Disconnected,
                ExecutionReadinessState =
                    global::Briosa.SpatialAnalyzerExecutionReadinessState.Unverified
            };

        public void Set(global::Briosa.SpatialAnalyzerSdkState sdkState)
        {
            var state = Current.Clone();
            state.SdkState = sdkState;
            Current = state;
        }
    }

    private sealed class FakeProcessPlatform : ISpatialAnalyzerProcessPlatform
    {
        public List<SpatialAnalyzerProcessObservation> Observations { get; } = [];

        public FakeOwnedProcess? StartedProcess { get; private set; }

        public IReadOnlyList<string> LastArguments { get; private set; } = [];

        public bool LastUseShellExecute { get; private set; }

        public IReadOnlyList<SpatialAnalyzerProcessObservation> ObserveEligibleProcesses(
            string executablePath) => Observations;

        public ISpatialAnalyzerOwnedProcess Start(ProcessStartInfo startInfo)
        {
            LastArguments = [.. startInfo.ArgumentList];
            LastUseShellExecute = startInfo.UseShellExecute;
            StartedProcess = new FakeOwnedProcess();
            return StartedProcess;
        }
    }

    private sealed class FakeOwnedProcess : ISpatialAnalyzerOwnedProcess
    {
        public SpatialAnalyzerProcessIdentity Identity { get; } = new(51, 5200);

        public bool HasExited { get; private set; }

        public bool IsApplicationWindowReady => !HasExited;

        public bool CloseRequested { get; private set; }

        public bool RequestClose()
        {
            CloseRequested = true;
            HasExited = true;
            return true;
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public void Refresh()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class TemporaryFile : IDisposable
    {
        private TemporaryFile(string path) => Path = path;

        public string Path { get; }

        public static TemporaryFile Create() => new(System.IO.Path.GetTempFileName());

        public void Dispose() => File.Delete(Path);
    }
}
