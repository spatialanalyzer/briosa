using System.Text.Json;
using Briosa.Server.Operations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

/// <summary>
/// Duration-class execution budgets and the separate readiness-probe and
/// startup bounds (F3, #296). Every deadline runs on the injected virtual clock.
/// </summary>
[Collection("Worker process lifecycle")]
public sealed class ExecutionBudgetTests
{
    private const string QuickOperation = "file_operations.get_working_directory";
    private const string LongRunningOperation = "file_operations.backup_now";
    private const string InteractiveOperation =
        "construction_operations.construct_circles_from_surface_faces_runtime_select";

    // A quick operation whose caller option can open an operator dialog.
    private const string QueryPointsToObjects = "analysis_operations.query_points_to_objects";

    // Distinct from each other and from every lifecycle bound below, so a
    // scheduled timer identifies exactly which bound the supervisor armed.
    private static readonly TimeSpan QuickBudget = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan LongRunningBudget = TimeSpan.FromSeconds(7);
    private static readonly TimeSpan InteractiveBudget = TimeSpan.FromSeconds(13);
    private static readonly TimeSpan ReadinessProbeBound = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan StartupBound = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(17);
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(1);

    // A real fake worker process must still start and exit within a real bound.
    private static readonly TimeSpan ProcessBound = TimeSpan.FromSeconds(30);

    [Theory]
    [InlineData(QuickOperation, (int)OperationDurationClass.Quick)]
    [InlineData(LongRunningOperation, (int)OperationDurationClass.LongRunning)]
    [InlineData(InteractiveOperation, (int)OperationDurationClass.Interactive)]
    public void SampleOperationsCarryTheirReviewedDurationClass(string operationId, int expected)
    {
        var lookup = OperationClassification.Find(operationId);

        Assert.True(lookup.IsReviewed);
        Assert.Equal((OperationDurationClass)expected, lookup.Row!.Duration);
    }

