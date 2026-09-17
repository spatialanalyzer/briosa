using Briosa.Worker;

namespace Briosa.Worker.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void WorkerIsX64AndReferencesExpectedInteropAssembly()
    {
        Assert.True(Environment.Is64BitProcess);
        Assert.Equal("Briosa.SpatialAnalyzer.Interop", InteropMetadata.AssemblyName.Name);
        Assert.Equal(new Version(2024, 1, 508, 5), InteropMetadata.AssemblyName.Version);
    }
}
