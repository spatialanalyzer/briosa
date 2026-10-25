using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Briosa.Server.Operations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static Briosa.Server.Tests.InteractionModeApplicationTests;

namespace Briosa.Server.Tests;

// #313: when an exchange stalls, the supervisor checks whether SpatialAnalyzer has a
// modal dialog open and reports the typed, value-free sa-modal-dialog-open
// diagnostic instead of a generic timeout. It reports only and never dismisses.
public sealed class ModalDialogDetectionTests
{
    private const int SpatialAnalyzerProcessId = 4100;
    private const int OtherProcessId = 4200;
    private static readonly int[] SpatialAnalyzerOnly = [SpatialAnalyzerProcessId];
    private static readonly string[] ReadOnlyWindowQueries =
        ["EnumWindows", "GetClassNameW", "GetWindow", "GetWindowThreadProcessId", "IsWindowEnabled", "IsWindowVisible"];

    [Fact]
    public async Task WatchdogWithAModalDialogOpenReportsIt()
    {
        var clock = new HeartbeatTestClock();
        var probe = new FixedModalDialogProbe(SpatialAnalyzerModalDialogState.ModalDialogOpen);
        var process = new ScriptedWorkerProcess(_ => null);
        await using var supervisor = CreateSupervisor(
            new ScriptedWorkerProcessFactory(_ => process), SpatialAnalyzerInteractionMode.Unchanged, clock,
            modalDialogProbe: probe);
        Assert.True((await supervisor.StartAsync().WaitAsync(HangGuard)).Succeeded, supervisor.Current.DiagnosticCode);
        Assert.Equal(0, probe.Calls);

        var executing = supervisor.ExecuteAsync(CreateCommand());
        await process.Held.WaitAsync(HangGuard);
        await clock.FireNextAsync(ExecutionWatchdogTimeout).WaitAsync(HangGuard);
        var outcome = await executing.WaitAsync(HangGuard);

        Assert.Equal(WorkerExecutionStatus.WatchdogTimeout, outcome.Status);
        Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown, outcome.ExecutionDisposition);
        Assert.Equal("sa-modal-dialog-open", outcome.DiagnosticCode);
        Assert.Equal(1, probe.Calls);
        Assert.True(process.HasExited);
        var degraded = supervisor.Current;
        Assert.Equal(WorkerLifecycleState.Degraded, degraded.State);
        Assert.Equal("sa-modal-dialog-open", degraded.DiagnosticCode);
        Assert.Equal(WorkerIncidentKind.WatchdogTerminated, degraded.LastIncident!.Kind);
        Assert.Equal("sa-modal-dialog-open", degraded.LastIncident.DiagnosticCode);
    }

    [Theory]
    [InlineData((int)SpatialAnalyzerModalDialogState.NoModalDialog)]
    [InlineData((int)SpatialAnalyzerModalDialogState.NotObserved)]
    public async Task WatchdogWithoutAModalDialogKeepsTheGenericDiagnostic(int state)
    {
        var clock = new HeartbeatTestClock();
        var probe = new FixedModalDialogProbe((SpatialAnalyzerModalDialogState)state);
        var process = new ScriptedWorkerProcess(_ => null);
        await using var supervisor = CreateSupervisor(
            new ScriptedWorkerProcessFactory(_ => process), SpatialAnalyzerInteractionMode.Unchanged, clock,
            modalDialogProbe: probe);
        Assert.True((await supervisor.StartAsync().WaitAsync(HangGuard)).Succeeded, supervisor.Current.DiagnosticCode);

        var executing = supervisor.ExecuteAsync(CreateCommand());
        await process.Held.WaitAsync(HangGuard);
        await clock.FireNextAsync(ExecutionWatchdogTimeout).WaitAsync(HangGuard);
        var outcome = await executing.WaitAsync(HangGuard);

        Assert.Equal(WorkerExecutionStatus.WatchdogTimeout, outcome.Status);
        Assert.Equal("worker-execution-watchdog-timeout", outcome.DiagnosticCode);
        Assert.Equal(1, probe.Calls);
    }

    [Fact]
    public async Task AFailedDialogCheckKeepsTheGenericDiagnostic()
    {
        var clock = new HeartbeatTestClock();
        var process = new ScriptedWorkerProcess(_ => null);
        await using var supervisor = CreateSupervisor(
            new ScriptedWorkerProcessFactory(_ => process), SpatialAnalyzerInteractionMode.Unchanged, clock,
            modalDialogProbe: new ThrowingModalDialogProbe());
        Assert.True((await supervisor.StartAsync().WaitAsync(HangGuard)).Succeeded, supervisor.Current.DiagnosticCode);

        var executing = supervisor.ExecuteAsync(CreateCommand());
        await process.Held.WaitAsync(HangGuard);
        await clock.FireNextAsync(ExecutionWatchdogTimeout).WaitAsync(HangGuard);
        var outcome = await executing.WaitAsync(HangGuard);

        // The check is advisory: its failure neither escapes nor changes the outcome.
        Assert.Equal(WorkerExecutionStatus.WatchdogTimeout, outcome.Status);
        Assert.Equal("worker-execution-watchdog-timeout", outcome.DiagnosticCode);
        Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
        Assert.True(process.HasExited);
    }

    [Fact]
    public async Task ReadinessProbeHeldByAModalDialogRequiresOperatorRecovery()
    {
        var clock = new HeartbeatTestClock();
        var probe = new FixedModalDialogProbe(SpatialAnalyzerModalDialogState.ModalDialogOpen);
        var process = new ScriptedWorkerProcess(holdVerification: true);
        await using var supervisor = CreateSupervisor(
            new ScriptedWorkerProcessFactory(_ => process), SpatialAnalyzerInteractionMode.Unchanged, clock,
            modalDialogProbe: probe);

        var starting = supervisor.StartAsync();
        await process.Held.WaitAsync(HangGuard);
        await clock.FireNextAsync(ReadinessProbeTimeout).WaitAsync(HangGuard);

        Assert.False((await starting.WaitAsync(HangGuard)).Succeeded);
        Assert.Equal("sa-modal-dialog-open", supervisor.Current.DiagnosticCode);
        Assert.Equal(WorkerExecutionReadinessState.OperatorRecoveryRequired,
            supervisor.Current.Connection!.ExecutionReadinessState);
        // A dialog explains the stall, so no competing SDK client is suspected.
        Assert.DoesNotContain(supervisor.History, snapshot =>
            snapshot.Connection?.ExecutionReadinessState == WorkerExecutionReadinessState.CompetingClientSuspected);
    }

    [Fact]
    public async Task ReadinessProbeHeldWithoutADialogStillSuspectsACompetingClient()
    {
        var clock = new HeartbeatTestClock();
        var probe = new FixedModalDialogProbe(SpatialAnalyzerModalDialogState.NoModalDialog);
        var process = new ScriptedWorkerProcess(holdVerification: true);
        await using var supervisor = CreateSupervisor(
            new ScriptedWorkerProcessFactory(_ => process), SpatialAnalyzerInteractionMode.Unchanged, clock,
            modalDialogProbe: probe);

        var starting = supervisor.StartAsync();
        await process.Held.WaitAsync(HangGuard);
        await clock.FireNextAsync(ReadinessProbeTimeout).WaitAsync(HangGuard);

        Assert.False((await starting.WaitAsync(HangGuard)).Succeeded);
        Assert.Equal("execution-readiness-probe-timeout", supervisor.Current.DiagnosticCode);
        Assert.Contains(supervisor.History, snapshot =>
            snapshot.Connection?.ExecutionReadinessState == WorkerExecutionReadinessState.CompetingClientSuspected);
        Assert.Equal(1, probe.Calls);
    }

    [Fact]
    public async Task InteractionModeStepHeldByAModalDialogReportsIt()
    {
        var clock = new HeartbeatTestClock();
        var probe = new FixedModalDialogProbe(SpatialAnalyzerModalDialogState.ModalDialogOpen);
        var process = new ScriptedWorkerProcess(_ => null);
        await using var supervisor = CreateSupervisor(
            new ScriptedWorkerProcessFactory(_ => process), SpatialAnalyzerInteractionMode.Silent, clock,
            modalDialogProbe: probe);

        var starting = supervisor.StartAsync();
        await process.Held.WaitAsync(HangGuard);
        await clock.FireNextAsync(ReadinessProbeTimeout).WaitAsync(HangGuard);

        Assert.False((await starting.WaitAsync(HangGuard)).Succeeded);
        Assert.Equal("sa-modal-dialog-open", supervisor.Current.DiagnosticCode);
        Assert.Equal(WorkerExecutionReadinessState.OperatorRecoveryRequired,
            supervisor.Current.Connection!.ExecutionReadinessState);
    }

    [Fact]
    public void ModalDialogWatchdogTellsTheCallerAnOperatorMustAct()
    {
        var modal = Error(Assert.Throws<RpcException>(() => RequireSuccess("sa-modal-dialog-open")));
        var generic = Error(Assert.Throws<RpcException>(() => RequireSuccess("worker-execution-watchdog-timeout")));

        Assert.Equal(OperationFailureKind.WorkerWatchdogTimeout, modal.Kind);
        Assert.Equal("sa-modal-dialog-open", modal.DiagnosticCode);
        Assert.Equal(ExecutionDisposition.StartedOutcomeUnknown, modal.ExecutionDisposition);
        Assert.Equal(RecoveryGuidance.OperatorInterventionRequired, modal.RecoveryGuidance);
        Assert.Equal(ReplayGuidance.ReconcileBeforeReplay, modal.ReplayGuidance);
        Assert.Equal(RecoveryGuidance.WorkerReplacement, generic.RecoveryGuidance);

        static SuccessfulOperationExecution RequireSuccess(string diagnosticCode) =>
            GrpcOperationOutcomeMapper.RequireSuccess(
                new WorkerExecutionOutcome(WorkerExecutionStatus.WatchdogTimeout,
                    WorkerExecutionDisposition.StartedOutcomeUnknown, Execution: null, Connection: null,
                    diagnosticCode, Generation: 1),
                "file_operations.get_working_directory", ReplaySafety.Unknown, [], callerDeadlineExceeded: false);

        static OperationError Error(RpcException exception) => OperationError.Parser.ParseFrom(
            Assert.Single(exception.Trailers, item => item.Key == GrpcOperationOutcomeMapper.ErrorTrailerName)
                .ValueBytes);
    }

    [Fact]
    public async Task HostRegistrationGivesTheSupervisorTheProbe()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddSpatialAnalyzerLifecycle(configuration);
        services.AddWorkerProcessLifecycle(configuration);
        await using var provider = services.BuildServiceProvider();

        Assert.IsType<SpatialAnalyzerModalDialogProbe>(
            provider.GetRequiredService<ISpatialAnalyzerModalDialogProbe>());
        Assert.Same(
            provider.GetRequiredService<ISpatialAnalyzerModalDialogProbe>(),
            provider.GetRequiredService<WorkerProcessSupervisor>().ModalDialogProbe);
    }

    [Fact]
    public void ModalDialogIsAVisibleStandardDialogWhoseSpatialAnalyzerOwnerIsDisabled()
    {
        var probe = CreateProbe(
            [SpatialAnalyzerProcessId],
            new SpatialAnalyzerTopLevelWindow(SpatialAnalyzerProcessId, Visible: true, "Afx:main", null, OwnerEnabled: false),
            new SpatialAnalyzerTopLevelWindow(SpatialAnalyzerProcessId, Visible: true, "#32770",
                SpatialAnalyzerProcessId, OwnerEnabled: false));

        Assert.Equal(SpatialAnalyzerModalDialogState.ModalDialogOpen, probe.Observe());
    }

    [Theory]
    // A modeless dialog leaves its owner enabled.
    [InlineData(true, "#32770", SpatialAnalyzerProcessId, true)]
    // A hidden dialog cannot be what blocks the operator.
    [InlineData(false, "#32770", SpatialAnalyzerProcessId, false)]
    // Another window class, such as a tool or progress window.
    [InlineData(true, "Afx:tool", SpatialAnalyzerProcessId, false)]
    // An unowned dialog disables nothing.
    [InlineData(true, "#32770", null, false)]
    // A dialog owned by another process's window.
    [InlineData(true, "#32770", OtherProcessId, false)]
    public void OtherWindowsAreNotModalDialogs(bool visible, string className, int? ownerProcessId, bool ownerEnabled)
    {
        var probe = CreateProbe(
            [SpatialAnalyzerProcessId],
            new SpatialAnalyzerTopLevelWindow(SpatialAnalyzerProcessId, visible, className, ownerProcessId, ownerEnabled));

        Assert.Equal(SpatialAnalyzerModalDialogState.NoModalDialog, probe.Observe());
    }

    [Fact]
    public void OnlyEligibleSpatialAnalyzerProcessesAreAsked()
    {
        var windows = new RecordingWindowSource(new SpatialAnalyzerTopLevelWindow(
            OtherProcessId, Visible: true, "#32770", OtherProcessId, OwnerEnabled: false));
        var probe = new SpatialAnalyzerModalDialogProbe(Options, new FixedProcessPlatform([SpatialAnalyzerProcessId]), windows);

        Assert.Equal(SpatialAnalyzerModalDialogState.NoModalDialog, probe.Observe());
        Assert.Equal(SpatialAnalyzerOnly, Assert.Single(windows.Requests).Order());
    }

    [Fact]
    public void NoEligibleProcessIsNotAnObservation()
    {
        var windows = new RecordingWindowSource();
        var probe = new SpatialAnalyzerModalDialogProbe(Options, new FixedProcessPlatform([]), windows);

        Assert.Equal(SpatialAnalyzerModalDialogState.NotObserved, probe.Observe());
        Assert.Empty(windows.Requests);
    }

    [Fact]
    public void UnreadableWindowsAreNotAnObservation()
    {
        var probe = new SpatialAnalyzerModalDialogProbe(
            Options, new FixedProcessPlatform([SpatialAnalyzerProcessId]), new ThrowingWindowSource());

        Assert.Equal(SpatialAnalyzerModalDialogState.NotObserved, probe.Observe());
    }

    [Fact]
    public void WindowSourceImportsOnlyReadOnlyWindowQueries()
    {
        // Never GetWindowText, SendMessage, PostMessage, EndDialog or input: the probe
        // reads ownership, visibility, enabled state and class only.
        var imports = typeof(WindowsSpatialAnalyzerWindowSource).GetNestedTypes(BindingFlags.NonPublic)
            .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(method => method.GetCustomAttribute<DllImportAttribute>() is not null)
            .Select(method => method.GetCustomAttribute<DllImportAttribute>()!.EntryPoint ?? method.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ReadOnlyWindowQueries, imports);
    }

    [Fact]
    public void WindowSourceReadsThisDesktop()
    {
        using var current = Process.GetCurrentProcess();
        var windows = new WindowsSpatialAnalyzerWindowSource().GetTopLevelWindows(new HashSet<int> { current.Id });

        Assert.All(windows, window => Assert.Equal(current.Id, window.ProcessId));
    }

    private static readonly SpatialAnalyzerApplicationOptions Options = new(
        @"C:\SA\x64\Spatial Analyzer64.exe", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

    private static SpatialAnalyzerModalDialogProbe CreateProbe(
        IReadOnlyList<int> processIds, params SpatialAnalyzerTopLevelWindow[] windows) =>
        new(Options, new FixedProcessPlatform(processIds), new RecordingWindowSource(windows));

    private sealed class FixedModalDialogProbe(SpatialAnalyzerModalDialogState state) : ISpatialAnalyzerModalDialogProbe
    {
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public SpatialAnalyzerModalDialogState Observe()
        {
            Interlocked.Increment(ref _calls);
            return state;
        }
    }

    private sealed class ThrowingModalDialogProbe : ISpatialAnalyzerModalDialogProbe
    {
        public SpatialAnalyzerModalDialogState Observe() => throw new UnauthorizedAccessException();
    }

    private sealed class FixedProcessPlatform(IReadOnlyList<int> processIds) : ISpatialAnalyzerProcessPlatform
    {
        public IReadOnlyList<SpatialAnalyzerProcessObservation> ObserveEligibleProcesses(string executablePath)
        {
            Assert.Equal(Options.ExecutablePath, executablePath);
            return [.. processIds.Select(id => new SpatialAnalyzerProcessObservation(new SpatialAnalyzerProcessIdentity(id, 1)))];
        }

        public SpatialAnalyzerProcessPresence ObserveProcess(string executablePath, SpatialAnalyzerProcessIdentity identity) =>
            throw new NotSupportedException();

        public ISpatialAnalyzerOwnedProcess Start(ProcessStartInfo startInfo) => throw new NotSupportedException();
    }

    private sealed class RecordingWindowSource(params SpatialAnalyzerTopLevelWindow[] windows) : ISpatialAnalyzerWindowSource
    {
        public List<IReadOnlySet<int>> Requests { get; } = [];

        public IReadOnlyList<SpatialAnalyzerTopLevelWindow> GetTopLevelWindows(IReadOnlySet<int> processIds)
        {
            Requests.Add(processIds);
            return windows;
        }
    }

    private sealed class ThrowingWindowSource : ISpatialAnalyzerWindowSource
    {
        public IReadOnlyList<SpatialAnalyzerTopLevelWindow> GetTopLevelWindows(IReadOnlySet<int> processIds) =>
            throw new Win32Exception(5);
    }
}