    [Theory]
    [InlineData((int)OperationDurationClass.Quick, 3)]
    [InlineData((int)OperationDurationClass.LongRunning, 7)]
    [InlineData((int)OperationDurationClass.Interactive, 13)]
    public void PolicySelectsTheBudgetForEachDurationClass(int durationClass, int expectedSeconds)
    {
        var policy = CreateExecutionPolicy(_ => (OperationDurationClass)durationClass);

        Assert.True(policy.TryGetExecutionBudget("any", out var selected, out var budget));
        Assert.Equal((OperationDurationClass)durationClass, selected);
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), budget);
    }

    [Theory]
    [InlineData((int)OperationDurationClass.Unspecified)]
    [InlineData(99)]
    public void PolicyFailsClosedWithoutAReviewedDurationClass(int durationClass)
    {
        var policy = CreateExecutionPolicy(_ => (OperationDurationClass)durationClass);

        Assert.False(policy.TryGetExecutionBudget("any", out var selected, out var budget));
        Assert.Equal(OperationDurationClass.Unspecified, selected);
        Assert.Equal(TimeSpan.Zero, budget);
    }

    [Theory]
    [InlineData(null, (int)OperationDurationClass.Quick, 3)]
    [InlineData((int)OperationDurationClass.Quick, (int)OperationDurationClass.Quick, 3)]
    [InlineData((int)OperationDurationClass.LongRunning, (int)OperationDurationClass.LongRunning, 7)]
    [InlineData((int)OperationDurationClass.Interactive, (int)OperationDurationClass.Interactive, 13)]
    public void EffectiveRequestClassEscalatesAQuickRow(int? effective, int expectedClass, int expectedSeconds)
    {
        var policy = CreateExecutionPolicy(_ => OperationDurationClass.Quick);

        Assert.True(policy.TryGetExecutionBudget(
            "any", (OperationDurationClass?)effective, out var selected, out var budget));
        Assert.Equal((OperationDurationClass)expectedClass, selected);
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), budget);
    }

    [Fact]
    public void EffectiveRequestClassNeverShortensTheReviewedBudget()
    {
        var policy = CreateExecutionPolicy(_ => OperationDurationClass.LongRunning);
        // An operator may configure interactive shorter than long_running.
        var shortInteractive = new WorkerExecutionPolicy(QuickBudget, queueCapacity: 4,
            longRunningWatchdogTimeout: TimeSpan.FromSeconds(20),
            interactiveWatchdogTimeout: InteractiveBudget,
            durationClassOf: _ => OperationDurationClass.LongRunning);

        Assert.True(policy.TryGetExecutionBudget("any", OperationDurationClass.Quick, out var quickClass, out var quick));
        Assert.True(shortInteractive.TryGetExecutionBudget(
            "any", OperationDurationClass.Interactive, out var interactiveClass, out var interactive));
        Assert.Equal(OperationDurationClass.LongRunning, quickClass);
        Assert.Equal(LongRunningBudget, quick);
        Assert.Equal(OperationDurationClass.LongRunning, interactiveClass);
        Assert.Equal(TimeSpan.FromSeconds(20), interactive);
    }

    [Theory]
    [InlineData((int)OperationDurationClass.Quick, (int)OperationDurationClass.Unspecified)]
    [InlineData((int)OperationDurationClass.Quick, 99)]
    [InlineData((int)OperationDurationClass.Unspecified, (int)OperationDurationClass.Interactive)]
    public void UnreviewedRowOrEffectiveClassFailsClosed(int reviewed, int effective)
    {
        var policy = CreateExecutionPolicy(_ => (OperationDurationClass)reviewed);

        Assert.False(policy.TryGetExecutionBudget(
            "any", (OperationDurationClass)effective, out var selected, out var budget));
        Assert.Equal(OperationDurationClass.Unspecified, selected);
        Assert.Equal(TimeSpan.Zero, budget);
    }

    [Fact]
    public async Task UnspecifiedEffectiveClassDoesNotExecute()
    {
        // The budget is chosen before readiness, so no worker is started.
        await using var supervisor = CreateSupervisor("normal", new HeartbeatTestClock());
        var mapped = false;

        var rejected = await supervisor.ExecuteAsync(
            new WorkerCommandSubmission(QuickOperation, () =>
            {
                mapped = true;
                return Command(QuickOperation);
            }, DurationClass: OperationDurationClass.Unspecified),
            Guid.NewGuid());

        Assert.Equal(WorkerExecutionStatus.PolicyDenied, rejected.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, rejected.ExecutionDisposition);
        Assert.Equal("operation-duration-unreviewed", rejected.DiagnosticCode);
        Assert.False(mapped);
    }

    // PR #298 review: a caller-enabled operator dialog must receive the
    // interactive budget, not the quick watchdog of the operation's table row.
    [Theory]
    [InlineData("results-dialog", 13)]
    [InlineData("default-request", 3)]
    [InlineData("long-running", 7)]
    public async Task AdmittedRequestArmsItsEffectiveDurationClassBudget(string requestCase, int budgetSeconds)
    {
        var (operationId, request) = requestCase switch
        {
            "results-dialog" => (QueryPointsToObjects, Populated<Api.QueryPointsToObjectsRequest>(
                request => request.ShowResultsDialog = true)),
            "default-request" => (QueryPointsToObjects, Populated<Api.QueryPointsToObjectsRequest>(
                request => request.ShowResultsDialog = false)),
            _ => (LongRunningOperation, (IMessage)Populated<Api.BackupNowRequest>(_ => { }))
        };
        var budget = TimeSpan.FromSeconds(budgetSeconds);
        var clock = new HeartbeatTestClock();
        await using var supervisor = CreateSupervisor("hang-on-execute", clock);
        var enforcer = new PolicyEnforcingWorkerCommandExecutor(supervisor, supervisor,
            OperationPolicyTests.CreatePolicy(
                profile: "standard", settings: new() { ["Flags:interactive_ui"] = "allow" }),
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance));
        Assert.True((await supervisor.StartAsync().WaitAsync(ProcessBound)).Succeeded,
            supervisor.Current.DiagnosticCode);

        var executing = enforcer.ExecuteAsync(
            new WorkerCommandSubmission(operationId,
                () => OperationConditionalOptionTests.OperationBuilder.For(operationId).Create(request),
                Request: request),
            Guid.NewGuid());
        WorkerExecutionOutcome outcome;
        try
        {
            await clock.WaitForScheduledAsync(budget);
            // Advancing past every shorter budget, including the quick one, fires nothing.
            clock.Advance(budget - Tick);
            Assert.False(executing.IsCompleted);
            Assert.True(supervisor.Current.ReadyForExecution);

            await clock.FireNextAsync(budget);
            outcome = await executing.WaitAsync(ProcessBound);
        }
        finally
        {
            ReleaseEveryBound(clock);
        }

        Assert.Equal(WorkerExecutionStatus.WatchdogTimeout, outcome.Status);
        Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown, outcome.ExecutionDisposition);
        Assert.Equal(operationId, supervisor.Current.LastIncident!.OperationId);
    }

    [Fact]
    public void DefaultPolicyUsesTheReviewedClassificationTable()
    {
        var policy = new WorkerExecutionPolicy(QuickBudget, queueCapacity: 4,
            longRunningWatchdogTimeout: LongRunningBudget, interactiveWatchdogTimeout: InteractiveBudget);

        Assert.True(policy.TryGetExecutionBudget(QuickOperation, out _, out var quick));
        Assert.True(policy.TryGetExecutionBudget(LongRunningOperation, out _, out var longRunning));
        Assert.True(policy.TryGetExecutionBudget(InteractiveOperation, out _, out var interactive));
        Assert.False(policy.TryGetExecutionBudget("synthetic.unreviewed", out _, out _));
        Assert.Equal(QuickBudget, quick);
        Assert.Equal(LongRunningBudget, longRunning);
        Assert.Equal(InteractiveBudget, interactive);
    }

    [Fact]
    public void EveryRegisteredOperationResolvesAnExecutionBudget()
    {
        var policy = new WorkerExecutionPolicy(QuickBudget, queueCapacity: 4);

        var unresolved = SpatialAnalyzerApi.Operations
            .Where(operation => !policy.TryGetExecutionBudget(operation.OperationId, out _, out _))
            .Select(operation => operation.OperationId)
            .ToList();

        Assert.Empty(unresolved);
    }

    [Theory]
    [InlineData(QuickOperation, 3)]
    [InlineData(LongRunningOperation, 7)]
    [InlineData(InteractiveOperation, 13)]
    public async Task SupervisorEnforcesTheDurationClassBudgetAsTheExecutionWatchdog(
        string operationId,
        int budgetSeconds)
    {
        var budget = TimeSpan.FromSeconds(budgetSeconds);
        var clock = new HeartbeatTestClock();
        await using var supervisor = CreateSupervisor("hang-on-execute", clock);
        Assert.True((await supervisor.StartAsync().WaitAsync(ProcessBound)).Succeeded,
            supervisor.Current.DiagnosticCode);

        var executing = supervisor.ExecuteAsync(Command(operationId));
        WorkerExecutionOutcome outcome;
        try
        {
            await clock.WaitForScheduledAsync(budget);
            clock.Advance(budget - Tick);
            Assert.False(executing.IsCompleted);
            Assert.True(supervisor.Current.ReadyForExecution);

            await clock.FireNextAsync(budget);
            outcome = await executing.WaitAsync(ProcessBound);
        }
        finally
        {
            ReleaseEveryBound(clock);
        }

        Assert.Equal(WorkerExecutionStatus.WatchdogTimeout, outcome.Status);
        Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown, outcome.ExecutionDisposition);
        Assert.Equal("worker-execution-watchdog-timeout", outcome.DiagnosticCode);
        Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
        Assert.Equal(operationId, supervisor.Current.LastIncident!.OperationId);
        Assert.Equal(WorkerIncidentKind.WatchdogTerminated, supervisor.Current.LastIncident.Kind);
        Assert.Equal(1, supervisor.ExecutionSnapshot.WatchdogTimeouts);
    }

    [Fact]
    public async Task UnreviewedOperationDoesNotExecuteAndLeavesTheWorkerReady()
    {
        // The default lookup is the reviewed table; the virtual clock never advances.
        await using var supervisor = CreateSupervisor("normal", new HeartbeatTestClock());
        Assert.True((await supervisor.StartAsync().WaitAsync(ProcessBound)).Succeeded,
            supervisor.Current.DiagnosticCode);
        var mapped = false;

        var rejected = await supervisor.ExecuteAsync(
            new WorkerCommandSubmission("synthetic.unreviewed", () =>
            {
                mapped = true;
                return Command("synthetic.unreviewed");
            }),
            Guid.NewGuid());
        var completed = await supervisor.ExecuteAsync(Command(QuickOperation));

        Assert.Equal(WorkerExecutionStatus.PolicyDenied, rejected.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, rejected.ExecutionDisposition);
        Assert.Equal("operation-duration-unreviewed", rejected.DiagnosticCode);
        Assert.Null(rejected.Execution);
        Assert.False(mapped);
        Assert.Equal(WorkerExecutionStatus.Completed, completed.Status);
        Assert.Equal(1, supervisor.ExecutionSnapshot.AdmittedRequests);
        Assert.True(supervisor.Current.ReadyForExecution);
    }

    [Fact]
    public async Task ReadinessProbeUsesItsOwnBoundNotAnExecutionBudget()
    {
        var clock = new HeartbeatTestClock();
        await using var supervisor = CreateSupervisor("hang-on-verify", clock);

        var starting = supervisor.StartAsync();
        WorkerLifecycleResult result;
        try
        {
            await clock.WaitForScheduledAsync(ReadinessProbeBound);
            // The quick execution budget is shorter than the probe bound and passes unnoticed.
            clock.Advance(ReadinessProbeBound - Tick);
            Assert.False(starting.IsCompleted);

            await clock.FireNextAsync(ReadinessProbeBound);
            result = await starting.WaitAsync(ProcessBound);
        }
        finally
        {
            ReleaseEveryBound(clock);
        }

        Assert.False(result.Succeeded);
        Assert.Equal(WorkerLifecycleFailure.ReadinessTimeout, supervisor.Current.LifecycleFailure);
        Assert.Equal("execution-readiness-probe-timeout", supervisor.Current.DiagnosticCode);
        Assert.Equal(
            WorkerExecutionReadinessState.OperatorRecoveryRequired,
            supervisor.Current.Connection!.ExecutionReadinessState);
    }

    [Fact]
    public async Task ReadinessProbeBoundIsIndependentOfALongerExecutionWatchdog()
    {
        var clock = new HeartbeatTestClock();
        var shortProbe = TimeSpan.FromSeconds(2);
        await using var supervisor = CreateSupervisor("hang-on-verify", clock, readinessProbeTimeout: shortProbe);

        var starting = supervisor.StartAsync();
        WorkerLifecycleResult result;
        try
        {
            await clock.FireNextAsync(shortProbe);
            result = await starting.WaitAsync(ProcessBound);
        }
        finally
        {
            ReleaseEveryBound(clock);
        }

        Assert.False(result.Succeeded);
        Assert.Equal(WorkerLifecycleFailure.ReadinessTimeout, supervisor.Current.LifecycleFailure);
    }

    [Fact]
    public async Task StartupUsesItsOwnBoundNotAnExecutionOrProbeBudget()
    {
        var clock = new HeartbeatTestClock();
        await using var supervisor = CreateSupervisor("hang-before-ready", clock);

        var starting = supervisor.StartAsync();
        WorkerLifecycleResult result;
        try
        {
            await clock.WaitForScheduledAsync(StartupBound);
            clock.Advance(StartupBound - Tick);
            Assert.False(starting.IsCompleted);

            await clock.FireNextAsync(StartupBound);
            result = await starting.WaitAsync(ProcessBound);
        }
        finally
        {
            ReleaseEveryBound(clock);
        }

        Assert.False(result.Succeeded);
        Assert.Equal(WorkerLifecycleFailure.StartupTimeout, supervisor.Current.LifecycleFailure);
        Assert.Equal("worker-startup-timeout", supervisor.Current.DiagnosticCode);
    }

    [Fact]
    public async Task OmittedConfigurationUsesTheDocumentedDefaults()
    {
        var provider = BuildProvider([]);
        await using var providerScope = provider.ConfigureAwait(true);
        var options = provider.GetRequiredService<WorkerProcessOptions>();
        var supervisor = provider.GetRequiredService<WorkerProcessSupervisor>();

        // These literals pin the documented production defaults; the code
        // constants in WorkerProcessOptions are their only source.
        Assert.Equal(TimeSpan.FromSeconds(30), options.ExecutionWatchdogTimeout);
        Assert.Equal(TimeSpan.FromMinutes(10), options.LongRunningExecutionWatchdogTimeout);
        Assert.Equal(TimeSpan.FromMinutes(30), options.InteractiveExecutionWatchdogTimeout);
        Assert.Equal(TimeSpan.FromSeconds(30), options.ReadinessProbeTimeout);
        Assert.Equal(TimeSpan.FromSeconds(10), options.StartupTimeout);
        Assert.Equal(options.ExecutionWatchdogTimeout, supervisor.ExecutionPolicy.WatchdogTimeout);
        Assert.Equal(options.LongRunningExecutionWatchdogTimeout,
            supervisor.ExecutionPolicy.LongRunningWatchdogTimeout);
        Assert.Equal(options.InteractiveExecutionWatchdogTimeout,
            supervisor.ExecutionPolicy.InteractiveWatchdogTimeout);
        Assert.Equal(options.ReadinessProbeTimeout, supervisor.LifecyclePolicy.ReadinessProbeTimeout);
        Assert.Equal(options.StartupTimeout, supervisor.LifecyclePolicy.StartupTimeout);
    }

    [Fact]
    public async Task ConfiguredBudgetsAndBoundsReachTheSupervisor()
    {
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            [WorkerProcessOptions.ExecutionWatchdogTimeoutKey] = "00:00:45",
            [WorkerProcessOptions.LongRunningExecutionWatchdogTimeoutKey] = "00:20:00",
            [WorkerProcessOptions.InteractiveExecutionWatchdogTimeoutKey] = "01:00:00",
            [WorkerProcessOptions.ReadinessProbeTimeoutKey] = "00:00:20",
            [WorkerProcessOptions.StartupTimeoutKey] = "00:00:40"
        });
        await using var providerScope = provider.ConfigureAwait(true);
        var supervisor = provider.GetRequiredService<WorkerProcessSupervisor>();

        Assert.Equal(TimeSpan.FromSeconds(45), supervisor.ExecutionPolicy.WatchdogTimeout);
        Assert.Equal(TimeSpan.FromMinutes(20), supervisor.ExecutionPolicy.LongRunningWatchdogTimeout);
        Assert.Equal(TimeSpan.FromHours(1), supervisor.ExecutionPolicy.InteractiveWatchdogTimeout);
        Assert.Equal(TimeSpan.FromSeconds(20), supervisor.LifecyclePolicy.ReadinessProbeTimeout);
        Assert.Equal(TimeSpan.FromSeconds(40), supervisor.LifecyclePolicy.StartupTimeout);
        Assert.True(supervisor.ExecutionPolicy.TryGetExecutionBudget(LongRunningOperation, out _, out var budget));
        Assert.Equal(TimeSpan.FromMinutes(20), budget);
    }

    // The probe and quick defaults are both 30 s, so independence is proved by
    // configuring one and observing that the other does not follow it.
    [Theory]
    [InlineData(WorkerProcessOptions.ExecutionWatchdogTimeoutKey, "00:02:00")]
    [InlineData(WorkerProcessOptions.ReadinessProbeTimeoutKey, "00:00:05")]
    public async Task ReadinessProbeBoundAndQuickWatchdogAreConfiguredIndependently(string key, string value)
    {
        var provider = BuildProvider(new Dictionary<string, string?> { [key] = value });
        await using var providerScope = provider.ConfigureAwait(true);
        var supervisor = provider.GetRequiredService<WorkerProcessSupervisor>();
        var configured = TimeSpan.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        if (key == WorkerProcessOptions.ExecutionWatchdogTimeoutKey)
        {
            Assert.Equal(configured, supervisor.ExecutionPolicy.WatchdogTimeout);
            Assert.Equal(WorkerProcessOptions.DefaultReadinessProbeTimeout,
                supervisor.LifecyclePolicy.ReadinessProbeTimeout);
        }
        else
        {
            Assert.Equal(configured, supervisor.LifecyclePolicy.ReadinessProbeTimeout);
            Assert.Equal(WorkerProcessOptions.DefaultExecutionWatchdogTimeout,
                supervisor.ExecutionPolicy.WatchdogTimeout);
        }
    }

    [Theory]
    [InlineData(WorkerProcessOptions.LongRunningExecutionWatchdogTimeoutKey, "")]
    [InlineData(WorkerProcessOptions.LongRunningExecutionWatchdogTimeoutKey, "not-a-duration")]
    [InlineData(WorkerProcessOptions.LongRunningExecutionWatchdogTimeoutKey, "00:00:00")]
    [InlineData(WorkerProcessOptions.LongRunningExecutionWatchdogTimeoutKey, "-00:00:01")]
    [InlineData(WorkerProcessOptions.LongRunningExecutionWatchdogTimeoutKey, "02:00:01")]
    [InlineData(WorkerProcessOptions.InteractiveExecutionWatchdogTimeoutKey, "")]
    [InlineData(WorkerProcessOptions.InteractiveExecutionWatchdogTimeoutKey, "not-a-duration")]
    [InlineData(WorkerProcessOptions.InteractiveExecutionWatchdogTimeoutKey, "00:00:00")]
    [InlineData(WorkerProcessOptions.InteractiveExecutionWatchdogTimeoutKey, "08:00:01")]
    [InlineData(WorkerProcessOptions.ReadinessProbeTimeoutKey, "")]
    [InlineData(WorkerProcessOptions.ReadinessProbeTimeoutKey, "00:00:00")]
    [InlineData(WorkerProcessOptions.ReadinessProbeTimeoutKey, "-00:00:01")]
    [InlineData(WorkerProcessOptions.ReadinessProbeTimeoutKey, "00:10:01")]
    [InlineData(WorkerProcessOptions.StartupTimeoutKey, "")]
    [InlineData(WorkerProcessOptions.StartupTimeoutKey, "not-a-duration")]
    [InlineData(WorkerProcessOptions.StartupTimeoutKey, "00:00:00")]
    [InlineData(WorkerProcessOptions.StartupTimeoutKey, "00:05:01")]
    public void InvalidBoundFailsStartupWithItsKeyAndNoEchoedValue(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddWorkerProcessLifecycle(configuration));

        Assert.Contains($"'{key}'", exception.Message, StringComparison.Ordinal);
        if (value.Length > 0)
            Assert.DoesNotContain(value, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(WorkerProcessOptions.LongRunningExecutionWatchdogTimeoutKey)]
    [InlineData(WorkerProcessOptions.InteractiveExecutionWatchdogTimeoutKey)]
    public void LongerClassBudgetShorterThanTheQuickWatchdogFailsStartup(string key)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WorkerProcessOptions.ExecutionWatchdogTimeoutKey] = "00:05:00",
                [key] = "00:04:59"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddWorkerProcessLifecycle(configuration));

        Assert.Contains($"'{key}'", exception.Message, StringComparison.Ordinal);
        Assert.Contains(WorkerProcessOptions.ExecutionWatchdogTimeoutKey, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PackagedConfigurationDoesNotRepeatTheWorkerTimingDefaults()
    {
        using var settings = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json")));
        var worker = settings.RootElement.GetProperty("Briosa").GetProperty("Worker");

        foreach (var key in new[]
                 {
                     WorkerProcessOptions.ExecutionWatchdogTimeoutKey,
                     WorkerProcessOptions.LongRunningExecutionWatchdogTimeoutKey,
                     WorkerProcessOptions.InteractiveExecutionWatchdogTimeoutKey,
                     WorkerProcessOptions.ReadinessProbeTimeoutKey,
                     WorkerProcessOptions.StartupTimeoutKey
                 })
        {
            Assert.False(worker.TryGetProperty(key["Briosa:Worker:".Length..], out _), key);
        }
    }

    // Fires every armed virtual bound. After a failed assertion this lets
    // disposal finish instead of waiting on a hung exchange or startup whose
    // virtual deadline would otherwise never arrive.
    private static void ReleaseEveryBound(HeartbeatTestClock clock) => clock.Advance(TimeSpan.FromHours(9));

    private static ServiceProvider BuildProvider(Dictionary<string, string?> values)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorkerProcessLifecycle(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
        return services.BuildServiceProvider();
    }

    private static WorkerProcessSupervisor CreateSupervisor(
        string scenario,
        TimeProvider clock,
        TimeSpan? readinessProbeTimeout = null) =>
        new(
            new NamedPipeWorkerProcessFactory(_ => CreateLaunch(scenario)),
            CreateLifecyclePolicy(readinessProbeTimeout),
            CreateExecutionPolicy(),
            clock,
            identityPolicy: CreateIdentityPolicy());

    private static WorkerLifecyclePolicy CreateLifecyclePolicy(TimeSpan? readinessProbeTimeout = null) =>
        new(
            heartbeatInterval: HeartbeatInterval,
            heartbeatTimeout: TimeSpan.FromMilliseconds(250),
            startupTimeout: StartupBound,
            shutdownTimeout: TimeSpan.FromMilliseconds(500),
            readinessProbeTimeout: readinessProbeTimeout ?? ReadinessProbeBound);

    // Without an explicit lookup the policy uses the target's reviewed table.
    private static WorkerExecutionPolicy CreateExecutionPolicy(
        Func<string, OperationDurationClass>? durationClassOf = null) =>
        new(
            QuickBudget,
            queueCapacity: 4,
            longRunningWatchdogTimeout: LongRunningBudget,
            interactiveWatchdogTimeout: InteractiveBudget,
            durationClassOf: durationClassOf);

    private static ExactTargetIdentityPolicy CreateIdentityPolicy() =>
        ExactTargetIdentityPolicy.CreateForTesting(
            "2024.1.0508.5",
            activatedSdkVersion: "2024.1.0508.5",
            connectedSpatialAnalyzerVersion: "2024.1.0508.5");

    private static T Populated<T>(Action<T> configure)
        where T : IMessage, new()
    {
        var request = OperationConditionalOptionTests.RequestPopulator.Create<T>();
        configure(request);
        return request;
    }

    private static WorkerMpCommand Command(string operationId) =>
        new(operationId, "Scripted Step", [], []);

    private static WorkerProcessLaunch CreateLaunch(string scenario)
    {
        var executable = Path.Combine(
            AppContext.BaseDirectory,
            "worker-test-host",
            "Briosa.Worker.TestHost.exe");
        Assert.True(File.Exists(executable), $"The fake worker executable was not found at '{executable}'.");
        return new WorkerProcessLaunch(executable, ["--scenario", scenario], Path.GetDirectoryName(executable));
    }
}
