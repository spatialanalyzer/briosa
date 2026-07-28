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

    public List<string> DataClassifications { get; init; } = ["unknown"];

    [JsonRequired]
    public required List<string> ValueFamilies { get; init; }

    [JsonRequired]
    public required string? DeliveryWave { get; init; }

    public CommandShapeResolution? CommandShape { get; init; }
}

internal sealed class CommandShapeResolution
{
    [JsonRequired]
    public required string Status { get; init; }

    public required string? MpStep { get; init; }

    [JsonRequired]
    public required List<CommandArgumentResolution> Arguments { get; init; }

    [JsonRequired]
    public required List<CommandShapeDiscrepancy> Discrepancies { get; init; }
}

internal sealed class CommandArgumentResolution
{
    [JsonRequired]
    public required int InventoryIndex { get; init; }

    [JsonRequired]
    public required int Ordinal { get; init; }

    [JsonRequired]
    public required string MpName { get; init; }

    [JsonRequired]
    public required string Direction { get; init; }

    [JsonRequired]
    public required string ResultOnly { get; init; }

    public CommandInputResolution? Input { get; init; }

    [JsonRequired]
    public required CommandSdkBindingResolution SdkBinding { get; init; }
}

internal sealed class CommandInputResolution
{
    [JsonRequired]
    public required string Presence { get; init; }

    [JsonRequired]
    public required string OmissionBehavior { get; init; }

    [JsonRequired]
    public required CommandDefaultResolution Default { get; init; }
}

internal sealed class CommandDefaultResolution
{
    [JsonRequired]
    public required string Status { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReviewStatus { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DecisionReference { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EvidenceState { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ReasonCodes { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Value { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Evidence { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CommandDefaultCandidate>? Candidates { get; init; }
}

internal sealed class CommandDefaultCandidate
{
    [JsonRequired]
    public required string Source { get; init; }

    [JsonRequired]
    public required object? Value { get; init; }
}

internal sealed class CommandSdkBindingResolution
{
    public required string? Setter { get; init; }

    public required string? Getter { get; init; }
}

internal sealed class CommandShapeDiscrepancy
{
    [JsonRequired]
    public required string Code { get; init; }

    [JsonRequired]
    public required List<int> ArgumentIndexes { get; init; }

    [JsonRequired]
    public required string Owner { get; init; }

    [JsonRequired]
    public required string BlockerReference { get; init; }

    [JsonRequired]
    public required string Rationale { get; init; }
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
