using System.Reflection;
using global::Briosa;
using Briosa.Server.Operations;

namespace Briosa.Server.Services;

internal sealed class AssemblyServerBuildIdentityProvider : IServerBuildIdentityProvider
{
    internal const string ProtocolPackage = "briosa";
    internal const string InteropFingerprint =
        "sha256:E2CDB8A2AA53B55CC96C94D91D537CA1C1F25A39402CF91ABF11B053464B9F42";

    private readonly VersionCoordinates _coordinates;

    public AssemblyServerBuildIdentityProvider()
        : this(typeof(Program).Assembly)
    {
    }

    internal AssemblyServerBuildIdentityProvider(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _coordinates = ReadVersionCoordinates(assembly);
    }

    public VersionCoordinates CreateVersionCoordinates() => _coordinates.Clone();

    private static VersionCoordinates ReadVersionCoordinates(Assembly assembly)
    {
        var version = new VersionCoordinates
        {
            BriosaVersion = GetBriosaVersion(assembly),
            ProtocolPackage = ProtocolPackage,
            SpatialAnalyzerTarget = SpatialAnalyzerApi.TargetVersion,
            InteropFingerprint = InteropFingerprint
        };
        var sourceRevision = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
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
