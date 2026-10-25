using System.Collections.Concurrent;
using Briosa.Server.Operations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.Logging;

namespace Briosa.Server.Tests;

// #313: Briosa:SpatialAnalyzer:InteractionMode is applied through Set Interaction Mode
// after every successful Connect, before readiness is published, and is audited.
public sealed class InteractionModeApplicationTests
{
    internal static readonly TimeSpan HangGuard = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan ReadinessProbeTimeout = TimeSpan.FromSeconds(7);
    internal static readonly TimeSpan ExecutionWatchdogTimeout = TimeSpan.FromSeconds(13);

    [Fact]
    public async Task UnchangedSendsNoInteractionModeCommand()
    {
        var factory = new ScriptedWorkerProcessFactory(_ => new ScriptedWorkerProcess());
        await using var supervisor = CreateSupervisor(factory, SpatialAnalyzerInteractionMode.Unchanged);

        Assert.True((await supervisor.StartAsync().WaitAsync(HangGuard)).Succeeded, supervisor.Current.DiagnosticCode);

        Assert.True(supervisor.Current.ReadyForExecution);
        Assert.Empty(factory.Generations[1].Commands);
    }

    [Theory]
    [InlineData("Silent", WorkerSaInteractionModeValue.Silent)]
    [InlineData("Automatic", WorkerSaInteractionModeValue.Automatic)]
    [InlineData("Manual", WorkerSaInteractionModeValue.Manual)]
    public async Task ConfiguredModeIsSetAfterTheProbeAndBeforeReadiness(
        string setting, WorkerSaInteractionModeValue expected)
    {
        var mode = Enum.Parse<SpatialAnalyzerInteractionMode>(setting);
        WorkerLifecycleSnapshot? atExecute = null;
        WorkerProcessSupervisor? observed = null;
        var factory = new ScriptedWorkerProcessFactory(_ => new ScriptedWorkerProcess
        {
            OnExecute = _ => atExecute = observed!.Current
        });
        await using var supervisor = CreateSupervisor(factory, mode);
        observed = supervisor;

        Assert.True((await supervisor.StartAsync().WaitAsync(HangGuard)).Succeeded, supervisor.Current.DiagnosticCode);

        var process = factory.Generations[1];
        var command = Assert.Single(process.Commands);
        Assert.Equal("utility_operations.set_interaction_mode", command.OperationId);
        Assert.Equal("Set Interaction Mode", command.StepName);
        Assert.Empty(command.OutputArguments);
        Assert.Collection(command.InputArguments,
            argument => Assert.Equal(
                (WorkerMpValue)new WorkerChoiceValue<WorkerSaInteractionModeValue>(expected), argument.Value),
            argument => Assert.Equal(
                (WorkerMpValue)new WorkerChoiceValue<WorkerMpInteractionModeValue>(
                    WorkerMpInteractionModeValue.HaltOnFailureOrPartialSuccess), argument.Value),
            argument => Assert.Equal(
                (WorkerMpValue)new WorkerChoiceValue<WorkerMpDialogInteractionModeValue>(
                    WorkerMpDialogInteractionModeValue.AllowApplicationInteraction), argument.Value));
        Assert.Equal(
            new[] { WorkerControlMessageKind.VerifyExecution, WorkerControlMessageKind.Execute },
            process.Received.Where(kind => kind != WorkerControlMessageKind.Ping));
        Assert.NotNull(atExecute);
        Assert.Equal(WorkerLifecycleState.Starting, atExecute.State);
        Assert.False(atExecute.ReadyForExecution);
        Assert.True(supervisor.Current.ReadyForExecution);
        Assert.Equal("worker-execution-ready", supervisor.Current.DiagnosticCode);
    }

