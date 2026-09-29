using System.Globalization;

namespace Briosa.Server.Workers;

internal sealed record WorkerProcessOptions(
    string ExecutablePath,
    TimeSpan ExecutionWatchdogTimeout)
{
    internal const string ExecutablePathKey = "Briosa:Worker:ExecutablePath";
    internal const string ExecutionWatchdogTimeoutKey =
        "Briosa:Worker:ExecutionWatchdogTimeout";

    private static readonly TimeSpan DefaultExecutionWatchdogTimeout =
        TimeSpan.FromSeconds(30);

    public static WorkerProcessOptions BindAndValidate(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredPath = configuration[ExecutablePathKey];
        var executablePath = configuredPath is null
            ? Path.Combine(AppContext.BaseDirectory, "Briosa.Worker.exe")
            : ResolveConfiguredExecutable(configuredPath);
        var watchdogTimeout = ReadWatchdogTimeout(configuration);
        return new WorkerProcessOptions(executablePath, watchdogTimeout);
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

    private static TimeSpan ReadWatchdogTimeout(IConfiguration configuration)
    {
        var configured = configuration[ExecutionWatchdogTimeoutKey];
        if (configured is null)
        {
            return DefaultExecutionWatchdogTimeout;
        }

        if (!TimeSpan.TryParse(
                configured,
                CultureInfo.InvariantCulture,
                out var value) ||
            value <= TimeSpan.Zero ||
            value > TimeSpan.FromMinutes(10))
        {
            throw InvalidConfiguration(
                ExecutionWatchdogTimeoutKey,
                "must be a positive duration no greater than ten minutes");
        }

        return value;
    }

    private static InvalidOperationException InvalidConfiguration(
        string key,
        string requirement) =>
        new($"Configuration value '{key}' {requirement}.");
}
