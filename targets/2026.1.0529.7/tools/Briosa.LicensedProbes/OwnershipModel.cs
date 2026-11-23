using System.Globalization;
using Api = global::Briosa;

namespace Briosa.LicensedProbes;

/// <summary>
/// The dedicated SDK-ownership probes (#252) for licensed session 4 (#402). Each is one
/// bounded, scripted run against one packaged server, except <see cref="Observe"/>, which
/// only watches the process list.
/// </summary>
internal enum OwnershipScenario
{
    // Start, connect and stop the SDK: which SDK process the worker activated, and whether a
    // clean stop releases it. The first start on a fresh server is also the cold-start sample.
    Release,

    // Start and connect, then terminate the idle worker from outside, as a crash would.
    WorkerCrash,

    // Start and connect, then terminate the server, the worker's parent.
    ParentDeath,

    // Start and connect, then stall one MP on a SpatialAnalyzer dialog until the server's
    // watchdog terminates the worker (the probe 26 trigger).
    WatchdogKill,

    // Start and connect, then start a second, unchanged worker that activates the SDK and
    // attaches with ConnectEx while the first is connected. The second never runs an MP.
    CompetingClient,

    // Watch only: until no SDK or Briosa process remains, or the window ends.
    Observe
}

/// <summary>The parsed ownership command line. Parsing is strict, as for the probe phases.</summary>
internal sealed record OwnershipOptions
{
    public const string Option = "--ownership";

    private static readonly HashSet<string> Flags = new(StringComparer.Ordinal) { "--dry-run", ProbeTarget.ConfirmFlag };

    private static readonly HashSet<string> Valued = new(StringComparer.Ordinal)
    {
        Option, "--output-directory", "--address", "--worker-path", "--observe-seconds", "--harness-revision",
        "--step-timeout-seconds", "--operator-timeout-seconds", ProbeOptions.RestoreModeOption
    };

    public OwnershipScenario Scenario { get; init; }

    public bool DryRun { get; init; }

    public string? OutputDirectory { get; init; }

    public Uri Address { get; init; } = new("http://127.0.0.1:50051");

    public string? WorkerPath { get; init; }

    public TimeSpan ObserveWindow { get; init; } = TimeSpan.FromSeconds(120);

    public TimeSpan StepTimeout { get; init; } = TimeSpan.FromSeconds(120);

    public TimeSpan OperatorTimeout { get; init; } = TimeSpan.FromSeconds(300);

    public string? HarnessRevision { get; init; }

    public InteractionModeSettings InteractionMode { get; init; } = InteractionModeSettings.Placeholder;

    public static bool IsOwnership(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return arguments.Contains(Option, StringComparer.Ordinal);
    }

    public static string ScenarioName(OwnershipScenario scenario) => scenario switch
    {
        OwnershipScenario.Release => "release",
        OwnershipScenario.WorkerCrash => "worker-crash",
        OwnershipScenario.ParentDeath => "parent-death",
        OwnershipScenario.WatchdogKill => "watchdog-kill",
        OwnershipScenario.CompetingClient => "competing-client",
        _ => "observe"
    };

    public static OwnershipOptions Parse(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        var flags = new HashSet<string>(StringComparer.Ordinal);
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            if (argument.StartsWith(ProbeTarget.ConfirmFlagPrefix, StringComparison.Ordinal) &&
                !string.Equals(argument, ProbeTarget.ConfirmFlag, StringComparison.Ordinal))
                throw new ProbeUsageException("This harness serves only its exact target; another target's confirmation is refused.");
            if (Flags.Contains(argument))
            {
                if (!flags.Add(argument))
                    throw new ProbeUsageException($"Option '{argument}' is repeated.");
                continue;
            }

            if (!Valued.Contains(argument))
                throw new ProbeUsageException("An unknown option was supplied.");
            if (index + 1 >= arguments.Count)
                throw new ProbeUsageException($"Option '{argument}' needs a value.");
            if (!values.TryAdd(argument, arguments[++index]))
                throw new ProbeUsageException($"Option '{argument}' is repeated.");
        }

        var scenario = values.GetValueOrDefault(Option) switch
        {
            "release" => OwnershipScenario.Release,
            "worker-crash" => OwnershipScenario.WorkerCrash,
            "parent-death" => OwnershipScenario.ParentDeath,
            "watchdog-kill" => OwnershipScenario.WatchdogKill,
            "competing-client" => OwnershipScenario.CompetingClient,
            "observe" => OwnershipScenario.Observe,
            _ => throw new ProbeUsageException(
                $"{Option} must be release, worker-crash, parent-death, watchdog-kill, competing-client or observe.")
        };

