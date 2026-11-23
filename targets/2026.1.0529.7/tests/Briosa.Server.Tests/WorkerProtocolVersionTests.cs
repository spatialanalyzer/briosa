using System.Globalization;
using Briosa.Server.Operations;
using Briosa.Server.Workers;
using Briosa.Worker.Control;

namespace Briosa.Server.Tests;

// The host and the worker ship together, but a mismatched pair must fail at
// startup, before any command runs: the host validates the private protocol
// version of the worker's Ready message. An older worker (offset -1) and a newer
// one (offset +1) both end in the typed startup failure.
[Collection("Worker process lifecycle")]
public sealed class WorkerProtocolVersionTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task AWorkerOnAnotherProtocolVersionFailsAtStartupBeforeAnyCommand(int offset)
    {
        var supervisor = new WorkerProcessSupervisor(
            new NamedPipeWorkerProcessFactory(_ => CreateLaunch(offset)),
            new WorkerLifecyclePolicy(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)),
            TestDurationClasses.SyntheticQuickPolicy(),
            identityPolicy: ExactTargetIdentityPolicy.CreateForTesting(
                SpatialAnalyzerApi.TargetVersion,
                activatedSdkVersion: SpatialAnalyzerApi.TargetVersion,
                connectedSpatialAnalyzerVersion: SpatialAnalyzerApi.TargetVersion));
        await using var supervisorLifetime = supervisor.ConfigureAwait(true);

        var started = await supervisor.StartAsync().ConfigureAwait(true);
        var execution = await supervisor.ExecuteAsync(
            new WorkerMpCommand("protocol-version", "Protocol Version", [], [])).ConfigureAwait(true);

        Assert.False(started.Succeeded);
        var current = supervisor.Current;
        Assert.Equal(WorkerLifecycleState.Degraded, current.State);
        Assert.Equal(WorkerLifecycleFailure.StartupFailed, current.LifecycleFailure);
        Assert.Equal("worker-startup-failed", current.DiagnosticCode);
        Assert.Equal(WorkerIncidentKind.StartFailed, current.LastIncident!.Kind);
        Assert.False(current.ReadyForExecution);
        Assert.Equal(WorkerExecutionStatus.Unavailable, execution.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, execution.ExecutionDisposition);
    }

    private static WorkerProcessLaunch CreateLaunch(int protocolVersionOffset)
    {
        var executable = Path.Combine(
            AppContext.BaseDirectory,
            "worker-test-host",
            "Briosa.Worker.TestHost.exe");
        Assert.True(File.Exists(executable), $"The fake worker executable was not found at '{executable}'.");
        return new WorkerProcessLaunch(executable,
            ["--scenario", "normal", "--protocol-version-offset",
                protocolVersionOffset.ToString(CultureInfo.InvariantCulture)],
            Path.GetDirectoryName(executable));
    }
}
