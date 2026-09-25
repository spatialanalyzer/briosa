using Briosa.Server.Operations;
using Briosa.Server.Services;

namespace Briosa.Server.Tests;

public sealed class InstallationDiscoveryTests
{
    [Fact]
    public void AutomaticApplicationSelectionHonorsScopeProtectionAndRejectsNetworkPaths()
    {
        var selected = SpatialAnalyzerInstallationDiscovery.Select(null, @"C:\SA\Spatial Analyzer64.exe",
            [@"C:\SA"], _ => true, _ => SpatialAnalyzerApi.TargetVersion, _ => false);
        Assert.Null(selected.ExecutablePath);
        var explicitNetwork = SpatialAnalyzerInstallationDiscovery.Select("//host/share/Spatial Analyzer64.exe",
            @"C:\SA\Spatial Analyzer64.exe", [], _ => throw new InvalidOperationException("Network I/O attempted"),
            _ => throw new InvalidOperationException("Network I/O attempted"));
        Assert.Equal("spatial-analyzer-executable-invalid", explicitNetwork.DiagnosticCode);
    }

    [Fact]
    public void MissingSaDoesNotPreventInertHostDiscovery()
    {
        var selected = SpatialAnalyzerInstallationDiscovery.Select(null, @"C:\missing\Spatial Analyzer64.exe",
            [], _ => false, _ => null);
        Assert.Null(selected.ExecutablePath);
        Assert.Equal("spatial-analyzer-installation-not-found", selected.DiagnosticCode);
        Assert.Equal(2U, ServerCompatibility.Create().Major);
    }

    [Fact]
    public void MultipleExactTargetCopiesRequireExplicitSelection()
    {
        var selected = SpatialAnalyzerInstallationDiscovery.Select(null, @"C:\one\Spatial Analyzer64.exe",
            [@"C:\one", @"D:\two"], p => !p.Contains("x64", StringComparison.Ordinal),
            _ => SpatialAnalyzerApi.TargetVersion);
        Assert.Equal("spatial-analyzer-installation-ambiguous", selected.DiagnosticCode);
        var explicitChoice = SpatialAnalyzerInstallationDiscovery.Select(
            @"D:\two\Spatial Analyzer64.exe", @"C:\one\Spatial Analyzer64.exe",
            [@"C:\one", @"D:\two"], _ => true, _ => SpatialAnalyzerApi.TargetVersion);
        Assert.Equal(@"D:\two\Spatial Analyzer64.exe", explicitChoice.ExecutablePath);
    }

    [Fact]
    public void InvalidExplicitSelectionNeverFallsBack()
    {
        var selected = SpatialAnalyzerInstallationDiscovery.Select(
            @"D:\wrong\Spatial Analyzer64.exe", @"C:\right\Spatial Analyzer64.exe",
            [@"C:\right"], _ => true,
            p => p.StartsWith("D:", StringComparison.Ordinal) ? "2000.1.1.1" : SpatialAnalyzerApi.TargetVersion);
        Assert.Null(selected.ExecutablePath);
        Assert.Equal("spatial-analyzer-file-version-mismatch", selected.DiagnosticCode);
    }

    [Fact]
    public void VendorCommaVersionsAndIconLocationsAreEvidenceOnly()
    {
        Assert.True(SpatialAnalyzerInstallationDiscovery.MatchesTarget(
            SpatialAnalyzerApi.TargetVersion.Replace(".", ", ", StringComparison.Ordinal)));
        Assert.False(SpatialAnalyzerInstallationDiscovery.MatchesTarget(null));
        Assert.Equal(@"C:\SA folder\Spatial Analyzer64.exe",
            SpatialAnalyzerInstallationDiscovery.IconPath("\"C:\\SA folder\\Spatial Analyzer64.exe\",0"));
        Assert.Null(SpatialAnalyzerInstallationDiscovery.IconPath("relative.exe"));
    }

    [Fact]
    public void ContractSnapshotsCannotMutateTheDeclaration()
    {
        var first = ServerCompatibility.Create();
        first.Major = 99;
        Assert.Equal(2U, ServerCompatibility.Create().Major);
        Assert.Equal(0U, ServerCompatibility.Create().Revision);
    }
}
