using Briosa.Server.Services;
using Briosa.Server.Workers;

namespace Briosa.Server.Tests;

public sealed class WorkerIncidentProjectionTests
{
    [Theory]
    [InlineData((int)WorkerIncidentKind.StartFailed, SpatialAnalyzerSdkTerminationKind.StartFailed)]
    [InlineData((int)WorkerIncidentKind.SdkProcessExited, SpatialAnalyzerSdkTerminationKind.SdkProcessExited)]
    [InlineData((int)WorkerIncidentKind.WatchdogTerminated, SpatialAnalyzerSdkTerminationKind.WatchdogTerminated)]
    [InlineData((int)WorkerIncidentKind.SdkConnectionLost, SpatialAnalyzerSdkTerminationKind.SdkConnectionLost)]
    [InlineData((int)WorkerIncidentKind.WorkerProcessExited, SpatialAnalyzerSdkTerminationKind.WorkerProcessExited)]
    [InlineData((int)WorkerIncidentKind.ControlChannelLost, SpatialAnalyzerSdkTerminationKind.ControlChannelLost)]
    public void IncidentClassificationDoesNotInterpretDiagnosticText(int kind, SpatialAnalyzerSdkTerminationKind expected)
    {
        const string misleading = "activation-startup-watchdog-connection-sdk-process-exited";
        var incident = new WorkerIncidentSnapshot(1, WorkerTerminationKind.Forced,
            WorkerExecutionDisposition.StartedOutcomeUnknown, "test", misleading, (WorkerIncidentKind)kind);
        var snapshot = new WorkerLifecycleSnapshot(WorkerLifecycleState.Degraded, 1, null, 0,
            WorkerTerminationKind.Forced, misleading, null, DateTimeOffset.UnixEpoch, LastIncident: incident);

        var state = SpatialAnalyzerSdkLifecycleStateProjection.ToPublicState(snapshot);

        Assert.Equal(expected, state.LastIncident.TerminationKind);
        Assert.Equal(misleading, state.LastIncident.DiagnosticCode);
        Assert.Equal(ExecutionDisposition.StartedOutcomeUnknown, state.LastIncident.ExecutionDisposition);
        Assert.False(state.ReadyForMp);
    }
}
