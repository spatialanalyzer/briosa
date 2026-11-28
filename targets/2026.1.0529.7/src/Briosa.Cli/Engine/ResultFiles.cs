using System.Security.AccessControl;
using System.Security.Principal;

namespace Briosa.Cli.Engine;

/// <summary>The file operations result delivery needs, so tests can make them fail.</summary>
internal interface IResultFileSystem
{
    /// <summary>Creates a new file; fails when it already exists.</summary>
    Stream CreateNew(string path);

    /// <summary>Deletes a file this process created, only while it is still empty.</summary>
    void DeleteIfEmpty(string path);

    /// <summary>Creates the directory if needed with a DACL for the current user and SYSTEM only.</summary>
    void EnsurePrivateDirectory(string path);
}

/// <summary>The real file system.</summary>
internal sealed class ResultFileSystem : IResultFileSystem
{
    public static ResultFileSystem Instance { get; } = new();

    /// <summary>
    /// The per-user spill directory: <c>%LOCALAPPDATA%\Briosa\cli\2026.1.0529.7\results</c>.
    /// </summary>
    public static string DefaultSpillDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Briosa", "cli", BridgeContract.SpatialAnalyzerTarget, "results");

    public Stream CreateNew(string path) =>
        new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.WriteThrough);

    public void DeleteIfEmpty(string path)
    {
        var file = new FileInfo(path);
        if (file.Exists && file.Length == 0 && (file.Attributes & FileAttributes.ReparsePoint) == 0)
        {
            file.Delete();
        }
    }

    public void EnsurePrivateDirectory(string path)
    {
        var directory = Directory.CreateDirectory(path);
        if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new IOException("The spill directory cannot be a reparse point.");
        }

        using var identity = WindowsIdentity.GetCurrent();
        var user = identity.User ?? throw new IOException("The current user has no security identifier.");
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.SetOwner(user);
        security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None,
            AccessControlType.Allow));
        security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None, AccessControlType.Allow));
        directory.SetAccessControl(security);
    }
}
