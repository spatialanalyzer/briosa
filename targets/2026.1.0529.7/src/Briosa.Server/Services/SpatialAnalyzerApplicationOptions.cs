using System.Globalization;

namespace Briosa.Server.Services;

internal sealed record SpatialAnalyzerApplicationOptions(
    string ExecutablePath,
    TimeSpan StartupTimeout,
    TimeSpan ShutdownTimeout)
{
    internal const string ExecutablePathKey =
        "Briosa:SpatialAnalyzer:ExecutablePath";
    internal const string StartupTimeoutKey =
        "Briosa:SpatialAnalyzer:ApplicationStartupTimeout";
    internal const string ShutdownTimeoutKey =
        "Briosa:SpatialAnalyzer:ApplicationShutdownTimeout";

    public static SpatialAnalyzerApplicationOptions BindAndValidate(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var configuredPath = configuration[ExecutablePathKey];
        var executablePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "New River Kinematics",
                "SpatialAnalyzer 2026.1.0529.7",
                "x64",
                "Spatial Analyzer64.exe")
            : ResolveAbsolutePath(configuredPath, ExecutablePathKey);
        return new SpatialAnalyzerApplicationOptions(
            executablePath,
            ReadTimeout(configuration, StartupTimeoutKey, TimeSpan.FromSeconds(30)),
            ReadTimeout(configuration, ShutdownTimeoutKey, TimeSpan.FromSeconds(30)));
    }

    private static string ResolveAbsolutePath(string path, string key)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            if (!Path.IsPathFullyQualified(fullPath) ||
                !string.Equals(
                    Path.GetFileName(fullPath),
                    "Spatial Analyzer64.exe",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException();
            }

            return fullPath;
        }
        catch (Exception exception) when (
            exception is ArgumentException or
                NotSupportedException or
                PathTooLongException or
                InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Configuration value '{key}' must be an absolute path to Spatial Analyzer64.exe.",
                exception);
        }
    }

    private static TimeSpan ReadTimeout(
        IConfiguration configuration,
        string key,
        TimeSpan defaultValue)
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
            value > TimeSpan.FromMinutes(10))
        {
            throw new InvalidOperationException(
                $"Configuration value '{key}' must be a positive duration no greater than ten minutes.");
        }

        return value;
    }
}
