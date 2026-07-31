using System.Reflection;
using global::Briosa;
using Briosa.Server.Operations;

namespace Briosa.Server.Services;

internal interface IServerBuildIdentityProvider
{
    VersionCoordinates CreateVersionCoordinates();
}

internal sealed class AssemblyServerBuildIdentityProvider : IServerBuildIdentityProvider
{
    internal const string ProtocolPackage = "briosa";
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
            ProtocolPackage = ProtocolPackage,
            SpatialAnalyzerTarget = SpatialAnalyzerApi.TargetVersion,
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
