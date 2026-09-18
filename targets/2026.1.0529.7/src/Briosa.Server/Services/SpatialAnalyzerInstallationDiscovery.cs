using System.Diagnostics;
using System.Security;
using Microsoft.Win32;
using Briosa.Server.Operations;

namespace Briosa.Server.Services;

internal sealed record SpatialAnalyzerInstallation(string? ExecutablePath, string? DiagnosticCode);

internal static class SpatialAnalyzerInstallationDiscovery
{
    private const string ExecutableName = "Spatial Analyzer64.exe";

    internal static SpatialAnalyzerInstallation Resolve(string? explicitPath, string defaultPath) =>
        Select(explicitPath, defaultPath, ReadInstalledDirectories(), File.Exists, FileVersion);

    internal static SpatialAnalyzerInstallation Select(
        string? explicitPath,
        string defaultPath,
        IEnumerable<string> directories,
        Func<string, bool> exists,
        Func<string, string?> version)
    {
        if (explicitPath is not null)
        {
            return Check(explicitPath, exists, version);
        }

        var candidates = directories.Where(IsAbsoluteLocalPath)
            .SelectMany(directory => new[]
            {
                Path.Combine(directory, ExecutableName),
                Path.Combine(directory, "x64", ExecutableName)
            })
            .Append(defaultPath)
            .Where(IsAbsoluteLocalPath)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(path => Check(path, exists, version).DiagnosticCode is null)
            .ToArray();
        return candidates.Length switch
        {
            0 => new(null, "spatial-analyzer-installation-not-found"),
            1 => new(candidates[0], null),
            _ => new(null, "spatial-analyzer-installation-ambiguous")
        };
    }

    internal static SpatialAnalyzerInstallation Check(string path) => Check(path, File.Exists, FileVersion);

    private static SpatialAnalyzerInstallation Check(
        string path, Func<string, bool> exists, Func<string, string?> version)
    {
        if (!IsAbsoluteLocalPath(path) ||
            !string.Equals(Path.GetFileName(path), ExecutableName, StringComparison.OrdinalIgnoreCase))
        {
            return new(null, "spatial-analyzer-executable-invalid");
        }

        if (!exists(path))
        {
            return new(null, "spatial-analyzer-installation-not-found");
        }

        if (!MatchesTarget(version(path)))
        {
            return new(null, "spatial-analyzer-file-version-mismatch");
        }

        return new(Path.GetFullPath(path), null);
    }

    internal static bool MatchesTarget(string? value) =>
        Version.TryParse(value?.Replace(',', '.').Replace(" ", "", StringComparison.Ordinal), out var actual) &&
        actual == Version.Parse(SpatialAnalyzerApi.TargetVersion);

    private static bool IsAbsoluteLocalPath(string path) =>
        Path.IsPathFullyQualified(path) && !path.StartsWith(@"\\", StringComparison.Ordinal);

    private static string? FileVersion(string path)
    {
        try
        {
            return FileVersionInfo.GetVersionInfo(path).FileVersion;
        }
        catch (Exception exception) when (ReadFailure(exception) || exception is ArgumentException)
        {
            return null;
        }
    }

    private static List<string> ReadInstalledDirectories()
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        var result = new List<string>();
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            {
                try
                {
                    using var root = RegistryKey.OpenBaseKey(hive, view);
                    using var uninstall = root.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Uninstall");
                    if (uninstall is null)
                    {
                        continue;
                    }

                    foreach (var name in uninstall.GetSubKeyNames().Take(4096))
                    {
                        try
                        {
                            using var entry = uninstall.OpenSubKey(name);
                            if (entry?.GetValue("DisplayName") is not string display ||
                                !(display.Contains("SpatialAnalyzer", StringComparison.OrdinalIgnoreCase) ||
                                  display.Contains("Spatial Analyzer", StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }

                            if (entry.GetValue("InstallLocation") is string directory)
                            {
                                result.Add(directory.Trim().Trim('"'));
                            }

                            if (entry.GetValue("DisplayIcon") is string icon)
                            {
                                var path = IconPath(icon);
                                if (path is not null && File.Exists(path) && Path.GetDirectoryName(path) is { } parent)
                                {
                                    result.Add(parent);
                                }
                            }
                        }
                        catch (Exception exception) when (ReadFailure(exception))
                        {
                            // Other registrations and the corroborated default remain candidates.
                        }
                    }
                }
                catch (Exception exception) when (ReadFailure(exception))
                {
                    // Unavailable registration is never evidence of a matching installation.
                }
            }
        }

        return result;
    }

    internal static string? IconPath(string value)
    {
        var path = value.Trim();
        if (path.StartsWith('"'))
        {
            var end = path.IndexOf('"', 1);
            if (end < 0)
            {
                return null;
            }

            path = path[1..end];
        }
        else
        {
            var comma = path.LastIndexOf(',');
            if (comma >= 0 && int.TryParse(path[(comma + 1)..].Trim(), out _))
            {
                path = path[..comma].Trim();
            }
        }

        return IsAbsoluteLocalPath(path) ? path : null;
    }

    private static bool ReadFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or SecurityException;
}