    [Fact]
    public async Task ModeIsSetByAnExplicitConnectToo()
    {
        var factory = new ScriptedWorkerProcessFactory(_ => new ScriptedWorkerProcess(connectedAtReady: false));
        await using var supervisor = CreateSupervisor(factory, SpatialAnalyzerInteractionMode.Silent);

        Assert.True((await supervisor.StartAsync().WaitAsync(HangGuard)).Succeeded, supervisor.Current.DiagnosticCode);
        Assert.Empty(factory.Generations[1].Commands);
        Assert.True((await supervisor.ConnectAsync(supervisor.Current.Generation).WaitAsync(HangGuard)).Succeeded,
            supervisor.Current.DiagnosticCode);

        var command = Assert.Single(factory.Generations[1].Commands);
        Assert.Equal("utility_operations.set_interaction_mode", command.OperationId);
        Assert.True(supervisor.Current.ReadyForExecution);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ModeThatSaDoesNotAcceptKeepsReadinessClosed(int mpResultCode)
    {
        var factory = new ScriptedWorkerProcessFactory(_ => new ScriptedWorkerProcess(
            _ => ScriptedWorkerProcess.Completed(mpResultCode)));
        await using var supervisor = CreateSupervisor(factory, SpatialAnalyzerInteractionMode.Silent);

        Assert.False((await supervisor.StartAsync().WaitAsync(HangGuard)).Succeeded);
        var blocked = await supervisor.ExecuteAsync(CreateCommand()).WaitAsync(HangGuard);

        AssertQuarantined(supervisor, "interaction-mode-not-applied");
        Assert.True(factory.Generations[1].HasExited);
        Assert.Single(factory.Generations[1].Commands);
        Assert.Equal(WorkerExecutionStatus.Unavailable, blocked.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, blocked.ExecutionDisposition);
    }

    [Fact]
    public async Task RejectedModeKeepsReadinessClosed()
    {
        var factory = new ScriptedWorkerProcessFactory(_ => new ScriptedWorkerProcess(
            _ => new WorkerExecuteRejected(DurationMilliseconds: 1, DiagnosticCode: "sdk-execute-rejected")));
        await using var supervisor = CreateSupervisor(factory, SpatialAnalyzerInteractionMode.Silent);

        Assert.False((await supervisor.StartAsync().WaitAsync(HangGuard)).Succeeded);

        AssertQuarantined(supervisor, "interaction-mode-not-applied");
    }

    [Fact]
    public async Task RecoveryAppliesTheModeToTheNewGeneration()
    {
        var factory = new ScriptedWorkerProcessFactory(generation => new ScriptedWorkerProcess(
            _ => ScriptedWorkerProcess.Completed(generation == 1 ? 3 : 2)));
        await using var supervisor = CreateSupervisor(factory, SpatialAnalyzerInteractionMode.Silent);

        Assert.False((await supervisor.StartAsync().WaitAsync(HangGuard)).Succeeded);
        Assert.True((await supervisor.RecoverSdkAsync(supervisor.Current.Generation).WaitAsync(HangGuard)).Succeeded,
            supervisor.Current.DiagnosticCode);

        Assert.Single(factory.Generations[1].Commands);
        Assert.Equal("utility_operations.set_interaction_mode",
            Assert.Single(factory.Generations[2].Commands).OperationId);
        Assert.True(supervisor.Current.ReadyForExecution);
        Assert.Equal(2, supervisor.Current.Generation);
    }

    [Fact]
    public async Task ModeStepIsBoundedByTheReadinessProbeTimeout()
    {
        var clock = new HeartbeatTestClock();
        var process = new ScriptedWorkerProcess(_ => null);
        await using var supervisor = CreateSupervisor(
            new ScriptedWorkerProcessFactory(_ => process), SpatialAnalyzerInteractionMode.Silent, clock);

        var starting = supervisor.StartAsync();
        await process.Held.WaitAsync(HangGuard);
        await clock.FireNextAsync(ReadinessProbeTimeout).WaitAsync(HangGuard);

        Assert.False((await starting.WaitAsync(HangGuard)).Succeeded);
        AssertQuarantined(supervisor, "interaction-mode-apply-timeout");
        Assert.True(process.HasExited);
    }

    [Fact]
    public async Task AppliedAndRefusedModesAreAudited()
    {
        var logger = new CapturingLogger();
        var factory = new ScriptedWorkerProcessFactory(generation => new ScriptedWorkerProcess(
            _ => ScriptedWorkerProcess.Completed(generation == 1 ? 3 : 2)));
        await using var supervisor = CreateSupervisor(factory, SpatialAnalyzerInteractionMode.Silent, logger: logger);

        await supervisor.StartAsync().WaitAsync(HangGuard);
        await supervisor.RecoverSdkAsync(supervisor.Current.Generation).WaitAsync(HangGuard);

        var audits = logger.Entries.Where(entry => entry.EventId.Id == 1202).ToArray();
        Assert.Collection(audits,
            refused =>
            {
                Assert.Equal(LogLevel.Warning, refused.Level);
                Assert.Equal(1, refused.Value("Generation"));
                Assert.Equal(3, refused.Value("MpResultCode"));
                Assert.Equal("interaction-mode-not-applied", refused.Value("DiagnosticCode"));
                AssertModes(refused);
            },
            applied =>
            {
                Assert.Equal(LogLevel.Information, applied.Level);
                Assert.Equal(2, applied.Value("Generation"));
                Assert.Equal(2, applied.Value("MpResultCode"));
                Assert.Equal("interaction-mode-applied", applied.Value("DiagnosticCode"));
                AssertModes(applied);
            });

        static void AssertModes(LogEntry entry)
        {
            Assert.Equal(SpatialAnalyzerInteractionMode.Silent, entry.Value("SaInteractionMode"));
            Assert.Equal(global::Briosa.MpInteractionMode.HaltOnFailureOrPartialSuccess, entry.Value("MpInteractionMode"));
            Assert.Equal(global::Briosa.MpDialogInteractionMode.AllowApplicationInteraction,
                entry.Value("MpDialogInteractionMode"));
        }
    }

    private static void AssertQuarantined(WorkerProcessSupervisor supervisor, string diagnosticCode)
    {
        var current = supervisor.Current;
        Assert.Equal(WorkerLifecycleState.Degraded, current.State);
        Assert.False(current.ReadyForExecution);
        Assert.Equal(diagnosticCode, current.DiagnosticCode);
        Assert.Equal(WorkerExecutionReadinessState.OperatorRecoveryRequired, current.Connection!.ExecutionReadinessState);
        Assert.DoesNotContain(supervisor.History, snapshot => snapshot.ReadyForExecution);
    }

    internal static WorkerProcessSupervisor CreateSupervisor(
        IWorkerProcessFactory factory,
        SpatialAnalyzerInteractionMode mode,
        TimeProvider? timeProvider = null,
        ILogger<WorkerProcessSupervisor>? logger = null,
        ISpatialAnalyzerModalDialogProbe? modalDialogProbe = null) =>
        new(
            factory,
            new WorkerLifecyclePolicy(
                heartbeatInterval: TimeSpan.FromHours(1),
                heartbeatTimeout: TimeSpan.FromSeconds(5),
                startupTimeout: TimeSpan.FromSeconds(11),
                shutdownTimeout: TimeSpan.FromSeconds(3),
                readinessProbeTimeout: ReadinessProbeTimeout),
            new WorkerExecutionPolicy(
                ExecutionWatchdogTimeout,
                queueCapacity: 4,
                durationClassOf: TestDurationClasses.ReviewedOrSyntheticQuick),
            timeProvider,
            logger,
            ExactTargetIdentityPolicy.CreateForTesting(
                SpatialAnalyzerApi.TargetVersion,
                activatedSdkVersion: SpatialAnalyzerApi.TargetVersion,
                connectedSpatialAnalyzerVersion: SpatialAnalyzerApi.TargetVersion),
            interactionMode: mode,
            modalDialogProbe: modalDialogProbe);

    internal static WorkerMpCommand CreateCommand() =>
        new("file_operations.get_working_directory", "Get Working Directory", [],
            [new WorkerMpOutputArgument("Directory", WorkerMpValueKind.Text, "GetStringArg")]);

    internal sealed class CapturingLogger : ILogger<WorkerProcessSupervisor>
    {
        public ConcurrentQueue<LogEntry> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Enqueue(new LogEntry(logLevel, eventId,
                state as IReadOnlyList<KeyValuePair<string, object?>> ?? []));
    }

    internal sealed record LogEntry(
        LogLevel Level, EventId EventId, IReadOnlyList<KeyValuePair<string, object?>> State)
    {
        public object? Value(string name) => State.FirstOrDefault(pair => pair.Key == name).Value;
    }
}
