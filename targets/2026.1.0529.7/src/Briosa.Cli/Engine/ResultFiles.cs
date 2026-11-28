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
    /// handle, a protected DACL for the current user and SYSTEM only, set on it alone:
    /// nothing already inside it changes. The file is created relative to that handle
    /// with its own protected DACL for the same two, and must resolve to exactly the
    /// base's final path and the components. A file it created is removed through its
    /// own handle if anything fails before the file is handed over; if that removal
    /// fails too, a <see cref="ResultFileLeftException"/> names the file left behind.
    /// </summary>
    ResultFile CreatePrivate(string baseDirectory, IReadOnlyList<string> components, string fileName);
}

/// <summary>
/// The operations on an open result file's handle: writing, reading back, flushing to
/// the storage device, marking for removal, and closing. Tests replace them to make
/// any one of them fail.
/// </summary>
internal interface IResultFileIo
{
    /// <summary>Writes all of <paramref name="bytes"/> at <paramref name="offset"/>, or throws.</summary>
    void Write(SafeFileHandle file, ReadOnlySpan<byte> bytes, long offset);

    long GetLength(SafeFileHandle file);

    int Read(SafeFileHandle file, Span<byte> buffer, long offset);

    /// <summary>Asks the operating system to write the file's buffered data and metadata to the storage device.</summary>
    void FlushToDisk(SafeFileHandle file);

    /// <summary>Marks the file to be removed when its handle closes.</summary>
    void DeleteOnClose(SafeFileHandle file);

    void Close(SafeFileHandle file);
}

/// <summary>The real handle operations.</summary>
internal sealed class SystemResultFileIo : IResultFileIo
{
    public static SystemResultFileIo Instance { get; } = new();

    public void Write(SafeFileHandle file, ReadOnlySpan<byte> bytes, long offset) => RandomAccess.Write(file, bytes, offset);

    public long GetLength(SafeFileHandle file) => RandomAccess.GetLength(file);

    public int Read(SafeFileHandle file, Span<byte> buffer, long offset) => RandomAccess.Read(file, buffer, offset);

    // FlushFileBuffers on Windows: the file's data and metadata, to the device.
    public void FlushToDisk(SafeFileHandle file) => RandomAccess.FlushToDisk(file);

    public void DeleteOnClose(SafeFileHandle file) => NativeMethods.DeleteOnClose(file);

    public void Close(SafeFileHandle file) => file.Dispose();
}

/// <summary>
/// A file this process created could be neither handed over nor removed through its
/// handle. It stays at <see cref="Path"/>, the path it was created at, with nothing
/// written to it.
/// </summary>
internal sealed class ResultFileLeftException : IOException
{
    public ResultFileLeftException()
        : base("A new result file could be neither handed over nor removed.")
    {
    }

    public ResultFileLeftException(string message)
        : base(message)
    {
    }

    public ResultFileLeftException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public string Path { get; init; } = string.Empty;
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

/// <summary>What reading a written result file back through its own handle showed.</summary>
internal enum ResultContent
{
    /// <summary>The file holds exactly the bytes that were meant to be written.</summary>
    Complete,

    /// <summary>The file is shorter, or its bytes differ: it is known not to hold the result.</summary>
    Incomplete,

    /// <summary>The file could not be read back, so what it holds is not known.</summary>
    Unknown
}

/// <summary>
/// A result file created by this process and written once, through its handle.
/// Removing it goes through that handle, never through its name, so a file that
/// someone else put at the same path can never be removed in its place. It is opened
/// for reading too, so that what was written can be read back before it is trusted.
/// </summary>
internal sealed class ResultFile : IDisposable
{
    private readonly IResultFileIo _io;

    public ResultFile(string path, SafeFileHandle handle, IResultFileIo? io = null)
    {
        Path = path;
        Handle = handle;
        _io = io ?? SystemResultFileIo.Instance;
    }

    public string Path { get; }

    public SafeFileHandle Handle { get; }

    public bool IsClosed { get; private set; }

    /// <summary>Writes <paramref name="bytes"/> from the start of the file, or throws.</summary>
    public void Write(ReadOnlySpan<byte> bytes)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        _io.Write(Handle, bytes, 0);
    }

