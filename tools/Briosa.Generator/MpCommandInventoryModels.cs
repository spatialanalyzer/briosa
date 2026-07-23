using System.Text.Json.Serialization;

namespace Briosa.Generator;

internal sealed class MpCommandInventory
{
    [JsonPropertyName("$schema")]
    [JsonRequired]
    public required string Schema { get; init; }

    [JsonRequired]
    public required int SchemaVersion { get; init; }

    [JsonRequired]
    public required string SpatialAnalyzerTarget { get; init; }

    [JsonRequired]
    public required MpCommandInventoryProvenance Provenance { get; init; }

    [JsonRequired]
    public required MpCommandInventorySummary Summary { get; init; }

    [JsonRequired]
    public required List<MpCommandInventoryCommand> Commands { get; init; }
}

internal sealed class MpCommandInventoryProvenance
{
    [JsonRequired]
    public required MpCommandInventorySource Documentation { get; init; }

    [JsonRequired]
    public required MpCommandInventorySource SdkCode { get; init; }
}

internal sealed class MpCommandInventorySource
{
    [JsonRequired]
    public required string Kind { get; init; }

    [JsonRequired]
    public required int FileCount { get; init; }

    [JsonRequired]
    public required int RecordCount { get; init; }

    [JsonRequired]
    public required string AggregateSha256 { get; init; }

    [JsonRequired]
    public required bool SourceMaterialCommitted { get; init; }
}

internal sealed class MpCommandInventorySummary
{
    [JsonRequired]
    public required int CommandCount { get; init; }

    [JsonRequired]
    public required int MatchedCommandCount { get; init; }

    [JsonRequired]
    public required int DocumentationOnlyCommandCount { get; init; }

    [JsonRequired]
    public required int SdkOnlyCommandCount { get; init; }

    [JsonRequired]
    public required int AmbiguousCommandCount { get; init; }

    [JsonRequired]
    public required List<MpCommandInventoryFindingCount> FindingCounts { get; init; }
}

internal sealed class MpCommandInventoryFindingCount
{
    [JsonRequired]
    public required string Finding { get; init; }

    [JsonRequired]
    public required int Count { get; init; }
}

internal sealed class MpCommandInventoryCommand
{
    [JsonRequired]
    public required string InventoryKey { get; init; }

    [JsonRequired]
    public required string MpStep { get; init; }

    [JsonRequired]
    public required List<string> CategoryPath { get; init; }

    public MpCommandInventoryDocumentEvidence? Documentation { get; init; }

    [JsonRequired]
    public required List<MpCommandInventorySdkEvidence> SdkEvidence { get; init; }

    [JsonRequired]
    public required string OverallOutcome { get; init; }

    [JsonRequired]
    public required List<MpCommandInventoryArgument> Arguments { get; init; }

    [JsonRequired]
    public required List<string> Findings { get; init; }
}

internal sealed class MpCommandInventoryDocumentEvidence
{
    [JsonRequired]
    public required string Reference { get; init; }

    [JsonRequired]
    public required string Sha256 { get; init; }

    [JsonRequired]
    public required bool HasInputArgumentsSection { get; init; }

    [JsonRequired]
    public required bool HasReturnArgumentsSection { get; init; }

    [JsonRequired]
    public required bool HasReturnedStatusSection { get; init; }
}

internal sealed class MpCommandInventorySdkEvidence
{
    [JsonRequired]
    public required string Reference { get; init; }

    [JsonRequired]
    public required string Sha256 { get; init; }

    [JsonRequired]
    public required int Occurrence { get; init; }

    [JsonRequired]
    public required string MpStep { get; init; }
}

internal sealed class MpCommandInventoryArgument
{
    public int? Ordinal { get; init; }

    public int? SdkOrder { get; init; }

    [JsonRequired]
    public required string MpName { get; init; }

    [JsonRequired]
    public required string DocumentedType { get; init; }

    [JsonRequired]
    public required string Direction { get; init; }

    [JsonRequired]
    public required string ResultOnly { get; init; }

    [JsonRequired]
    public required string Presence { get; init; }

    [JsonRequired]
    public required MpCommandInventorySdkBinding SdkBinding { get; init; }

    [JsonRequired]
    public required List<string> Findings { get; init; }
}

internal sealed class MpCommandInventorySdkBinding
{
    [JsonRequired]
    public required MpCommandInventoryBindingEvidence Setter { get; init; }

    [JsonRequired]
    public required MpCommandInventoryBindingEvidence Getter { get; init; }
}

internal sealed class MpCommandInventoryBindingEvidence
{
    [JsonRequired]
    public required string Status { get; init; }

    public string? Method { get; init; }

    public string? ArgumentName { get; init; }
}

internal sealed record MpCommandInventoryExtraction(
    MpCommandInventory Inventory,
    string InventoryJson,
    string ReportMarkdown);
