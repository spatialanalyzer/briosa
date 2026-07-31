using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Security;

namespace Briosa.Server.Operations;

/// <summary>
/// Identifies the exact SpatialAnalyzer API implemented by this Briosa build.
/// </summary>
internal static class SpatialAnalyzerApi
{
    public const string TargetVersion = "2026.1.0529.7";
    public const string TargetProtocolPackage = "briosa.sa.v2026_1_0529_7.v1alpha1";

    public static IReadOnlyList<OperationDescriptor> Operations { get; } =
        [GetWorkingDirectoryOperation.Descriptor];
}