    /// <summary>
    /// Reads the file back through its own handle and compares every byte with
    /// <paramref name="expected"/>, before the file is closed.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "A read-back that fails in any way leaves the content unknown, never complete.")]
    public ResultContent ReadBack(byte[] expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        if (IsClosed)
        {
            return ResultContent.Unknown;
        }

        try
        {
            if (_io.GetLength(Handle) != expected.Length)
            {
                return ResultContent.Incomplete;
            }

            var actual = new byte[expected.Length];
            var total = 0;
            while (total < actual.Length)
            {
                var read = _io.Read(Handle, actual.AsSpan(total), total);
                if (read == 0)
                {
                    return ResultContent.Incomplete;
                }

                total += read;
            }

            return actual.AsSpan().SequenceEqual(expected) ? ResultContent.Complete : ResultContent.Incomplete;
        }
        catch (Exception)
        {
            return ResultContent.Unknown;
        }
    }

    /// <summary>The file's length through its own handle, or null when it cannot be read.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "A length that cannot be read is reported as unknown.")]
    public long? Length()
    {
        try
        {
            return IsClosed ? null : _io.GetLength(Handle);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Asks the operating system to write the file's data and metadata to the storage device, or throws.</summary>
    public void FlushToDisk()
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        _io.FlushToDisk(Handle);
    }

    /// <summary>
    /// Marks the file for removal through its handle, then closes it. True when it was
    /// marked, so it is gone once the handle is closed; false when it stays.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "A removal that fails in any way means the file stays, which the caller reports.")]
    public bool Remove()
    {
        if (IsClosed)
        {
            return false;
        }

        var marked = false;
        try
        {
            _io.DeleteOnClose(Handle);
            marked = true;
        }
        catch (Exception)
        {
            // The file stays; the caller reports it.
        }

        TryClose();
        return marked;
    }

    /// <summary>
    /// Removes the file through its handle if nothing was written to it, then closes
    /// it. True when it was removed.
    /// </summary>
    public bool RemoveIfEmpty()
    {
        if (IsClosed)
        {
            return false;
        }

        if (Length() == 0)
        {
            return Remove();
        }

        TryClose();
        return false;
    }

    /// <summary>Closes the file. True when nothing reported an error.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "An error while closing is reported to the caller as a false result.")]
    public bool TryClose()
    {
        try
        {
            Dispose();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (IsClosed)
        {
            return;
        }

        IsClosed = true;
        try
        {
            _io.Close(Handle);
        }
        finally
        {
            // The handle closes even when closing reported an error.
            Handle.Dispose();
        }
    }
}

/// <summary>The steps of creating a spill file before the file exists, so tests can make each fail.</summary>
internal enum SpillStep
{
    /// <summary>Opening, or creating, one managed directory.</summary>
    OpenDirectory,

    /// <summary>Setting the owner and permissions of <c>results</c>.</summary>
    Protect,

    /// <summary>Creating the file.</summary>
    CreateFile
}

/// <summary>The real file system.</summary>
internal sealed class ResultFileSystem : IResultFileSystem
{
    private readonly Func<SafeFileHandle, bool> _keepsPermissions;
    private readonly Action<int>? _afterHeld;
    private readonly Func<SafeFileHandle, string> _finalPath;
    private readonly Func<SafeFileHandle, FileAttributes> _attributes;
    private readonly IResultFileIo _io;
    private readonly Action<SpillStep>? _beforeStep;

    /// <summary>
    /// The real file system. Tests may replace the check that a volume keeps file
    /// permissions, act each time another spill directory is held (the argument is how
    /// many are held, the base included), replace the queries of a handle's final path
    /// and attributes, replace the operations on a file's handle, and act before each
    /// step of creating a spill file, to make any of them fail.
    /// </summary>
    internal ResultFileSystem(Func<SafeFileHandle, bool>? keepsPermissions = null, Action<int>? afterHeld = null,
        Func<SafeFileHandle, string>? finalPath = null, Func<SafeFileHandle, FileAttributes>? attributes = null,
        IResultFileIo? io = null, Action<SpillStep>? beforeStep = null)
    {
        _keepsPermissions = keepsPermissions ?? NativeMethods.KeepsPermissions;
        _afterHeld = afterHeld;
        _finalPath = finalPath ?? NativeMethods.FinalPath;
        _attributes = attributes ?? File.GetAttributes;
        _io = io ?? SystemResultFileIo.Instance;
        _beforeStep = beforeStep;
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The handle passes to the returned file, whose constructor only stores it.")]
    public ResultFile CreateNew(string path)
    {
        // Read access to read it back, delete access to remove it through this handle;
        // no sharing means no one else can open, rename, or replace it while it is held.
        var handle = NativeMethods.CreateFile(path, NativeMethods.GenericRead | NativeMethods.GenericWrite | NativeMethods.Delete, 0,
            securityDescriptor: null, NativeMethods.CreateNew, NativeMethods.FileAttributeNormal | NativeMethods.FlagWriteThrough);
        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastPInvokeError();
            handle.Dispose();
            throw new IOException("The result file could not be created.", unchecked((int)0x8007_0000) | error);
        }

        return new ResultFile(path, handle, _io);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The new file's handle passes to HandOver, which returns it in a file or removes and closes it.")]
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
                _beforeStep?.Invoke(SpillStep.OpenDirectory);
                held.Add(OpenChild(held[^1], components[index], last: index == components.Count - 1));
                _afterHeld?.Invoke(held.Count);
            }

            var user = CurrentUser();
            // Known by construction to be the last component under the base it was
            // reached from, whatever any path leads to by now.
            _beforeStep?.Invoke(SpillStep.Protect);
            ProtectDirectory(held[^1], user);
            var expected = System.IO.Path.Combine([_finalPath(held[0]), .. components, fileName]);
            _beforeStep?.Invoke(SpillStep.CreateFile);
            // Read access to read it back, delete access to remove it through this
            // handle, and no sharing, so no one else can open, rename, or replace it
            // while it is held.
            return HandOver(NativeMethods.CreateChildFile(held[^1], fileName,
                NativeMethods.GenericRead | NativeMethods.GenericWrite | NativeMethods.Delete, PrivateFileSecurity(user)), expected);
        }
        finally
        {
            foreach (var handle in held)
            {
                handle.Dispose();
            }
        }
    }

