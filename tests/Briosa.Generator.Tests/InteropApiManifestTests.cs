using Briosa.Generator;

namespace Briosa.Generator.Tests;

public sealed class InteropApiManifestTests
{
    [Fact]
    public void ManifestIsStableForIdenticalInput()
    {
        var assemblyPath = typeof(InteropApiManifest).Assembly.Location;

        var first = InteropApiManifest.Create(assemblyPath);
        var second = InteropApiManifest.Create(assemblyPath);

        Assert.Equal(first, second);
        Assert.Contains(
            "# Volatile PE headers and the module MVID are intentionally excluded.",
            first,
            StringComparison.Ordinal);
    }
}
