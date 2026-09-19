using System.Diagnostics;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;
using Briosa.Server.Operations;

namespace Briosa.Server.Services;

internal sealed record SpatialAnalyzerInstallation(string? ExecutablePath, string? DiagnosticCode);

internal static class SpatialAnalyzerInstallationDiscovery
{
    private const string ExecutableName = "Spatial Analyzer64.exe";

    internal static SpatialAnalyzerInstallation Resolve(string? explicitPath, string defaultPath)
    {
        if (explicitPath is not null) return Check(explicitPath);
        var elevated = OperatingSystem.IsWindows() &&
            new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        return Select(null, defaultPath, ReadInstalledDirectories(elevated), File.Exists, FileVersion,
            elevated ? ProtectedExecutable : null);
    }

    internal static SpatialAnalyzerInstallation Select(
        string? explicitPath,
        string defaultPath,
        IEnumerable<string> directories,
        Func<string, bool> exists,
        Func<string, string?> version,
        Func<string, bool>? automaticCandidateAllowed = null)
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
            .Where(path => automaticCandidateAllowed?.Invoke(path) ?? true)
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
        Path.IsPathFullyQualified(path) && !path.StartsWith(@"\\", StringComparison.Ordinal) && !path.StartsWith("//", StringComparison.Ordinal);

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

    private static List<string> ReadInstalledDirectories(bool machineOnly)
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
                if (machineOnly && hive == RegistryHive.CurrentUser) continue;
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

    private static bool ProtectedExecutable(string path)
    {
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            const FileSystemRights changes = FileSystemRights.Write | FileSystemRights.Delete |
                FileSystemRights.DeleteSubdirectoriesAndFiles | FileSystemRights.ChangePermissions | FileSystemRights.TakeOwnership;
            static bool Trusted(string sid) => sid is "S-1-5-32-544" or "S-1-5-18" or
                "S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464";
            FileSystemInfo item = new FileInfo(path);
            while (item is not DirectoryInfo { Parent: null })
            {
                if ((item.Attributes & FileAttributes.ReparsePoint) != 0) return false;
                var acl = item is FileInfo file ? (FileSystemSecurity)file.GetAccessControl() : ((DirectoryInfo)item).GetAccessControl();
                if (acl.GetOwner(typeof(SecurityIdentifier)) is not SecurityIdentifier owner || !Trusted(owner.Value)) return false;
                foreach (FileSystemAccessRule rule in acl.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                    if (rule.AccessControlType == AccessControlType.Allow && (rule.FileSystemRights & changes) != 0 &&
                        !Trusted(rule.IdentityReference.Value)) return false;
                item = item is FileInfo leaf ? leaf.Directory! : ((DirectoryInfo)item).Parent!;
            }
            return true;
        }
        catch (Exception exception) when (ReadFailure(exception)) { return false; }
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
