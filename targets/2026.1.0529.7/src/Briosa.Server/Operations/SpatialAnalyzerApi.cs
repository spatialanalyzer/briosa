using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Operations.WaveA;
using Briosa.Server.Operations.Variables;
using Briosa.Server.Security;

namespace Briosa.Server.Operations;

/// <summary>
/// Identifies the exact SpatialAnalyzer API implemented by this Briosa build.
/// </summary>
internal static class SpatialAnalyzerApi
{
    public const string TargetVersion = "2026.1.0529.7";
    public const string ProtocolPackage = "briosa";

    public static IReadOnlyList<OperationDescriptor> Operations { get; } =
        [
            .. MpOperationCatalog.Operations.Select(operation => operation.Descriptor),
            GetActiveCollectionNameOperation.Descriptor,
            SetDoubleVariableOperation.Descriptor,
            GetDoubleVariableOperation.Descriptor,
            SetNamedDoubleListVariableOperation.Descriptor,
            GetNamedDoubleListVariableOperation.Descriptor,
        ];
}
