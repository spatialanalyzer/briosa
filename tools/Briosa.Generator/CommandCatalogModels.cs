using System.Text.Json;
using System.Text.Json.Serialization;

namespace Briosa.Generator;

internal sealed class CommandCatalogManifest
{
    [JsonPropertyName("$schema")]
    [JsonRequired]
    public required string Schema { get; init; }

    [JsonRequired]
    public required int SchemaVersion { get; init; }

    [JsonRequired]
    public required string CatalogId { get; init; }

    [JsonRequired]
    public required string CatalogRevision { get; init; }

    [JsonRequired]
    public required string SpatialAnalyzerTarget { get; init; }

    [JsonRequired]
    public required string TargetProtocolPackage { get; init; }

    [JsonRequired]
    public required List<CommandCatalogSource> Sources { get; init; }

    [JsonRequired]
    public required List<string> OperationFiles { get; init; }
}

internal sealed class CommandCatalogSource
{
    [JsonRequired]
    public required string SourceId { get; init; }

    [JsonRequired]
    public required string Kind { get; init; }

    [JsonRequired]
    public required string Description { get; init; }

    [JsonRequired]
    public required bool SourceMaterialCommitted { get; init; }
}

internal sealed class CommandCatalogOperation
{
    [JsonPropertyName("$schema")]
    [JsonRequired]
    public required string Schema { get; init; }

    [JsonRequired]
    public required string OperationId { get; init; }

    [JsonRequired]
    public required string MpStep { get; init; }

    [JsonRequired]
    public required string Category { get; init; }

    [JsonRequired]
    public required CommandCatalogProtocolNames Protocol { get; init; }

    [JsonRequired]
    public required string Stability { get; init; }

    [JsonRequired]
    public required CommandCatalogDeprecation Deprecation { get; init; }

    [JsonRequired]
    public required CommandCatalogRisk Risk { get; init; }

    [JsonRequired]
    public required CommandCatalogDocumentation Documentation { get; init; }

    [JsonRequired]
    public required List<CommandCatalogArgument> Arguments { get; init; }

    [JsonRequired]
    public required List<CommandCatalogEvidence> Evidence { get; init; }
}

internal sealed class CommandCatalogProtocolNames
{
    [JsonRequired]
    public required string Service { get; init; }

    [JsonRequired]
    public required string Rpc { get; init; }

    [JsonRequired]
    public required string Request { get; init; }

    [JsonRequired]
    public required string Result { get; init; }
}

internal sealed class CommandCatalogDeprecation
{
    [JsonRequired]
    public required string Status { get; init; }

    public string? Reason { get; init; }

    public string? ReplacementOperationId { get; init; }
}

internal sealed class CommandCatalogRisk
{
    [JsonRequired]
    public required string Effect { get; init; }

    [JsonRequired]
    public required List<string> Flags { get; init; }
}

internal sealed class CommandCatalogDocumentation
{
    [JsonRequired]
    public required string Summary { get; init; }
}

internal sealed class CommandCatalogArgument
{
    [JsonRequired]
    public required string ArgumentId { get; init; }

    [JsonRequired]
    public required int Ordinal { get; init; }

    [JsonRequired]
    public required string MpName { get; init; }

    [JsonRequired]
    public required string Direction { get; init; }

    [JsonRequired]
    public required string ResultOnly { get; init; }

    [JsonRequired]
    public required string SemanticType { get; init; }

    public CommandCatalogInputMetadata? Input { get; init; }

    [JsonRequired]
    public required CommandCatalogSdkBinding SdkBinding { get; init; }

    [JsonRequired]
    public required string Documentation { get; init; }
}

internal sealed class CommandCatalogInputMetadata
{
    [JsonRequired]
    public required string Presence { get; init; }

    [JsonRequired]
    public required string OmissionBehavior { get; init; }

    [JsonRequired]
    public required CommandCatalogDefault Default { get; init; }
}

internal sealed class CommandCatalogDefault
{
    [JsonRequired]
    public required string Status { get; init; }

    public JsonElement? Value { get; init; }
}

internal sealed class CommandCatalogSdkBinding
{
    [JsonRequired]
    public required string Status { get; init; }

    [JsonRequired]
    public required string? Setter { get; init; }

    [JsonRequired]
    public required string? Getter { get; init; }
}

internal sealed class CommandCatalogEvidence
{
    [JsonRequired]
    public required string SourceId { get; init; }

    [JsonRequired]
    public required string Reference { get; init; }
}
