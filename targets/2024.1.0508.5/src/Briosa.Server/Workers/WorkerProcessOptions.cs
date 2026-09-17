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

internal sealed record SpatialAnalyzerConnectionOptions(
    string Host,
    SpatialAnalyzerIdentityOptions Identity)
{
    internal const string HostKey = "Briosa:SpatialAnalyzer:Host";
    internal const string DefaultHost = "localhost";

    public static SpatialAnalyzerConnectionOptions BindAndValidate(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredHost = configuration[HostKey];
        var host = configuredHost is null ? DefaultHost : configuredHost;
        if (!string.Equals(host, DefaultHost, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Configuration value '{HostKey}' must be 'localhost' for the local-only lifecycle contract.");
        }

        return new SpatialAnalyzerConnectionOptions(
            DefaultHost,
            SpatialAnalyzerIdentityOptions.BindAndValidate(configuration));
    }
}

internal sealed record SpatialAnalyzerIdentityOptions(
    OperatorAttestationOptions? ActivatedSdk,
    OperatorAttestationOptions? ConnectedSpatialAnalyzer)
{
    public static SpatialAnalyzerIdentityOptions BindAndValidate(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new SpatialAnalyzerIdentityOptions(
            OperatorAttestationOptions.BindAndValidate(
                configuration,
                ExactTargetIdentityPolicy.ActivatedSdkVersionKey,
                ExactTargetIdentityPolicy.ActivatedSdkReferenceKey),
            OperatorAttestationOptions.BindAndValidate(
                configuration,
                ExactTargetIdentityPolicy.ConnectedSpatialAnalyzerVersionKey,
                ExactTargetIdentityPolicy.ConnectedSpatialAnalyzerReferenceKey));
    }
}

internal sealed record OperatorAttestationOptions(string Version, string Reference)
{
    public static OperatorAttestationOptions? BindAndValidate(
        IConfiguration configuration,
        string versionKey,
        string referenceKey)
    {
        var version = configuration[versionKey];
        var reference = configuration[referenceKey];
        if (string.IsNullOrWhiteSpace(version) && string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(reference))
        {
            throw new InvalidOperationException(
                $"Operator attestation requires both '{versionKey}' and '{referenceKey}'.");
        }

        if (!IsValidVersion(version) ||
            reference.Length > 256 ||
            reference.Contains('\r', StringComparison.Ordinal) ||
            reference.Contains('\n', StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Operator attestation values for '{versionKey}' and '{referenceKey}' have an invalid shape.");
        }

        return new OperatorAttestationOptions(version, reference);
    }

    private static bool IsValidVersion(string version) =>
        version.Length <= 128 &&
        !version.Contains('\r', StringComparison.Ordinal) &&
        !version.Contains('\n', StringComparison.Ordinal);
}
