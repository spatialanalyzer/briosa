using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Briosa.Cli.Engine;

/// <summary>
/// The Win32 and native calls result files need and .NET does not expose: opening a
/// directory without following a reparse point, opening or creating a directory or
/// file relative to a held directory handle rather than by path, creating a file with
/// delete access and its own security, deleting it through its handle, reading the
/// path a handle actually refers to, and asking whether a handle's volume keeps file
/// permissions.
/// </summary>
internal static partial class NativeMethods
{
    public const uint GenericWrite = 0x4000_0000;
    public const uint Delete = 0x0001_0000;
    public const uint FileReadAttributes = 0x0000_0080;
    public const uint FileTraverse = 0x0000_0020;
    public const uint Synchronize = 0x0010_0000;
    public const uint WriteDac = 0x0004_0000;
    public const uint WriteOwner = 0x0008_0000;

    public const uint FileShareRead = 0x1;
    public const uint FileShareWrite = 0x2;
    public const uint FileShareDelete = 0x4;

    public const uint CreateNew = 1;
    public const uint OpenExisting = 3;

    public const uint FileAttributeNormal = 0x0000_0080;
    public const uint FlagWriteThrough = 0x8000_0000;
    public const uint FlagBackupSemantics = 0x0200_0000;
    public const uint FlagOpenReparsePoint = 0x0020_0000;

    public const int ErrorFileNotFound = 2;
    public const int ErrorPathNotFound = 3;
    public const int ErrorAccessDenied = 5;
    public const int ErrorFileExists = 80;
    public const int ErrorAlreadyExists = 183;

    private const int FileDispositionInfoClass = 4;
    private const uint FilePersistentAcls = 0x0000_0008;

