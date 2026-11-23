using System.Globalization;
using Briosa.Server.Operations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Services;
using Grpc.Core;
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
    // generation serves the next call. The frame is larger than the request
    // budget, so this test fails if the host endpoint bounds worker frames by the
    // request budget.
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

    // A getter fault with two retrieved Text outputs of 530,000 and 540,000
    // characters, through a separate fake worker, the real pipe, the supervisor and
    // HTTP/2. Together they exceed the 1 MiB budget, so the fallback drops the
    // larger value; the smaller one now fits and is reported as retrieved. With the
    // former 64 KiB budget both were dropped (6 of 9 outputs retrieved); now 7 are.
    // Everything else is the fault outcome it has always been.
    [Fact]
    public async Task AnSdkGetterFaultReportsEveryRetrievedOutputThatFitsTheBudget()
    {
        var supervisor = new WorkerProcessSupervisor(
            new NamedPipeWorkerProcessFactory(_ => CreateLaunch(
                "--sdk-fault-output", "Humidity (% RH)", "--output-text-lengths", "530000,540000")),
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
        var host = await GrpcTestHost.StartAsync<InstrumentOperationsService>(supervisor).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(host.Channel);
        var request = new Api.GetObservationInfoRequest
        {
            Point = new Api.PointName { CollectionName = "A", GroupName = "G", TargetName = "P" },
            ObservationIndex = 0
        };

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetObservationInfoAsync(request)).ConfigureAwait(true);
        var next = await client.GetObservationInfoAsync(request);

        Assert.Equal(StatusCode.DataLoss, exception.StatusCode);
        var error = Api.OperationError.Parser.ParseFrom(Assert.Single(
            exception.Trailers, entry => entry.Key == GrpcOperationOutcomeMapper.ErrorTrailerName).ValueBytes);
        Assert.Equal(Api.OperationFailureKind.SdkCallFaulted, error.Kind);
        Assert.Equal("sdk-output-getter-faulted", error.DiagnosticCode);
        Assert.Equal(Api.ExecutionDisposition.Completed, error.ExecutionDisposition);
        Assert.Equal(Api.ReplayGuidance.DoNotReplay, error.ReplayGuidance);
        Assert.Equal(Api.RecoveryGuidance.None, error.RecoveryGuidance);
        Assert.Equal(Api.MpExecutionState.Succeeded, error.MpExecution.State);
        Assert.Equal(2, error.MpExecution.MpResultCode);
        var retrievals = error.MpExecution.OutputRetrievals;
        Assert.Equal(9, retrievals.Count);
        // Resulting Instrument through RMS Error and the other readings, including
        // the 530,000-character Timestamp, arrive as retrieved.
        Assert.All(retrievals.Take(7), retrieval =>
        {
            Assert.Equal(Api.OutputRetrievalState.Retrieved, retrieval.State);
            Assert.False(retrieval.HasDiagnosticCode);
        });
        Assert.Equal(Api.OutputRetrievalState.Failed, retrievals[7].State);
        Assert.Equal("sdk-output-getter-faulted", retrievals[7].DiagnosticCode);
        Assert.Equal(Api.OutputRetrievalState.Failed, retrievals[8].State);
        Assert.Equal(WorkerExecutionDelivery.OutputEncodingRejected, retrievals[8].DiagnosticCode);
        Assert.Equal(Api.MpExecutionState.Succeeded, next.Execution.State);
        Assert.Equal(started.Snapshot.Generation, supervisor.Current.Generation);
    }

    private static WorkerProcessLaunch CreateLaunch(int outputTextLength) =>
        CreateLaunch("--output-text-length", outputTextLength.ToString(CultureInfo.InvariantCulture));

    private static WorkerProcessLaunch CreateLaunch(params string[] options)
    {
        var executable = Path.Combine(
            AppContext.BaseDirectory,
            "worker-test-host",
            "Briosa.Worker.TestHost.exe");
        Assert.True(File.Exists(executable), $"The fake worker executable was not found at '{executable}'.");
        return new WorkerProcessLaunch(executable, ["--scenario", "normal", .. options],
            Path.GetDirectoryName(executable));
    }
}