        var options = new OwnershipOptions
        {
            Scenario = scenario,
            DryRun = flags.Contains("--dry-run"),
            OutputDirectory = values.GetValueOrDefault("--output-directory"),
            WorkerPath = values.GetValueOrDefault("--worker-path"),
            ObserveWindow = Seconds(values, "--observe-seconds", 120, 10, 1800),
            StepTimeout = Seconds(values, "--step-timeout-seconds", 120, 5, 600),
            OperatorTimeout = Seconds(values, "--operator-timeout-seconds", 300, 30, 1800),
            HarnessRevision = values.GetValueOrDefault("--harness-revision"),
            InteractionMode = values.GetValueOrDefault(ProbeOptions.RestoreModeOption) switch
            {
                null => InteractionModeSettings.Placeholder,
                "manual" => new InteractionModeSettings(Api.SaInteractionMode.Manual, Supplied: true),
                _ => throw new ProbeUsageException(
                    $"{ProbeOptions.RestoreModeOption} must be manual: the watchdog-kill stall needs SA in Manual and ends with nothing restored.")
            }
        };

        if (options.HarnessRevision is not null &&
            (options.HarnessRevision.Length != 40 || !options.HarnessRevision.All(char.IsAsciiHexDigitLower)))
            throw new ProbeUsageException("--harness-revision must be a 40-character lowercase commit SHA.");

        if (values.TryGetValue("--address", out var addressText))
        {
            if (scenario == OwnershipScenario.Observe)
                throw new ProbeUsageException("observe watches processes only; --address does not apply.");
            var address = new Uri(addressText, UriKind.Absolute);
            if (!address.IsLoopback || address.Scheme != Uri.UriSchemeHttp)
                throw new ProbeUsageException("--address must be loopback HTTP.");
            options = options with { Address = address };
        }

        if (options.WorkerPath is not null && scenario != OwnershipScenario.CompetingClient)
            throw new ProbeUsageException("--worker-path applies only to competing-client.");
        if (options.InteractionMode.Supplied && scenario != OwnershipScenario.WatchdogKill)
            throw new ProbeUsageException($"{ProbeOptions.RestoreModeOption} applies only to watchdog-kill.");

        if (options.DryRun)
        {
            if (flags.Contains(ProbeTarget.ConfirmFlag) || values.ContainsKey("--address") || options.WorkerPath is not null)
                throw new ProbeUsageException("--dry-run never connects; remove the confirmation and connection options.");
            return options;
        }

        if (!flags.Contains(ProbeTarget.ConfirmFlag))
            throw new ProbeUsageException($"A licensed ownership probe requires {ProbeTarget.ConfirmFlag} or --dry-run.");
        if (options.OutputDirectory is null)
            throw new ProbeUsageException("A licensed ownership probe requires --output-directory.");
        if (scenario == OwnershipScenario.CompetingClient && options.WorkerPath is null)
            throw new ProbeUsageException("competing-client requires --worker-path to the package's Briosa.Worker.exe.");
        if (scenario == OwnershipScenario.WatchdogKill && !options.InteractionMode.Supplied)
            throw new ProbeUsageException($"watchdog-kill requires {ProbeOptions.RestoreModeOption} manual.");
        return options;
    }

    private static TimeSpan Seconds(Dictionary<string, string> values, string key, int fallback, int minimum, int maximum)
    {
        if (!values.TryGetValue(key, out var text))
            return TimeSpan.FromSeconds(fallback);
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) || seconds < minimum || seconds > maximum)
            throw new ProbeUsageException($"{key} must be between {minimum} and {maximum}.");
        return TimeSpan.FromSeconds(seconds);
    }
}

/// <summary>
/// The value-free result of one lifecycle or discovery call: transport status, enum names,
/// the SDK generation number, readiness, and a listed diagnostic code.
/// </summary>
internal sealed record LifecycleResult(
    string Transport,
    string? SdkState,
    int? Generation,
    bool ReadyForMp,
    string? ConnectionState,
    string? DiagnosticCode,
    string? IncidentTermination)
{
    public const string ClientCancelledTransport = "grpc:client-cancelled";

    // The connection's peer could not be proven to be the bound server, so the call was never
    // sent (#402).
    public const string PeerUnverifiedTransport = "peer-unverified";

    public bool Ok => string.Equals(Transport, "grpc:OK", StringComparison.Ordinal);

    /// <summary>The call was sent and the caller stopped waiting: its outcome is unknown.</summary>
    public bool Cancelled => string.Equals(Transport, ClientCancelledTransport, StringComparison.Ordinal);

    /// <summary>The call was never sent: no connection to the bound server could be proven.</summary>
    public bool PeerUnverified => string.Equals(Transport, PeerUnverifiedTransport, StringComparison.Ordinal);

    public static LifecycleResult Failed(string transport, string? diagnosticCode) =>
        new(transport, null, null, false, null, diagnosticCode, null);
}

