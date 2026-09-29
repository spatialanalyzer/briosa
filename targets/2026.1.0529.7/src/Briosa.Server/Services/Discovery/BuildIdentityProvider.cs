using global::Briosa;
using Briosa.Server.Operations;

namespace Briosa.Server.Services;

internal sealed class BuildIdentityProvider : IServerBuildIdentityProvider
{
    internal const string ProtocolPackage = "briosa";
    internal const string InteropFingerprint = "sha256:E2CDB8A2AA53B55CC96C94D91D537CA1C1F25A39402CF91ABF11B053464B9F42";

    private static readonly VersionCoordinates Coordinates = CreateCoordinates();

    public VersionCoordinates CreateVersionCoordinates() => Coordinates.Clone();

    private static VersionCoordinates CreateCoordinates()
    {
        var coordinates = new VersionCoordinates
        {
            BriosaVersion = ServerBuildIdentity.Version,
            ProtocolPackage = ProtocolPackage,
            SpatialAnalyzerTarget = SpatialAnalyzerApi.TargetVersion,
            InteropFingerprint = InteropFingerprint
        };
        if (!string.IsNullOrEmpty(ServerBuildIdentity.SourceRevision))
            coordinates.SourceRevision = ServerBuildIdentity.SourceRevision;
        return coordinates;
    }
}
