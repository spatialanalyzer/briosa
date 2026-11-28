using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Briosa.Cli.Engine;

/// <summary>
/// The Win32 calls result files need and .NET does not expose: opening a directory
/// without following a reparse point, creating a file with delete access and its own
/// security, deleting it through its handle, reading the path a handle actually
/// refers to, and asking whether a handle's volume keeps file permissions.
/// </summary>
internal static partial class NativeMethods
{
    public const uint GenericWrite = 0x4000_0000;
    public const uint Delete = 0x0001_0000;
    public const uint FileReadAttributes = 0x0000_0080;
    public const uint ReadControl = 0x0002_0000;
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
}
