namespace Briosa.Server.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void ServerReferencesProtocolButNotSpatialAnalyzerInterop()
    {
        var references = typeof(Program).Assembly.GetReferencedAssemblies();

        Assert.Contains(references, reference => reference.Name == "Briosa.Protocol");
        Assert.DoesNotContain(references, reference =>
            reference.Name == "Briosa.SpatialAnalyzer.Interop");
    }
}
