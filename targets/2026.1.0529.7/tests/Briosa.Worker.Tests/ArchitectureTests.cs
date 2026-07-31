using Briosa.Worker;

namespace Briosa.Worker.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void WorkerIsX64AndReferencesExpectedInteropAssembly()
    {
        Assert.True(Environment.Is64BitProcess);
        Assert.Equal("Briosa.SpatialAnalyzer.Interop", InteropMetadata.AssemblyName.Name);
        Assert.Equal(new Version(2026, 1, 529, 7), InteropMetadata.AssemblyName.Version);
    }
}
