using global::Briosa;
using Briosa.Server.Operations;

namespace Briosa.Server.Services;

internal sealed class BuildIdentityProvider : IServerBuildIdentityProvider
{
    internal const string ProtocolPackage = "briosa";
    internal const string InteropFingerprint = "sha256:98F7CA51055497263B3C36384D390784920257ABF3D70C8F1A40CEB6A8FDCFE2";

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
