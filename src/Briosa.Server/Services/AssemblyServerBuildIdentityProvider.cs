using System.Reflection;
using Briosa.Core.V1Alpha1;
using Briosa.Server.Generated.Sa.V2026_1_0529_7.V1Alpha1;

namespace Briosa.Server.Services;

internal interface IServerBuildIdentityProvider
{
    VersionCoordinates CreateVersionCoordinates();
}

internal sealed class AssemblyServerBuildIdentityProvider : IServerBuildIdentityProvider
{
    internal const string CoreProtocolPackage = "briosa.core.v1alpha1";
    internal const string InteropFingerprint =
        "sha256:E2CDB8A2AA53B55CC96C94D91D537CA1C1F25A39402CF91ABF11B053464B9F42";

    private readonly Assembly _assembly;

    public AssemblyServerBuildIdentityProvider()
        : this(typeof(Program).Assembly)
    {
    }

    internal AssemblyServerBuildIdentityProvider(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _assembly = assembly;
    }

    public VersionCoordinates CreateVersionCoordinates()
    {
        var version = new VersionCoordinates
        {
            BriosaVersion = GetBriosaVersion(_assembly),
            CoreProtocolPackage = CoreProtocolPackage,
            SpatialAnalyzerTarget = TargetCatalogMetadata.SpatialAnalyzerTarget,
            TargetProtocolPackage = TargetCatalogMetadata.TargetProtocolPackage,
            CatalogRevision = TargetCatalogMetadata.CatalogRevision,
            InteropFingerprint = InteropFingerprint
        };
        var sourceRevision = _assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "RepositoryCommit")?.Value;
        if (!string.IsNullOrWhiteSpace(sourceRevision))
        {
            version.SourceRevision = sourceRevision;
        }

        return version;
    }

    private static string GetBriosaVersion(Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ??
        assembly.GetName().Version?.ToString() ??
        "unknown";
}
