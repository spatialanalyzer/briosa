using System.Text.Json;
using System.Text.Json.Serialization;

namespace Briosa.LicensedProbes;

/// <summary>
/// Names of the manually prepared fixtures. The manifest is read only by the
/// harness; none of its values are written to observations or console output.
/// </summary>
internal sealed record FixtureManifest
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public int SchemaVersion { get; init; }

    public string SpatialAnalyzerTarget { get; init; } = string.Empty;

    // At least two instruments with common measured targets (USMN, #1 and #14).
    public IReadOnlyList<ManifestInstrument> UsmnInstruments { get; init; } = [];

    // The nominal point group for the USMN value variant (#1).
    public ManifestObject? UsmnNominalsGroup { get; init; }

    // An existing chart used as the template value (#3).
    public string TemplateChartName { get; init; } = string.Empty;

    public string UiProfileName { get; init; } = "Default";

    // An exported user-interface profile file (#4). The path is never recorded.
    public string UiProfileFile { get; init; } = string.Empty;

    // Optional manual replacement for the programmatic cylinder surface (#9).
    public ManifestObject? CircleLineSurface { get; init; }

    // Optional manual replacement for the programmatic callout view (#7-8).
    public ManifestObject? CalloutView { get; init; }

    [JsonIgnore]
    public bool IsPlaceholder { get; init; }

    public static FixtureManifest Placeholder { get; } = new()
    {
        SchemaVersion = 1,
        SpatialAnalyzerTarget = ProbeTarget.SpatialAnalyzerTarget,
        UsmnInstruments =
        [
            new ManifestInstrument { Collection = "MANUAL", InstrumentId = 0 },
            new ManifestInstrument { Collection = "MANUAL", InstrumentId = 1 }
        ],
        UsmnNominalsGroup = new ManifestObject { Collection = "MANUAL", Name = "MANUAL-NOMINALS" },
        TemplateChartName = "MANUAL-TEMPLATE-CHART",
        UiProfileFile = @"C:\MANUAL\profile",
        IsPlaceholder = true
    };

    public static FixtureManifest Parse(string json)
    {
        var manifest = JsonSerializer.Deserialize<FixtureManifest>(json, JsonOptions)
            ?? throw new InvalidDataException("The fixture manifest is empty.");
        manifest.Validate();
        return manifest;
    }

    public void Validate()
    {
        static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidDataException(message);
            }
        }

        Require(SchemaVersion == 1, "The fixture manifest schema_version must be 1.");
        Require(string.Equals(SpatialAnalyzerTarget, ProbeTarget.SpatialAnalyzerTarget, StringComparison.Ordinal),
            "The fixture manifest names a different SpatialAnalyzer target.");
        Require(UsmnInstruments.Count >= 2, "At least two USMN instruments are required.");
        Require(UsmnInstruments.All(static instrument => instrument.IsValid), "Every instrument needs a collection and a non-negative id.");
        Require(UsmnNominalsGroup is { IsValid: true }, "usmn_nominals_group is required.");
        Require(!string.IsNullOrWhiteSpace(TemplateChartName), "template_chart_name is required.");
        Require(!string.IsNullOrWhiteSpace(UiProfileName), "ui_profile_name must not be blank.");
        Require(!string.IsNullOrWhiteSpace(UiProfileFile) && Path.IsPathFullyQualified(UiProfileFile),
            "ui_profile_file must be a fully qualified path.");
        Require(CircleLineSurface is null or { IsValid: true }, "circle_line_surface is incomplete.");
        Require(CalloutView is null or { IsValid: true }, "callout_view is incomplete.");

        // Manual fixtures never live in the harness-owned collection, so the
        // harness can prove it created everything it may destroy.
        var collections = UsmnInstruments.Select(static instrument => instrument.Collection)
            .Append(UsmnNominalsGroup!.Collection)
            .Concat(CircleLineSurface is null ? [] : [CircleLineSurface.Collection])
            .Concat(CalloutView is null ? [] : [CalloutView.Collection]);
        Require(collections.All(static name => !string.Equals(name, FixtureNames.Collection, StringComparison.OrdinalIgnoreCase)),
            "Manual fixtures must not use the harness-owned collection.");
    }
}

internal sealed record ManifestInstrument
{
    public string Collection { get; init; } = string.Empty;

    public int InstrumentId { get; init; } = -1;

    [JsonIgnore]
    public bool IsValid => !string.IsNullOrWhiteSpace(Collection) && InstrumentId >= 0;
}

internal sealed record ManifestObject
{
    public string Collection { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    [JsonIgnore]
    public bool IsValid => !string.IsNullOrWhiteSpace(Collection) && !string.IsNullOrWhiteSpace(Name);
}

/// <summary>Names of programmatic fixtures. All live in one harness-owned collection.</summary>
internal static class FixtureNames
{
    public const string Collection = "B277";

    public const string GroupOne = "G1";
    public const string GroupRotated = "G2";
    public const string GroupMeasured = "G3";
    public const string GroupDelete = "GDEL";
    public const string GroupGrid = "GRID";
    public const string PlaneA = "PA";
    public const string PlaneB = "PB";
    public const string Frame = "F1";
    public const string Cylinder = "CY1";
    public const string CylinderSurface = "SCYL";
    public const string PolygonizeCloud = "CPOLY";
    public const string DeleteCloudAllBounds = "CDEL15";
    public const string DeleteCloudPartialBounds = "CDEL16";
    public const string Relationship = "R1";
    public const string VectorGroup = "VG1";
    public const string CalloutView = "V1";

    // Fixture keys tracked by the session for destructive-step authorization.
    public const string DeleteCloudAllBoundsKey = "cloud:" + DeleteCloudAllBounds;
    public const string DeleteCloudPartialBoundsKey = "cloud:" + DeleteCloudPartialBounds;
}
