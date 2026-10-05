using System.Globalization;

namespace Briosa.Server.Workers;

/// <summary>
/// Validated worker configuration. The <c>Default*</c> fields are the single
/// authoritative source of each worker timing default; the packaged
/// <c>appsettings.json</c> does not repeat them.
/// </summary>
internal sealed record WorkerProcessOptions(
    string ExecutablePath,
    TimeSpan ExecutionWatchdogTimeout)
{
    internal const string ExecutablePathKey = "Briosa:Worker:ExecutablePath";
    internal const string ExecutionWatchdogTimeoutKey =
        "Briosa:Worker:ExecutionWatchdogTimeout";
    internal const string LongRunningExecutionWatchdogTimeoutKey =
        "Briosa:Worker:LongRunningExecutionWatchdogTimeout";
    internal const string InteractiveExecutionWatchdogTimeoutKey =
        "Briosa:Worker:InteractiveExecutionWatchdogTimeout";
    internal const string ReadinessProbeTimeoutKey = "Briosa:Worker:ReadinessProbeTimeout";
    internal const string StartupTimeoutKey = "Briosa:Worker:StartupTimeout";

    internal const string MaxRetainedWorkMiBKey = "Briosa:Worker:MaxRetainedWorkMiB";

    /// <summary>Execution budget for <c>quick</c> operations.</summary>
    internal static readonly TimeSpan DefaultExecutionWatchdogTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Execution budget for <c>long_running</c> operations.</summary>
    internal static readonly TimeSpan DefaultLongRunningExecutionWatchdogTimeout = TimeSpan.FromMinutes(10);

    /// <summary>Execution budget for <c>interactive</c> operations.</summary>
    internal static readonly TimeSpan DefaultInteractiveExecutionWatchdogTimeout = TimeSpan.FromMinutes(30);

    /// <summary>Bound for the private execution-readiness probe exchange.</summary>
    internal static readonly TimeSpan DefaultReadinessProbeTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Bound for worker launch, COM activation, Ready, and ConnectEx.</summary>
    internal static readonly TimeSpan DefaultStartupTimeout = TimeSpan.FromSeconds(10);

    internal static readonly TimeSpan MaxExecutionWatchdogTimeout = TimeSpan.FromMinutes(10);
    internal static readonly TimeSpan MaxLongRunningExecutionWatchdogTimeout = TimeSpan.FromHours(2);
    internal static readonly TimeSpan MaxInteractiveExecutionWatchdogTimeout = TimeSpan.FromHours(8);
    internal static readonly TimeSpan MaxReadinessProbeTimeout = TimeSpan.FromMinutes(10);
    internal static readonly TimeSpan MaxStartupTimeout = TimeSpan.FromMinutes(5);

    public int MaxRetainedWorkMiB { get; init; } = 32;

    public TimeSpan LongRunningExecutionWatchdogTimeout { get; init; } =
        DefaultLongRunningExecutionWatchdogTimeout;

    public TimeSpan InteractiveExecutionWatchdogTimeout { get; init; } =
        DefaultInteractiveExecutionWatchdogTimeout;

    public TimeSpan ReadinessProbeTimeout { get; init; } = DefaultReadinessProbeTimeout;

    public TimeSpan StartupTimeout { get; init; } = DefaultStartupTimeout;

    public static WorkerProcessOptions BindAndValidate(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredPath = configuration[ExecutablePathKey];
        var executablePath = configuredPath is null
            ? Path.Combine(AppContext.BaseDirectory, "Briosa.Worker.exe")
            : ResolveConfiguredExecutable(configuredPath);
        var watchdogTimeout = ReadDuration(configuration, ExecutionWatchdogTimeoutKey,
            DefaultExecutionWatchdogTimeout, MaxExecutionWatchdogTimeout, "ten minutes");
        var longRunningTimeout = ReadDuration(configuration, LongRunningExecutionWatchdogTimeoutKey,
            DefaultLongRunningExecutionWatchdogTimeout, MaxLongRunningExecutionWatchdogTimeout, "two hours");
        var interactiveTimeout = ReadDuration(configuration, InteractiveExecutionWatchdogTimeoutKey,
            DefaultInteractiveExecutionWatchdogTimeout, MaxInteractiveExecutionWatchdogTimeout, "eight hours");
        var readinessProbeTimeout = ReadDuration(configuration, ReadinessProbeTimeoutKey,
            DefaultReadinessProbeTimeout, MaxReadinessProbeTimeout, "ten minutes");
        var startupTimeout = ReadDuration(configuration, StartupTimeoutKey,
            DefaultStartupTimeout, MaxStartupTimeout, "five minutes");

        // A longer class must never receive less time than a quick operation.
        if (longRunningTimeout < watchdogTimeout)
        {
            throw InvalidConfiguration(
                LongRunningExecutionWatchdogTimeoutKey,
                $"must not be shorter than '{ExecutionWatchdogTimeoutKey}'");
        }

        if (interactiveTimeout < watchdogTimeout)
        {
            throw InvalidConfiguration(
                InteractiveExecutionWatchdogTimeoutKey,
                $"must not be shorter than '{ExecutionWatchdogTimeoutKey}'");
        }

        var maxRetainedWorkMiB = configuration.GetValue<int?>(MaxRetainedWorkMiBKey) ?? 32;
        if (maxRetainedWorkMiB is < 1 or > 1024)
            throw InvalidConfiguration(MaxRetainedWorkMiBKey, "must be between 1 and 1024 MiB");
        return new WorkerProcessOptions(executablePath, watchdogTimeout)
        {
            MaxRetainedWorkMiB = maxRetainedWorkMiB,
            LongRunningExecutionWatchdogTimeout = longRunningTimeout,
            InteractiveExecutionWatchdogTimeout = interactiveTimeout,
            ReadinessProbeTimeout = readinessProbeTimeout,
            StartupTimeout = startupTimeout
        };
    }

    private static string ResolveConfiguredExecutable(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw InvalidConfiguration(
                ExecutablePathKey,
                "must identify an existing executable file");
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw InvalidConfiguration(
                ExecutablePathKey,
                "must identify an existing executable file");
        }

        if (!File.Exists(fullPath))
        {
            throw InvalidConfiguration(
                ExecutablePathKey,
                "must identify an existing executable file");
        }

        return fullPath;
    }

    private static TimeSpan ReadDuration(
        IConfiguration configuration,
        string key,
        TimeSpan defaultValue,
        TimeSpan maximum,
        string maximumText)
    {
        var configured = configuration[key];
        if (configured is null)
        {
            return defaultValue;
        }

        if (!TimeSpan.TryParse(
                configured,
                CultureInfo.InvariantCulture,
                out var value) ||
            value <= TimeSpan.Zero ||
            value > maximum)
        {
            throw InvalidConfiguration(
                key,
                $"must be a positive duration no greater than {maximumText}");
        }

        return value;
    }

    private static InvalidOperationException InvalidConfiguration(
        string key,
        string requirement) =>
        new($"Configuration value '{key}' {requirement}.");
}