    // The new file becomes a ResultFile only at its verified path: a second check on the
    // construction, which already puts it in the base's tree at the expected path. On
    // any failure before that, it is removed through its own handle, never by name;
    // when that fails too, the exception names the file left behind at the path it was
    // created at, so that the envelope can report it.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The handle passes to the returned file, or is removed and closed here on failure.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Any failure before the hand-over removes the file; the failure itself is rethrown.")]
    private ResultFile HandOver(SafeFileHandle handle, string expected)
    {
        try
        {
            var actual = _finalPath(handle);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("The result file did not resolve to the verified directory.");
            }

            return new ResultFile(actual, handle, _io);
        }
        catch (Exception failure)
        {
            var marked = false;
            try
            {
                _io.DeleteOnClose(handle);
                marked = true;
            }
            catch (Exception)
            {
                // Reported below.
            }

            try
            {
                _io.Close(handle);
            }
            catch (Exception)
            {
                // A removal marked on the handle happens when the handle closes, below.
            }

            handle.Dispose();
            if (!marked)
            {
                throw new ResultFileLeftException("A new result file could be neither handed over nor removed.", failure)
                {
                    Path = expected
                };
            }

            throw;
        }
    }

    // Access to look at a directory and pass through it. Creating the next directory, or
    // the file, in it needs no right on this handle: the file system checks the
    // directory's own permissions when, and only when, something is created.
    private const uint DirectoryAccess = NativeMethods.FileReadAttributes | NativeMethods.FileTraverse;

    // The base, opened by path once, following a redirection of the user's folder.
    private SafeFileHandle OpenBase(string path)
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
    // and without delete sharing, so it cannot be renamed while held. Only the directory
    // that will hold the file is opened to have its owner and permissions set. A
    // directory this creates is kept, as after a successful spill.
    private SafeFileHandle OpenChild(SafeFileHandle parent, string name, bool last) =>
        RequireDirectory(NativeMethods.OpenOrCreateChildDirectory(parent, name,
            DirectoryAccess | (last ? NativeMethods.WriteDac | NativeMethods.WriteOwner : 0),
            NativeMethods.FileShareRead | NativeMethods.FileShareWrite), managed: true);

    // The handle is returned only for a directory that is not a reparse point; on any
    // failure, including of the check itself, it is closed here.
    private SafeFileHandle RequireDirectory(SafeFileHandle handle, bool managed)
    {
        try
        {
            var attributes = _attributes(handle);
            if ((attributes & FileAttributes.Directory) == 0 || (managed && (attributes & FileAttributes.ReparsePoint) != 0))
            {
                throw new IOException("A spill directory is a reparse point or not a directory.");
            }

            return handle;
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
    // SYSTEM in, and that what is created in it from now on inherits. It is written to
    // the held directory alone (SetKernelObjectSecurity), never propagated: files and
    // directories already inside keep their own owner and access lists.
    private static void ProtectDirectory(SafeFileHandle directory, SecurityIdentifier user)
    {
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.SetOwner(user);
        foreach (var identity in new[] { user, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null) })
        {
            security.AddAccessRule(new FileSystemAccessRule(identity, FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None,
                AccessControlType.Allow));
        }

        NativeMethods.SetOwnerAndAccessList(directory, security.GetSecurityDescriptorBinaryForm());
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

}