    // NtCreateFile dispositions, options, and object attributes.
    private const uint FileCreateDisposition = 2;
    private const uint FileOpenIfDisposition = 3;
    private const uint FileDirectoryFile = 0x0000_0001;
    private const uint FileWriteThroughOption = 0x0000_0002;
    private const uint FileSynchronousIoNonalert = 0x0000_0020;
    private const uint FileNonDirectoryFile = 0x0000_0040;
    private const uint FileOpenReparsePointOption = 0x0020_0000;
    private const uint ObjectCaseInsensitive = 0x0000_0040;

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static unsafe partial SafeFileHandle CreateFileNative(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        SecurityAttributes* securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [LibraryImport("kernel32.dll", EntryPoint = "GetVolumeInformationByHandleW", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool GetVolumeInformationByHandle(
        SafeFileHandle file,
        char* volumeName,
        uint volumeNameSize,
        uint* serialNumber,
        uint* maximumComponentLength,
        uint* fileSystemFlags,
        char* fileSystemName,
        uint fileSystemNameSize);

    private const uint OwnerSecurityInformation = 0x0000_0001;
    private const uint DaclSecurityInformation = 0x0000_0004;

    [LibraryImport("advapi32.dll", EntryPoint = "SetKernelObjectSecurity", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool SetKernelObjectSecurity(SafeFileHandle handle, uint securityInformation,
        byte* securityDescriptor);

    /// <summary>
    /// Writes the owner and the DACL of a self-relative security descriptor to the
    /// object the handle holds, and to nothing else: unlike SetSecurityInfo, it does not
    /// propagate inheritable entries to what is already inside a directory. The DACL's
    /// protection comes from the descriptor's own control flags.
    /// </summary>
    public static unsafe void SetOwnerAndAccessList(SafeFileHandle handle, byte[] securityDescriptor)
    {
        fixed (byte* descriptor = securityDescriptor)
        {
            if (!SetKernelObjectSecurity(handle, OwnerSecurityInformation | DaclSecurityInformation, descriptor))
            {
                throw new IOException("The spill directory's permissions could not be set.",
                    unchecked((int)0x8007_0000) | Marshal.GetLastPInvokeError());
            }
        }
    }

    [LibraryImport("ntdll.dll", EntryPoint = "NtCreateFile")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static unsafe partial int NtCreateFile(
        out SafeFileHandle handle,
        uint desiredAccess,
        ObjectAttributes* objectAttributes,
        IoStatusBlock* ioStatusBlock,
        long* allocationSize,
        uint fileAttributes,
        uint shareAccess,
        uint createDisposition,
        uint createOptions,
        void* eaBuffer,
        uint eaLength);

    [LibraryImport("ntdll.dll", EntryPoint = "RtlNtStatusToDosError")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint RtlNtStatusToDosError(int status);

    /// <summary>
    /// Opens the directory <paramref name="name"/> inside the directory that
    /// <paramref name="parent"/> holds, creating it if it does not exist. The name is
    /// resolved against that directory object itself, never against a path, so a
    /// redirected path cannot change which directory it is. A reparse point at the
    /// name is opened as itself, not followed.
    /// </summary>
    public static SafeFileHandle OpenOrCreateChildDirectory(SafeFileHandle parent, string name, uint desiredAccess,
        uint shareMode) =>
        CreateRelative(parent, name, desiredAccess | Synchronize, 0, shareMode, FileOpenIfDisposition,
            FileDirectoryFile | FileOpenReparsePointOption | FileSynchronousIoNonalert, securityDescriptor: null);

    /// <summary>
    /// Creates the new file <paramref name="name"/> inside the directory that
    /// <paramref name="parent"/> holds, with <paramref name="securityDescriptor"/> and no
    /// sharing. It fails when the name exists.
    /// </summary>
    public static SafeFileHandle CreateChildFile(SafeFileHandle parent, string name, uint desiredAccess,
        byte[] securityDescriptor) =>
        CreateRelative(parent, name, desiredAccess | Synchronize, FileAttributeNormal, 0, FileCreateDisposition,
            FileNonDirectoryFile | FileWriteThroughOption | FileSynchronousIoNonalert, securityDescriptor);

    private static unsafe SafeFileHandle CreateRelative(SafeFileHandle parent, string name, uint desiredAccess,
        uint fileAttributes, uint shareMode, uint disposition, uint options, byte[]? securityDescriptor)
    {
        // One path element: the name can never walk up or across.
        if (name.Length == 0 || name is "." or ".." || name.AsSpan().IndexOfAny('\\', '/', ':') >= 0)
        {
            throw new ArgumentException("A spill path element must be a single name.", nameof(name));
        }

        var added = false;
        parent.DangerousAddRef(ref added);
        try
        {
            fixed (char* characters = name)
            fixed (byte* descriptor = securityDescriptor)
            {
                var objectName = new UnicodeString
                {
                    Length = (ushort)(name.Length * sizeof(char)),
                    MaximumLength = (ushort)(name.Length * sizeof(char)),
                    Buffer = (IntPtr)characters
                };
                var attributes = new ObjectAttributes
                {
                    Length = (uint)sizeof(ObjectAttributes),
                    RootDirectory = parent.DangerousGetHandle(),
                    ObjectName = (IntPtr)(&objectName),
                    Attributes = ObjectCaseInsensitive,
                    SecurityDescriptor = (IntPtr)descriptor,
                    SecurityQualityOfService = IntPtr.Zero
                };
                IoStatusBlock status;
                var result = NtCreateFile(out var handle, desiredAccess, &attributes, &status, null, fileAttributes,
                    shareMode, disposition, options, null, 0);
                if (result < 0)
                {
                    handle.Dispose();
                    throw new IOException("A spill path element could not be opened or created.",
                        unchecked((int)0x8007_0000) | (int)RtlNtStatusToDosError(result));
                }

                return handle;
            }
        }
        finally
        {
            if (added)
            {
                parent.DangerousRelease();
            }
        }
    }

    /// <summary>
    /// Opens or creates a file. A new file gets <paramref name="securityDescriptor"/>,
    /// a self-relative security descriptor, when one is given. The last error stays set
    /// for the caller.
    /// </summary>
    public static unsafe SafeFileHandle CreateFile(string fileName, uint desiredAccess, uint shareMode,
        byte[]? securityDescriptor, uint creationDisposition, uint flagsAndAttributes)
    {
        if (securityDescriptor is null)
        {
            return CreateFileNative(fileName, desiredAccess, shareMode, null, creationDisposition, flagsAndAttributes,
                IntPtr.Zero);
        }

        fixed (byte* descriptor = securityDescriptor)
        {
            var attributes = new SecurityAttributes
            {
                Length = (uint)sizeof(SecurityAttributes),
                SecurityDescriptor = (IntPtr)descriptor,
                InheritHandle = 0
            };
            return CreateFileNative(fileName, desiredAccess, shareMode, &attributes, creationDisposition,
                flagsAndAttributes, IntPtr.Zero);
        }
    }

    /// <summary>Whether the volume that holds the handle's file keeps file permissions (ACLs).</summary>
    public static unsafe bool KeepsPermissions(SafeFileHandle file)
    {
        uint flags = 0;
        if (!GetVolumeInformationByHandle(file, null, 0, null, null, &flags, null, 0))
        {
            throw new IOException("The volume could not be inspected.", Marshal.GetLastPInvokeError());
        }

        return (flags & FilePersistentAcls) != 0;
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetFileInformationByHandle(
        SafeFileHandle file,
        int fileInformationClass,
        ref FileDispositionInfo information,
        uint bufferSize);

    [LibraryImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static unsafe partial uint GetFinalPathNameByHandle(SafeFileHandle file, char* path, uint length, uint flags);

    /// <summary>Marks the file for deletion when its last handle closes. The handle needs delete access.</summary>
    public static void DeleteOnClose(SafeFileHandle file)
    {
        var information = new FileDispositionInfo { DeleteFile = 1 };
        if (!SetFileInformationByHandle(file, FileDispositionInfoClass, ref information,
                (uint)Marshal.SizeOf<FileDispositionInfo>()))
        {
            throw new IOException("The file could not be marked for deletion.", Marshal.GetLastPInvokeError());
        }
    }

    /// <summary>The normalized path the handle refers to, without the <c>\\?\</c> prefix.</summary>
    public static unsafe string FinalPath(SafeFileHandle file)
    {
        var buffer = new char[1024];
        while (true)
        {
            uint length;
            fixed (char* pointer = buffer)
            {
                length = GetFinalPathNameByHandle(file, pointer, (uint)buffer.Length, 0);
            }

            if (length == 0)
            {
                throw new IOException("The file's path could not be read.", Marshal.GetLastPInvokeError());
            }

            if (length < buffer.Length)
            {
                var path = new string(buffer, 0, (int)length);
                return path.StartsWith(@"\\?\UNC\", StringComparison.Ordinal) ? @"\\" + path[8..]
                    : path.StartsWith(@"\\?\", StringComparison.Ordinal) ? path[4..]
                    : path;
            }

            buffer = new char[length];
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileDispositionInfo
    {
        public byte DeleteFile;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SecurityAttributes
    {
        public uint Length;
        public IntPtr SecurityDescriptor;
        public int InheritHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UnicodeString
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ObjectAttributes
    {
        public uint Length;
        public IntPtr RootDirectory;
        public IntPtr ObjectName;
        public uint Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoStatusBlock
    {
        public IntPtr Status;
        public IntPtr Information;
    }
}