/// <summary>What discovery says before a probe: target, readiness, and the published lifecycle bounds.</summary>
internal sealed record ServerDescription(
    bool Reachable,
    bool TargetMatches,
    string? ServerVersion,
    string? SourceRevision,
    IReadOnlyDictionary<string, long> LifecycleBoundsMilliseconds)
{
    public static ServerDescription Unreachable { get; } =
        new(false, false, null, null, new SortedDictionary<string, long>(StringComparer.Ordinal));

    /// <summary>Discovery was never sent: the connection's peer was not the bound server.</summary>
    public static ServerDescription PeerRefused { get; } = Unreachable with { PeerVerified = false };

    /// <summary>Whether every connection the call could use was proven to reach the bound server.</summary>
    public bool PeerVerified { get; init; } = true;
}

/// <summary>The value-free result of one step of the competing second worker.</summary>
internal sealed record SecondWorkerResult(string Result, string? ConnectionState, string? DiagnosticCode, bool? ActivatedSdkMatchesTarget)
{
    public const string Ready = "ready";
    public const string Connected = "connected";
}

/// <summary>
/// The packaged server, through generated clients on loopback only. Nothing is sent before
/// <see cref="BindPeer"/>, and afterwards only over a connection whose peer, read from the
/// TCP connection table, is the bound server process.
/// </summary>
internal interface IOwnershipServer : IAsyncDisposable
{
    /// <summary>Every connection from now on must reach this process, or the call is never sent.</summary>
    void BindPeer(int serverProcessId);

    /// <summary>Connections proven to reach the bound server; more than one means one was replaced.</summary>
    int VerifiedConnections { get; }

    /// <summary>Connections closed unused because their peer was not proven.</summary>
    int RefusedConnections { get; }

    Task<ServerDescription> DescribeAsync(CancellationToken cancellationToken);

    Task<LifecycleResult> GetSdkStateAsync(CancellationToken cancellationToken);

    Task<LifecycleResult> StartSdkAsync(CancellationToken cancellationToken);

    Task<LifecycleResult> ConnectAsync(int generation, CancellationToken cancellationToken);

    Task<LifecycleResult> StopSdkAsync(int generation, CancellationToken cancellationToken);

    /// <summary>Sends one MP step once through the generated client; never retried.</summary>
    Task<ProbeOutcome> ExecuteAsync(ProbeStep step, CancellationToken cancellationToken);
}

internal enum BindingCheck
{
    // The held handle still names a running process: the one that was bound.
    Running,

    // The bound process has exited. Nothing else can take its place behind the handle.
    Exited,

    // Its state could not be read.
    Unverifiable
}

/// <summary>
/// One Briosa worker or server process bound to this run through a handle that was opened and
/// checked once (name and start time read through the handle). While it is held, the handle
/// names only that process, even if its id is reused, so a check or a termination can never
/// reach a replacement. The handle can query and wait only, and can terminate only when the
/// scenario's script terminates that process.
/// </summary>
internal interface IBoundProcess : IDisposable
{
    BindingCheck Check();

    /// <summary>
    /// Ends that one process, never its children: terminated, exited, failed, or not-authorized
    /// when it was bound without the right to terminate.
    /// </summary>
    string Terminate();
}

/// <summary>What the ownership probes may read about, or do to, processes beyond the snapshot.</summary>
internal interface IProcessControl
{
    /// <summary>The operator's Windows session: the harness's own.</summary>
    int HarnessSessionId { get; }

    /// <summary>
    /// Binds one Briosa worker or server. Null when the process is gone, cannot be opened, or its
    /// name or start time read through the handle is not the one the tracker saw. Only a
    /// <paramref name="terminable"/> binding can end the process.
    /// </summary>
    IBoundProcess? Bind(int processId, DateTimeOffset startedAt, string expectedName, bool terminable);

