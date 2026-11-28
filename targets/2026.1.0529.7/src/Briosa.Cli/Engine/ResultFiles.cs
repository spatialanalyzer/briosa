using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace Briosa.Cli.Engine;

/// <summary>The file operations result delivery needs, so tests can make them fail.</summary>
internal interface IResultFileSystem
{
    /// <summary>
    /// Creates a new file at a path the caller named, and holds it exclusively until it
    /// is disposed. Fails when the path exists. A failure is an <see cref="IOException"/>
    /// whose <see cref="Exception.HResult"/> carries the Win32 error.
    /// </summary>
    ResultFile CreateNew(string path);

    /// <summary>
    /// Creates a new file in the directory <paramref name="components"/> names below
    /// <paramref name="baseDirectory"/>. The base is opened by path once, and its
    /// volume must keep file permissions, or nothing is created and a
    /// <see cref="SpillPermissionsUnsupportedException"/> is thrown. Each component is
    /// then opened, or created, relative to the held handle of its parent, never by
    /// path, as itself rather than what a reparse point leads to; it is refused if it
    /// is a reparse point or not a directory, and held open so it cannot be renamed
    /// until the file exists. Only then does the last component get, through its held
    /// handle, a protected DACL for the current user and SYSTEM only. The file is
    /// created relative to that handle with its own protected DACL for the same two,
    /// and must resolve to exactly the base's final path and the components.
    /// </summary>
    ResultFile CreatePrivate(string baseDirectory, IReadOnlyList<string> components, string fileName);
}

/// <summary>
/// The spill directory is on a volume that does not keep file permissions, so a
/// result written there could not be kept private. Nothing was created.
/// </summary>
internal sealed class SpillPermissionsUnsupportedException : IOException
{
    public SpillPermissionsUnsupportedException()
        : base("The spill directory's volume does not keep file permissions.")
    {
    }

    public SpillPermissionsUnsupportedException(string message)
        : base(message)
    {
    }

    public SpillPermissionsUnsupportedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// A result file created by this process and written once. Removing it goes through
/// its own handle, never through its name, so a file that someone else put at the
/// same path can never be removed in its place.
/// </summary>
internal sealed class ResultFile : IDisposable
{
    private Stream _stream;

    public ResultFile(string path, SafeFileHandle handle)
    {
        Path = path;
        Handle = handle;
        _stream = new FileStream(handle, FileAccess.Write, bufferSize: 0);
    }

    public string Path { get; }

    public SafeFileHandle Handle { get; }

    public Stream Stream => _stream;

    public bool IsClosed { get; private set; }

    /// <summary>Lets a test interpose on writes and on closing.</summary>
    internal void WrapStream(Func<Stream, Stream> wrap) => _stream = wrap(_stream);

    /// <summary>Removes the file, through its handle, if nothing was written to it; then closes it.</summary>
    public void DiscardIfEmpty()
    {
        if (IsClosed)
        {
            return;
        }

        try
        {
            if (RandomAccess.GetLength(Handle) == 0)
            {
                NativeMethods.DeleteOnClose(Handle);
            }
        }
        finally
        {
            Dispose();
        }
    }

    /// <summary>Removes the file through its handle, whatever it holds; then closes it.</summary>
    public void Discard()
    {
        try
        {
            NativeMethods.DeleteOnClose(Handle);
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        if (IsClosed)
        {
            return;
        }

        IsClosed = true;
        _stream.Dispose();
        Handle.Dispose();
    }
}

/// <summary>The real file system.</summary>
internal sealed class ResultFileSystem : IResultFileSystem
{
    private readonly Func<SafeFileHandle, bool> _keepsPermissions;
    private readonly Action<int>? _afterHeld;

    /// <summary>
    /// The real file system. Tests may replace the check that a volume keeps file
    /// permissions, and act each time another spill directory is held: the argument
    /// is how many are held, the base included.
    /// </summary>
    internal ResultFileSystem(Func<SafeFileHandle, bool>? keepsPermissions = null, Action<int>? afterHeld = null)
    {
        _keepsPermissions = keepsPermissions ?? NativeMethods.KeepsPermissions;
        _afterHeld = afterHeld;
    }

    public static ResultFileSystem Instance { get; } = new();

    /// <summary>The base of the per-user spill directory: <c>%LOCALAPPDATA%</c>.</summary>
    public static string DefaultSpillBase =>
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    /// <summary>The managed directories below the base: <c>Briosa\cli\2026.1.0529.7\results</c>.</summary>
    public static IReadOnlyList<string> DefaultSpillComponents { get; } =
        ["Briosa", "cli", BridgeContract.SpatialAnalyzerTarget, "results"];

    // A file named with --output takes the permissions of the directory the caller
    // chose; a spill file gets its own (CreateChildFile).
    public ResultFile CreateNew(string path)
    {
        // Delete access lets the file be removed through this handle; no sharing
        // means no one else can open, rename, or replace it while it is held.
        var handle = NativeMethods.CreateFile(path, NativeMethods.GenericWrite | NativeMethods.Delete, 0,
            securityDescriptor: null, NativeMethods.CreateNew, NativeMethods.FileAttributeNormal | NativeMethods.FlagWriteThrough);
        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastPInvokeError();
            handle.Dispose();
            throw new IOException("The result file could not be created.", unchecked((int)0x8007_0000) | error);
        }

