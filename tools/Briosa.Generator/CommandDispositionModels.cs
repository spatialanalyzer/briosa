using System.Text.Json.Serialization;

namespace Briosa.Generator;

internal sealed class CommandDispositionManifest
{
    [JsonPropertyName("$schema")]
    [JsonRequired]
    public required string Schema { get; init; }

    [JsonRequired]
    public required int SchemaVersion { get; init; }

    [JsonRequired]
    public required string SpatialAnalyzerTarget { get; init; }

    [JsonRequired]
    public required CommandDispositionInventoryReference Inventory { get; init; }

    [JsonRequired]
    public required List<CommandDispositionShardReference> Shards { get; init; }
}

internal sealed class CommandDispositionInventoryReference
{
    [JsonRequired]
    public required string Path { get; init; }

    [JsonRequired]
    public required string Sha256 { get; init; }

    [JsonRequired]
    public required int CommandCount { get; init; }
}

internal sealed class CommandDispositionShardReference
{
    [JsonRequired]
    public required string Category { get; init; }

    [JsonRequired]
    public required string Path { get; init; }

    [JsonRequired]
    public required int EntryCount { get; init; }

    [JsonRequired]
    public required string Sha256 { get; init; }
}

internal sealed class CommandDispositionShard
{
    [JsonPropertyName("$schema")]
    [JsonRequired]
    public required string Schema { get; init; }

    [JsonRequired]
    public required int SchemaVersion { get; init; }

    [JsonRequired]
    public required string SpatialAnalyzerTarget { get; init; }

    [JsonRequired]
    public required string Category { get; init; }

    [JsonRequired]
    public required List<CommandDispositionEntry> Entries { get; init; }
}

internal sealed class CommandDispositionEntry
{
    [JsonRequired]
    public required string InventoryKey { get; init; }

    [JsonRequired]
    public required string MpStep { get; init; }

    [JsonRequired]
    public required List<string> CategoryPath { get; init; }

    [JsonRequired]
    public required string InventoryEntrySha256 { get; init; }

    [JsonRequired]
    public required string Disposition { get; init; }

    [JsonRequired]
    public required string ReviewState { get; init; }

    [JsonRequired]
    public required string Rationale { get; init; }

    [JsonRequired]
    public required List<string> ReasonCodes { get; init; }

    [JsonRequired]
    public required List<string> EvidenceReferences { get; init; }

    [JsonRequired]
    public required List<string> DecisionReferences { get; init; }

    [JsonRequired]
    public required List<string> BlockerReferences { get; init; }

    [JsonRequired]
    public required string RiskEffect { get; init; }

    [JsonRequired]
    public required List<string> RiskFlags { get; init; }

    [JsonRequired]
    public required List<string> ValueFamilies { get; init; }

    [JsonRequired]
    public required string? DeliveryWave { get; init; }
}

internal sealed record CommandDispositionValidationResult(
    IReadOnlyList<string> Errors,
    int TargetCount,
    int EntryCount)
{
    public bool IsValid => Errors.Count == 0;
}

internal sealed record CommandDispositionSyncResult(
    IReadOnlyList<string> Files,
    int EntryCount,
    int NewEntryCount,
    int ReReviewCount);
