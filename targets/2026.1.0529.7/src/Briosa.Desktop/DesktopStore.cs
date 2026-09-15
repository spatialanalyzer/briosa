using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;

namespace Briosa.Desktop;

public sealed record DesktopRegistration(string Instance, string PackageDirectory, int ProcessId, long StartTimeUtcTicks);
public sealed record DesktopOwnership(string Instance, string Credential);
public sealed record DesktopPreferences(bool Notifications = true, string Theme = "system");
public sealed record DesktopExit(string Instance, bool Clean);

/// <summary>Private rendezvous and preferences, separate from immutable product files and exports.</summary>
public static class DesktopStore
{
    public static string PackageDirectory(string applicationDirectory)
    {
        var directory = new DirectoryInfo(applicationDirectory);
        return directory.Name.Equals("desktop", StringComparison.OrdinalIgnoreCase) && directory.Parent is { } parent &&
            File.Exists(Path.Combine(parent.FullName, "Briosa.Server.exe")) ? parent.FullName : directory.FullName;
    }
    public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Briosa", "ControlCenter", DesktopProtocol.Target);

    public static string EnsurePrivateDirectory(string path)
    {
        var directory = Directory.CreateDirectory(path);
        if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Desktop state cannot be a reparse point.");
        using var identity = WindowsIdentity.GetCurrent();
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.SetOwner(identity.User!);
        security.AddAccessRule(new FileSystemAccessRule(identity.User!, FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None, AccessControlType.Allow));
        directory.SetAccessControl(security);
        return path;
    }

    public static void Register(DesktopRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        if (!DesktopProtocol.IsInstance(registration.Instance)) throw new ArgumentException("Invalid instance.", nameof(registration));
        Write(Path.Combine(EnsurePrivateDirectory(Root), registration.Instance + ".instance.json"), registration);
        PruneExpired();
    }

    public static void RemoveRegistration(string instance)
    {
        if (!DesktopProtocol.IsInstance(instance)) return;
        File.Delete(Path.Combine(Root, instance + ".instance.json"));
    }

    public static void MarkExit(string instance, bool clean)
    {
        if (!DesktopProtocol.IsInstance(instance)) return;
        Write(Path.Combine(EnsurePrivateDirectory(Root), instance + ".exit.json"), new DesktopExit(instance, clean));
        RemoveRegistration(instance);
    }

    public static DesktopExit? ReadExit(string instance) => DesktopProtocol.IsInstance(instance)
        ? Read<DesktopExit>(Path.Combine(Root, instance + ".exit.json")) : null;

    public static DesktopRegistration? ReadRegistration(string instance) => DesktopProtocol.IsInstance(instance)
        ? Read<DesktopRegistration>(Path.Combine(Root, instance + ".instance.json")) : null;

    private static void PruneExpired()
    {
        foreach (var path in Directory.EnumerateFiles(Root, "*.json").Take(512))
        {
            try
            {
                var name = Path.GetFileName(path);
                var instance = name.Split('.')[0];
                if (!DesktopProtocol.IsInstance(instance) || File.GetLastWriteTimeUtc(path) > DateTime.UtcNow.AddDays(-7)) continue;
                var registration = ReadRegistration(instance);
                if (registration is not null && IsAlive(registration)) continue;
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0) File.Delete(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException) { }
        }
    }

    public static DesktopRegistration? Find(string packageDirectory)
    {
        if (!Directory.Exists(Root)) return null;
        foreach (var path in Directory.EnumerateFiles(Root, "*.instance.json").OrderByDescending(File.GetLastWriteTimeUtc).Take(128))
        {
            try
            {
                var item = Read<DesktopRegistration>(path);
                if (item is not null && DesktopProtocol.IsInstance(item.Instance) &&
                    string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(item.PackageDirectory)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(packageDirectory)), StringComparison.OrdinalIgnoreCase) && IsAlive(item))
                    return item;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException) { }
        }
        return null;
    }

    public static bool IsAlive(DesktopRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        try
        {
            using var process = Process.GetProcessById(registration.ProcessId);
            return process.StartTime.ToUniversalTime().Ticks == registration.StartTimeUtcTicks &&
                string.Equals(process.MainModule?.FileName, Path.Combine(registration.PackageDirectory, "Briosa.Server.exe"), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception) { return false; }
    }

    public static void SaveOwnership(DesktopOwnership ownership)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        if (!DesktopProtocol.IsInstance(ownership.Instance)) throw new ArgumentException("Invalid instance.", nameof(ownership));
        Write(Path.Combine(EnsurePrivateDirectory(Root), ownership.Instance + ".owner.json"), ownership);
    }

    public static DesktopOwnership? ReadOwnership(string instance) => DesktopProtocol.IsInstance(instance)
        ? Read<DesktopOwnership>(Path.Combine(Root, instance + ".owner.json")) : null;

    public static void RemoveOwnership(string instance)
    {
        if (DesktopProtocol.IsInstance(instance)) File.Delete(Path.Combine(Root, instance + ".owner.json"));
    }

    public static DesktopPreferences ReadPreferences() => Read<DesktopPreferences>(Path.Combine(Root, "preferences.json")) ?? new();
    public static void SavePreferences(DesktopPreferences preferences) =>
        Write(Path.Combine(EnsurePrivateDirectory(Root), "preferences.json"), preferences);

    private static string IdentitySettingsName(string packageDirectory) => "identity-" + Convert.ToHexString(
        System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(packageDirectory)).ToUpperInvariant()))) + ".json";

    public static DesktopIdentitySettings ReadIdentitySettings(string packageDirectory)
    {
        var settings = Read<DesktopIdentitySettings>(Path.Combine(Root, IdentitySettingsName(packageDirectory))) ?? new();
        settings.Validate();
        return settings;
    }

    public static void SaveIdentitySettings(string packageDirectory, DesktopIdentitySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        Write(Path.Combine(EnsurePrivateDirectory(Root), IdentitySettingsName(packageDirectory)), settings);
    }

    private static T? Read<T>(string path)
    {
        if (!File.Exists(path)) return default;
        var info = new FileInfo(path);
        if (info.Length > 16 * 1024 || (info.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Invalid desktop state file.");
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
    }

    private static void Write<T>(string path, T value)
    {
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Invalid desktop state file.");
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(value));
            File.Move(temporary, path, overwrite: true);
        }
        finally { File.Delete(temporary); }
    }
}
