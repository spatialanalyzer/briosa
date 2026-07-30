using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Briosa.SpatialAnalyzer.IntegrationTests;

internal static class FixtureDescriptorReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    internal static FixtureDescriptor Read(string path)
    {
        var resolved = Path.GetFullPath(path);
        if (!File.Exists(resolved))
        {
            throw new FileNotFoundException("fixture_descriptor_not_found");
        }

        return Parse(File.ReadAllText(resolved));
    }

    internal static FixtureDescriptor Parse(string json) =>
        JsonSerializer.Deserialize<FixtureDescriptor>(json, JsonOptions)
        ?? throw new InvalidDataException("fixture_descriptor_invalid");
}

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "System.Text.Json constructs this integration-only fixture model.")]
internal sealed record FixtureDescriptor(
    string? JobPath,
    string? InputPath,
    FixtureObject? Object,
    FixtureObject[]? Items);

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "System.Text.Json constructs this integration-only fixture model.")]
internal sealed record FixtureObject(
    string? CollectionName,
    string? Name,
    string? Type);
