using Briosa.Protocol;

namespace Briosa.Protocol.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void ProtocolDoesNotReferenceSpatialAnalyzerInterop()
    {
        var references = ProtocolAssembly.MarkerType.Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, reference =>
            reference.Name == "Briosa.SpatialAnalyzer.Interop");
    }
}
