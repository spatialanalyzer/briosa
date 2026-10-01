using System.Text.Json;

namespace Briosa.Server.Tests;

internal sealed record CommittedInteropProvenance(
    string InteropDirectory,
    string CanonicalApiFileName,
    string CanonicalApiSha256)
{
    private const string ProvenanceFileName = "Briosa.SpatialAnalyzer.Interop.provenance.json";

    public string ExpectedInteropFingerprint => "sha256:" + CanonicalApiSha256;

    public static CommittedInteropProvenance Load()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Briosa.slnx")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        var provenancePath = Assert.Single(Directory.GetFiles(
            Path.Combine(root.FullName, "interop", "SpatialAnalyzer"),
            ProvenanceFileName,
            SearchOption.AllDirectories));
        using var provenance = JsonDocument.Parse(File.ReadAllText(provenancePath));
        var artifact = provenance.RootElement.GetProperty("artifact");
        var canonicalApiFileName = artifact.GetProperty("canonicalApiFileName").GetString();
        var canonicalApiSha256 = artifact.GetProperty("canonicalApiSha256").GetString();
        Assert.NotNull(canonicalApiFileName);
        Assert.NotNull(canonicalApiSha256);
        Assert.Matches("^[0-9A-F]{64}$", canonicalApiSha256);
        return new CommittedInteropProvenance(
            Path.GetDirectoryName(provenancePath)!,
            canonicalApiFileName,
            canonicalApiSha256);
    }
}