    /// <summary>
    /// The owning process ids of every listener the endpoint's address can reach (its own address
    /// or its family's wildcard, both families for a name); null when the table cannot be read.
    /// </summary>
    IReadOnlyList<int>? EndpointListeners(Uri address);

    /// <summary>Whether the worker executable is in the bound server's own folder; null when that cannot be read.</summary>
    bool? WorkerFromServerPackage(IBoundProcess server, string workerPath);
}

/// <summary>A second, unchanged Briosa worker hosted by the harness (competing-client only).</summary>
internal interface ISecondWorker : IAsyncDisposable
{
    Task<SecondWorkerResult> StartAsync(CancellationToken cancellationToken);

    Task<SecondWorkerResult> ConnectAsync(CancellationToken cancellationToken);

    Task<SecondWorkerResult> StopAsync(CancellationToken cancellationToken);

    /// <summary>A license prompt is open: from now on nothing is sent and nothing is terminated.</summary>
    void Abandon();
}

/// <summary>Waits between process snapshots. Tests replace it so that no test sleeps.</summary>
internal interface IPause
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

internal sealed class SystemPause : IPause
{
    public static SystemPause Instance { get; } = new();

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) => Task.Delay(delay, cancellationToken);
}

/// <summary>Everything an ownership probe touches, so that portable tests can replace all of it.</summary>
internal sealed record OwnershipEnvironment(
    IProcessTable Processes,
    IOwnershipServer? Server,
    IProcessControl Control,
    Func<ISecondWorker>? SecondWorker,
    IOperatorConsole Console,
    IPause Pause,
    TimeProvider Clock,
    int HarnessProcessId);

/// <summary>One step of an ownership probe. It holds labels, enum names, listed codes and times only.</summary>
internal sealed record OwnershipStepRecord(string Id, string Action, long StartedMilliseconds, long ElapsedMilliseconds, string Result)
{
    public LifecycleResult? Lifecycle { get; init; }

    public ProbeOutcome? Outcome { get; init; }

    public SecondWorkerResult? SecondWorker { get; init; }

    // The label of the process a step bound or terminated, for example worker-1, or the id of
    // the step a cancellation interrupted.
    public string? Target { get; init; }

    public string? OperatorAnswer { get; init; }

    // Labels of the SDK, worker and server processes alive when the step ended, or for a
    // descendant census the labelled descendants.
    public IReadOnlyList<string> Alive { get; init; } = [];

    // A descendant census: how many processes of any kind descend from the bound worker, and how
    // many of them are not on the approved list (only their count is ever recorded).
    public int? DescendantCount { get; init; }

    public int? UnapprovedDescendantCount { get; init; }

    // The status of the snapshot the step ended with.
    public SnapshotStatus? Snapshot { get; init; }
}

internal sealed record OwnershipRecord(
    OwnershipScenario Scenario,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    string? StopReason,
    ServerDescription? Server,
    IReadOnlyList<OwnershipStepRecord> Steps,
    IReadOnlyList<TrackedProcess> Processes,
    TimeSpan ObserveWindow,
    string? HarnessRevision)
{
    public string? BoundServer { get; init; }

    public string? BoundWorker { get; init; }

    public int? BoundGeneration { get; init; }

    public int CompleteSnapshots { get; init; }

    public int IncompleteSnapshots { get; init; }

    public int FailedSnapshots { get; init; }

    /// <summary>Connections proven to reach the bound server, and connections refused unused.</summary>
    public int VerifiedConnections { get; init; }

    public int RefusedConnections { get; init; }

    /// <summary>The last snapshot of the run; only a complete one can show that nothing remains.</summary>
    public SnapshotStatus? FinalSnapshot { get; init; }

    /// <summary>Every planned step ran as the script says.</summary>
    public bool Completed => StopReason is null;

    /// <summary>Whether the residual list is known: the run ended with a complete snapshot.</summary>
    public bool ResidualKnown => FinalSnapshot == SnapshotStatus.Complete;

    /// <summary>
    /// SDK and worker processes alive when the run ended; for <see cref="OwnershipScenario.Observe"/>,
    /// and after parent death, server processes too. When <see cref="ResidualKnown"/> is false the
    /// list is only what the last complete snapshot showed, and postflight cannot pass.
    /// </summary>
    public IReadOnlyList<TrackedProcess> Residual => [.. Processes.Where(process => process.Alive &&
        (process.Role is ProcessRoles.Sdk or ProcessRoles.Worker ||
         (process.Role == ProcessRoles.Server && Scenario is OwnershipScenario.Observe or OwnershipScenario.ParentDeath)))];
}