        return new ResultFile(path, handle);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The verified file passes to the caller; any other is discarded through its own handle.")]
    public ResultFile CreatePrivate(string baseDirectory, IReadOnlyList<string> components, string fileName)
    {
        ArgumentNullException.ThrowIfNull(components);
        var held = new List<SafeFileHandle>();
        try
        {
            // The base is the user's own folder and may itself be redirected: the tree
            // is whatever it leads to now. Everything below it is ours and is reached
            // only through held handles, and no component can be a mount point, so the
            // base's volume holds them all.
            held.Add(OpenBase(System.IO.Path.GetFullPath(baseDirectory)));
            if (!_keepsPermissions(held[0]))
            {
                throw new SpillPermissionsUnsupportedException();
            }

            _afterHeld?.Invoke(held.Count);
            for (var index = 0; index < components.Count; index++)
            {
                held.Add(OpenChild(held[^1], components[index], last: index == components.Count - 1));
                _afterHeld?.Invoke(held.Count);
            }

            var user = CurrentUser();
            // Known by construction to be the last component under the base it was
            // reached from, whatever any path leads to by now.
            ProtectDirectory(held[^1], user);
            var expected = System.IO.Path.Combine([NativeMethods.FinalPath(held[0]), .. components, fileName]);
            ResultFile? file = CreateChildFile(held[^1], fileName, PrivateFileSecurity(user));
            try
            {
                // A second check on the construction: the file resolves to the base's tree.
                if (!string.Equals(file.Path, expected, StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException("The result file did not resolve to the verified directory.");
                }

                var verified = file;
                file = null;
                return verified;
            }
            finally
            {
                // Removed through its own handle, never by name.
                file?.Discard();
            }
        }
        finally
        {
            foreach (var handle in held)
            {
                handle.Dispose();
            }
        }
    }

    // Access to look at a directory and to create the next one in it.
    private const uint DirectoryAccess =
        NativeMethods.FileReadAttributes | NativeMethods.FileTraverse | NativeMethods.FileAddSubdirectory;

    // The base, opened by path once, following a redirection of the user's folder.
    private static SafeFileHandle OpenBase(string path)
    {
        var handle = NativeMethods.CreateFile(path, DirectoryAccess,
            NativeMethods.FileShareRead | NativeMethods.FileShareWrite | NativeMethods.FileShareDelete,
            securityDescriptor: null, NativeMethods.OpenExisting, NativeMethods.FlagBackupSemantics);
        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastPInvokeError();
            handle.Dispose();
            throw new IOException("A spill directory could not be opened.", unchecked((int)0x8007_0000) | error);
        }

        return RequireDirectory(handle, managed: false);
    }

    // A managed component, opened or created relative to its held parent, as itself,
    // and without delete sharing, so it cannot be renamed while held. The directory
    // that will hold the file is also opened to have its permissions set.
    private static SafeFileHandle OpenChild(SafeFileHandle parent, string name, bool last) =>
        RequireDirectory(NativeMethods.OpenOrCreateChildDirectory(parent, name,
            DirectoryAccess | (last
                ? NativeMethods.FileAddFile | NativeMethods.ReadControl | NativeMethods.WriteDac | NativeMethods.WriteOwner
                : 0),
            NativeMethods.FileShareRead | NativeMethods.FileShareWrite), managed: true);

    private static SafeFileHandle RequireDirectory(SafeFileHandle handle, bool managed)
    {
        var attributes = File.GetAttributes(handle);
        if ((attributes & FileAttributes.Directory) == 0 || (managed && (attributes & FileAttributes.ReparsePoint) != 0))
        {
            handle.Dispose();
            throw new IOException("A spill directory is a reparse point or not a directory.");
        }

        return handle;
    }

    // The new file, created relative to the held directory: delete access to remove it
    // through this handle, and no sharing, so no one else can open, rename, or replace
    // it while it is held. Its reported path is where it actually is.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The handle passes to the returned file, or is disposed here on failure.")]
    private static ResultFile CreateChildFile(SafeFileHandle directory, string fileName, byte[] securityDescriptor)
    {
        var handle = NativeMethods.CreateChildFile(directory, fileName, NativeMethods.GenericWrite | NativeMethods.Delete,
            securityDescriptor);
        try
        {
            return new ResultFile(NativeMethods.FinalPath(handle), handle);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    private static SecurityIdentifier CurrentUser()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return identity.User ?? throw new IOException("The current user has no security identifier.");
    }

    // Owned by the current user, with a protected DACL that lets only the user and
    // SYSTEM in, and that the files created in it inherit.
    private static void ProtectDirectory(SafeFileHandle directory, SecurityIdentifier user)
    {
        var security = new HandleSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.SetOwner(user);
        foreach (var identity in new[] { user, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null) })
        {
            security.AddAccessRule(new AccessRule<FileSystemRights>(identity, FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None,
                AccessControlType.Allow));
        }

        security.Apply(directory);
    }

    // The new file's own security, independent of anything it would inherit: owned by
    // the current user, with a protected DACL for the user and SYSTEM only.
    private static byte[] PrivateFileSecurity(SecurityIdentifier user)
    {
        var security = new FileSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.SetOwner(user);
        security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            FileSystemRights.FullControl, AccessControlType.Allow));
        return security.GetSecurityDescriptorBinaryForm();
    }

    // File-system security written through an open handle rather than a path.
    private sealed class HandleSecurity() : ObjectSecurity<FileSystemRights>(isContainer: true, ResourceType.FileObject)
    {
        public void Apply(SafeHandle handle) => Persist(handle);
    }
}
