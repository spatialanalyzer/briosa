using System.Globalization;
using Briosa.Server.Operations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

// Covers the production host endpoint: a separate fake worker process, the real
// named pipe, NamedPipeWorkerProcess, the supervisor and the public HTTP/2
// endpoint. WorkerResponseBudgetTests builds its own receiver, so it cannot catch
// a host endpoint that reads worker frames under the wrong budget.
[Collection("Worker process lifecycle")]
public sealed class WorkerResponseBudgetProcessTests
{
    // Room for the fake worker's ExecutionResult envelope around the text.
    private const int EnvelopeAllowanceBytes = 4 * 1024;

    // A result just under the response budget arrives whole, and the same worker
    // generation serves the next call. While the request and response budgets are
    // equal, this shows only that the real endpoints deliver a near-budget frame.
    // Once the response budget is larger, the frame is also larger than the
    // request budget, so this test fails if the host endpoint bounds worker
    // frames by the request budget.
    [Fact]
    public async Task AResultNearTheResponseBudgetCrossesTheRealWorkerPipeWhole()
    {
        var length = WorkerControlProtocol.MaximumResponseBytes - EnvelopeAllowanceBytes;
        var supervisor = new WorkerProcessSupervisor(
            new NamedPipeWorkerProcessFactory(_ => CreateLaunch(length)),
            new WorkerLifecyclePolicy(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)),
            TestDurationClasses.SyntheticQuickPolicy(),
            identityPolicy: ExactTargetIdentityPolicy.CreateForTesting(
                SpatialAnalyzerApi.TargetVersion,
                activatedSdkVersion: SpatialAnalyzerApi.TargetVersion,
                connectedSpatialAnalyzerVersion: SpatialAnalyzerApi.TargetVersion));
        await using var supervisorLifetime = supervisor.ConfigureAwait(true);
        var started = await supervisor.StartAsync().ConfigureAwait(true);
        Assert.True(started.Succeeded, supervisor.Current.DiagnosticCode);
        var host = await GrpcTestHost.StartAsync<FileOperationsService>(supervisor).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.FileOperations.FileOperationsClient(host.Channel);

        var result = await client.GetWorkingDirectoryAsync(new Api.GetWorkingDirectoryRequest());
        var next = await client.GetWorkingDirectoryAsync(new Api.GetWorkingDirectoryRequest());

        Assert.Equal(length, result.Directory.Length);
        Assert.All(result.Directory, character => Assert.Equal('x', character));
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(2, result.Execution.MpResultCode);
        Assert.Equal(length, next.Directory.Length);
        Assert.Equal(started.Snapshot.Generation, supervisor.Current.Generation);
    }

    private static WorkerProcessLaunch CreateLaunch(int outputTextLength)
    {
        var executable = Path.Combine(
            AppContext.BaseDirectory,
            "worker-test-host",
            "Briosa.Worker.TestHost.exe");
        Assert.True(File.Exists(executable), $"The fake worker executable was not found at '{executable}'.");
        return new WorkerProcessLaunch(executable,
            ["--scenario", "normal", "--output-text-length", outputTextLength.ToString(CultureInfo.InvariantCulture)],
            Path.GetDirectoryName(executable));
    }
}
