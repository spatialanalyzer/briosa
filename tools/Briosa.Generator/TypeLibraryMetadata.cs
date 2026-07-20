using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;

namespace Briosa.Generator;

public sealed record TypeLibraryMetadata(
    string Name,
    Guid TypeLibraryId,
    int Lcid,
    short MajorVersion,
    short MinorVersion,
    SYSKIND SystemKind,
    LIBFLAGS Flags)
{
    [SupportedOSPlatform("windows")]
    public static TypeLibraryMetadata Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The type-library input does not exist.", fullPath);
        }

        Marshal.ThrowExceptionForHR(LoadTypeLibEx(fullPath, RegistrationKind.None, out var typeLibrary));

        nint attributesPointer = 0;
        try
        {
            typeLibrary.GetLibAttr(out attributesPointer);
            var attributes = Marshal.PtrToStructure<TYPELIBATTR>(attributesPointer);
            typeLibrary.GetDocumentation(-1, out var name, out _, out _, out _);

            return new TypeLibraryMetadata(
                name,
                attributes.guid,
                attributes.lcid,
                attributes.wMajorVerNum,
                attributes.wMinorVerNum,
                attributes.syskind,
                attributes.wLibFlags);
        }
        finally
        {
            if (attributesPointer != 0)
            {
                typeLibrary.ReleaseTLibAttr(attributesPointer);
            }

            if (Marshal.IsComObject(typeLibrary))
            {
                _ = Marshal.FinalReleaseComObject(typeLibrary);
            }
        }
    }

    [DllImport("oleaut32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int LoadTypeLibEx(
        string typeLibraryFile,
        RegistrationKind registrationKind,
        [MarshalAs(UnmanagedType.Interface)] out ITypeLib typeLibrary);

    private enum RegistrationKind
    {
        None = 2
    }
}
