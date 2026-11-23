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

    // At least two instruments with common measured targets (USMN, #1).
    public IReadOnlyList<ManifestInstrument> UsmnInstruments { get; init; } = [];

    // The nominal point group for the USMN value variant (#1).
    public ManifestObject? UsmnNominalsGroup { get; init; }

    // An existing chart used as the template value (#3).
    public string TemplateChartName { get; init; } = string.Empty;

    public string UiProfileName { get; init; } = "Default";

    // An exported user-interface profile file (#4). The path is never recorded.
    public string UiProfileFile { get; init; } = string.Empty;

    // Licensed session 4 (#402). Each is optional: a missing one records its steps as
    // fixture-missing (never sent, never a pass). Older manifests stay valid.

    // Probe 34: a representative large point group already in the job.
    public ManifestObject? ResultSizePointGroup { get; init; }

    // Probe 34: a representative cloud already in the job (counted only).
    public ManifestObject? ResultSizeCloud { get; init; }

    // Probe 35: an existing callout view, the destination of the five callout text setters.
    public ManifestObject? CalloutView { get; init; }

    // Probe 36: a TCP fixture and its measurements, for the TCP uncertainty notes getter.
    public ManifestTcpFixture? TcpFixture { get; init; }

    // Probe 37: a robot machine and one of its parameter names, for the Machine ID getter.
    public ManifestRobotMachine? RobotMachine { get; init; }

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
        ResultSizePointGroup = new ManifestObject { Collection = "MANUAL", Name = "MANUAL-LARGE-GROUP" },
        ResultSizeCloud = new ManifestObject { Collection = "MANUAL", Name = "MANUAL-LARGE-CLOUD" },
        CalloutView = new ManifestObject { Collection = "MANUAL", Name = "MANUAL-CALLOUT-VIEW" },
        TcpFixture = new ManifestTcpFixture
        {
            Fixture = new ManifestObject { Collection = "MANUAL", Name = "MANUAL-TCP-FIXTURE" },
            Measurements =
            [
                new ManifestPoint { Collection = "MANUAL", Group = "MANUAL-TCP", Target = "M1" },
                new ManifestPoint { Collection = "MANUAL", Group = "MANUAL-TCP", Target = "M2" },
                new ManifestPoint { Collection = "MANUAL", Group = "MANUAL-TCP", Target = "M3" }
            ]
        },
        RobotMachine = new ManifestRobotMachine { Collection = "MANUAL", InstrumentId = 0, ParameterName = "MANUAL-PARAMETER" },
        IsPlaceholder = true
    };

    /// <summary>
    /// The session-4 manifest keys this manifest supplies. The placeholder supplies all of
    /// them, so a dry run without a manifest shows every planned step.
    /// </summary>
    public bool Supplies(string fixtureKey) => fixtureKey switch
    {
        FixtureKeys.ResultSizePointGroup => ResultSizePointGroup is not null,
        FixtureKeys.ResultSizeCloud => ResultSizeCloud is not null,
        FixtureKeys.CalloutView => CalloutView is not null,
        FixtureKeys.TcpFixture => TcpFixture is not null,
        FixtureKeys.RobotMachine => RobotMachine is not null,
        _ => throw new ArgumentOutOfRangeException(nameof(fixtureKey), "Unknown fixture key.")
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
        Require(UsmnInstruments is { Count: >= 2 }, "At least two USMN instruments are required.");
        Require(UsmnInstruments.All(static instrument => instrument is { IsValid: true }), "Every instrument needs a collection and a non-negative id.");
        Require(UsmnNominalsGroup is { IsValid: true }, "usmn_nominals_group is required.");
        Require(!string.IsNullOrWhiteSpace(TemplateChartName), "template_chart_name is required.");
        Require(!string.IsNullOrWhiteSpace(UiProfileName), "ui_profile_name must not be blank.");
        Require(!string.IsNullOrWhiteSpace(UiProfileFile) && Path.IsPathFullyQualified(UiProfileFile),
            "ui_profile_file must be a fully qualified path.");

        Require(ResultSizePointGroup is null || ResultSizePointGroup.IsValid, "result_size_point_group needs a collection and a name.");
        Require(ResultSizeCloud is null || ResultSizeCloud.IsValid, "result_size_cloud needs a collection and a name.");
        Require(CalloutView is null || CalloutView.IsValid, "callout_view needs a collection and a name.");
        Require(TcpFixture is null || TcpFixture.IsValid,
            "tcp_fixture needs a fixture, at least one measurement, and tcp_in_working of 16 finite numbers when given.");
        Require(RobotMachine is null || RobotMachine.IsValid,
            "robot_machine needs a collection, a non-negative instrument_id, and a parameter_name.");

        // Manual fixtures never live in the harness-owned collection, so the
        // harness can prove it created everything it may destroy.
        var collections = UsmnInstruments.Select(static instrument => instrument.Collection)
            .Append(UsmnNominalsGroup!.Collection)
            .Concat(new[] { ResultSizePointGroup, ResultSizeCloud, CalloutView, TcpFixture?.Fixture }
                .OfType<ManifestObject>().Select(static item => item.Collection))
            .Concat(TcpFixture?.Measurements.Select(static point => point.Collection) ?? [])
            .Concat(RobotMachine is null ? [] : [RobotMachine.Collection]);
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

/// <summary>The session-4 fixture-manifest keys (#402), as they appear in the manifest.</summary>
internal static class FixtureKeys
{
    public const string ResultSizePointGroup = "result_size_point_group";
    public const string ResultSizeCloud = "result_size_cloud";
    public const string CalloutView = "callout_view";
    public const string TcpFixture = "tcp_fixture";
    public const string RobotMachine = "robot_machine";
}

internal sealed record ManifestPoint
{
    public string Collection { get; init; } = string.Empty;

    public string Group { get; init; } = string.Empty;

    public string Target { get; init; } = string.Empty;

    [JsonIgnore]
    public bool IsValid => !string.IsNullOrWhiteSpace(Collection) && !string.IsNullOrWhiteSpace(Group) && !string.IsNullOrWhiteSpace(Target);
}

internal sealed record ManifestTcpFixture
{
    public ManifestObject? Fixture { get; init; }

    public IReadOnlyList<ManifestPoint> Measurements { get; init; } = [];

    // Sixteen numbers, row by row; the identity transform when absent.
    public IReadOnlyList<double>? TcpInWorking { get; init; }

    // A null list or a null entry (JSON null) is invalid, never dereferenced.
    [JsonIgnore]
    public bool IsValid => Fixture is { IsValid: true } && Measurements is { Count: > 0 } &&
        Measurements.All(static point => point is { IsValid: true }) &&
        (TcpInWorking is null || (TcpInWorking.Count == 16 && TcpInWorking.All(double.IsFinite)));
}

internal sealed record ManifestRobotMachine
{
    public string Collection { get; init; } = string.Empty;

    public int InstrumentId { get; init; } = -1;

    public string ParameterName { get; init; } = string.Empty;

    [JsonIgnore]
    public bool IsValid => !string.IsNullOrWhiteSpace(Collection) && InstrumentId >= 0 && !string.IsNullOrWhiteSpace(ParameterName);
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
    public const string GroupFit = "GFIT";
    public const string GroupDelete = "GDEL";
    public const string PlaneA = "PA";
    public const string PlaneB = "PB";
    public const string Relationship = "R1";
    public const string VectorGroup = "VG1";

    // Probe 24 (#307 R0): one disposable cloud per destructive step, and a sentinel
    // that no step targets. A destructive target's key is "cloud:" plus its name.
    public const string DeleteCloudR0 = "CDELR";
    public const string DeleteCloudR0a = "CDELRA";
    public const string DeleteSentinelCloud = "CDELS";

    public static string CloudKey(string cloud) => "cloud:" + cloud;

    // Probe 34 (#402): the ladder's B-spline and one point group per ladder rung.
    public const string ResultSizeBSpline = "BSZ";

    public static string ResultSizeGroup(int points) => "SZ" + points.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
