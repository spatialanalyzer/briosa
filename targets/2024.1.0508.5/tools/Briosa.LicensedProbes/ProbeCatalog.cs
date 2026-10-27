using System.Globalization;
using Google.Protobuf;
using Api = global::Briosa;

namespace Briosa.LicensedProbes;

/// <summary>
/// The maintainer-approved probe list from #277 and its follow-ups, expressed as
/// immutable steps. The public phase runs every probe the public API can express;
/// the worker phase runs only what the public API intentionally cannot express (a
/// skipped setter, a blank required value, a recased label, alternate step text).
/// </summary>
internal static class ProbeCatalog
{
    // Step text pairs for #19-21: the SDK View Code text (authoritative per #246)
    // and the SA 2026 installed-documentation text. Both are sent on both targets.
    public const string OrientationSdkStep = "Compute Group to Group Orientation (Rx,Ry,Rz)";
    public const string OrientationDocumentationStep = "Compute Group to Group Orientation (Rx, Ry, Rz)";
    public const string AngleSdkStep = "Angle Between Two Planes' normals";
    public const string AngleDocumentationStep = "Angle Between Two Planes’ normals";
    public const string ShowHideSdkStep = "Show / Hide Points";
    public const string ShowHideDocumentationStep = "Show/Hide Points";

    public const string NominalsLabel = "Nominals Group Name (blank for none)";
    public const string SeedGeometryLabel = "Starting Condition Geometry (optional)";
    public const string TemplateChartLabel = "Template Chart Name (optional)";
    public const string ProfileFileLabel = "Profile File Name (optional)";
    public const string HighPlaneLabel = "Resulting 'High' Plane Name";
    public const string LowPlaneLabel = "Resulting 'Low' Plane Name";
    public const string GroupToFitLabel = "Group To Fit";
    public const string GroupToFitRecased = "Group to Fit";
    public const string NumberSuffixLabel = "Use Number Suffix?";
    public const string NumberSuffixRecased = "Use number suffix?";

    // Probe 25: the operator replaces the initial answer with the entry; the record
    // keeps only which of the two harness constants came back.
    public const string AskInitialAnswer = "B277-initial";
    public const string AskOperatorEntry = "B277-entered";
    public const string AskPrompt = "Briosa probe 25: replace the answer with B277-entered, then click OK.";
    public const string AskSilentPrompt = "Briosa probe 25 (Silent): replace the answer with B277-entered, then click OK.";

    // Probe 25: a template chart name that matches no chart opens "Template Chart Not found".
    public const string MissingTemplateChart = "B277 no such template";

    // Probes 26 to 28: a public stall step's client deadline exceeds the 2-minute quick
    // budget the runbook sets, so the server's watchdog ends it. In the worker phase the
    // step's own deadline is the only watchdog.
    public static TimeSpan PublicStallClientTimeout { get; } = TimeSpan.FromSeconds(240);

    public static TimeSpan WorkerStallClientTimeout { get; } = TimeSpan.FromSeconds(60);

    public static IReadOnlyList<string> ProbeNumbers { get; } =
        [.. Enumerable.Range(1, 32).Select(static number => number.ToString(CultureInfo.InvariantCulture))];

    // Run only when --probe names them. Probes 26 to 28 each end their run with a planned
    // stall, and probe 29 needs another server setting, so none can join the default plan.
    public static IReadOnlyList<string> OptInProbeNumbers { get; } = ["26", "27", "28", "29"];

    // Retired with the operations they probed: the operations were removed because these
    // probes showed they cannot work on either exact target. Probes 7 to 10 and 14 to 16
    // (#308, #310, #307) found NOT_SUPPORTED substitutions that do not work; probe 11 (#309)
    // found a substitution that stalls, and probe 22 (#350) a getter that returns nothing.
    // Their numbers keep their meaning in the dated records; the plan no longer sends them.
    public static IReadOnlyList<string> RetiredProbeNumbers { get; } = ["7", "8", "9", "10", "11", "14", "15", "16", "22"];

    public static IReadOnlyList<string> ActiveProbeNumbers { get; } =
        [.. ProbeNumbers.Except(RetiredProbeNumbers, StringComparer.Ordinal)];

    public static IReadOnlyList<ProbeStep> Create(ProbePhase phase, FixtureManifest manifest, InteractionModeSettings interactionMode)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(interactionMode);
        var steps = new StepList(phase);
        AddCollectionGuard(steps);
        if (phase == ProbePhase.PublicApi)
        {
            AddPublicFixtures(steps);
            AddPublicProbes(steps, manifest);
            AddPublicInteractionModeProbe(steps, interactionMode);
            AddInteractionModeSettingProbe(steps, interactionMode);
            AddPublicStallProbes(steps, interactionMode);
        }
        else
        {
            AddWorkerFixtures(steps);
            AddWorkerProbes(steps, manifest);
            AddWorkerInteractionModeProbe(steps, interactionMode);
            AddWorkerStallProbe(steps, interactionMode);
        }

        return steps.ToList();
    }

    /// <summary>
    /// The recovery plan for a session that stopped while SA was in Silent (after probe
    /// 25's Silent step, or inside probe 29, whose server set it): one step that restores
    /// SA Interaction Mode. It creates no fixture and touches no job object.
    /// </summary>
    public static IReadOnlyList<ProbeStep> CreateInteractionModeRestore(ProbePhase phase, InteractionModeSettings interactionMode)
    {
        ArgumentNullException.ThrowIfNull(interactionMode);
        var steps = new StepList(phase);
        steps.Add(steps.NewControl("p25-restore", "25", "Restore SA Interaction Mode after a stopped session (recovery only)",
            ProbeOperations.SetInteractionMode, InteractionMode(interactionMode.Restore)) with
        {
            Checkpoints = OperatorCheckpoint.After
        });
        return steps.ToList();
    }

    private static void AddCollectionGuard(StepList steps)
    {
        steps.Guard("g-collections-before", "setup", "Count collections before creating the harness-owned collection",
            ProbeOperations.GetNumberOfCollections, new Api.GetNumberOfCollectionsRequest());
        steps.Setup("s-collection", "Create the harness-owned collection",
            ProbeOperations.ConstructCollection, new Api.ConstructCollectionRequest
            {
                CollectionName = new Api.CollectionName { Name = FixtureNames.Collection },
                FolderPath = string.Empty,
                MakeDefaultCollection = false
            });
        steps.Add(new ProbeStep("g-collections-after", "setup", ProbeStepKind.Guard, steps.Phase,
            "Prove the collection is new: the count must increase by exactly one",
            ProbeOperations.GetNumberOfCollections, new Api.GetNumberOfCollectionsRequest(),
            CommandVariant.Shipped, ProbeOutcomes.Succeeded)
        {
            Requirement = new ProbeRequirement(
                "collection_count increased by exactly one",
                static (history, outcome) => CollectionCountIncreased(history, outcome)),
            Prerequisites = ["g-collections-before"]
        });
    }

    internal static bool CollectionCountIncreased(IReadOnlyDictionary<string, ProbeOutcome> history, ProbeOutcome outcome) =>
        history.TryGetValue("g-collections-before", out var before) &&
        before.Observations.TryGetValue("collection_count", out var beforeText) &&
        outcome.Observations.TryGetValue("collection_count", out var afterText) &&
        int.TryParse(beforeText, NumberStyles.None, CultureInfo.InvariantCulture, out var beforeCount) &&
        int.TryParse(afterText, NumberStyles.None, CultureInfo.InvariantCulture, out var afterCount) &&
        afterCount == beforeCount + 1;

    private static void AddPointGroup(StepList steps, string group, IEnumerable<(string Name, double X, double Y, double Z)> points)
    {
        foreach (var (name, x, y, z) in points)
        {
            steps.Setup($"s-point-{group}-{name}", $"Construct fixture point {group}::{name}",
                ProbeOperations.ConstructPoint, new Api.ConstructPointInWorkingCoordinatesRequest
                {
                    PointName = Point(group, name),
                    WorkingCoordinates = Vector(x, y, z)
                });
        }
    }

    // Six points, non-coplanar so bounding planes are well defined.
    internal static IReadOnlyList<(string Name, double X, double Y, double Z)> GroupOnePoints { get; } =
    [
        ("P1", 0, 0, 0), ("P2", 100, 0, 0), ("P3", 0, 100, 0),
        ("P4", 100, 100, 0), ("P5", 50, 50, 20), ("P6", 50, 50, -20)
    ];

    // The same points rotated one degree about Z.
    internal static IReadOnlyList<(string Name, double X, double Y, double Z)> RotatedPoints { get; } =
        [.. GroupOnePoints.Select(static point =>
        {
            var angle = Math.PI / 180;
            return (point.Name,
                (point.X * Math.Cos(angle)) - (point.Y * Math.Sin(angle)),
                (point.X * Math.Sin(angle)) + (point.Y * Math.Cos(angle)),
                point.Z);
        })];

    // #311: the four coplanar corners of G1. Both targets document MP code 4 for this
    // fit as PARTIAL SUCCESS (the fit tolerance was exceeded), which G1's +/-20 points
    // caused; a plane through coplanar points is within any tolerance.
    internal static IReadOnlyList<(string Name, double X, double Y, double Z)> FitPoints { get; } = [.. GroupOnePoints.Take(4)];

    // Measured points: one within a +/-1 magnitude tolerance and two outside,
    // so the in-tolerance percentage is one third (a fractional value).
    internal static IReadOnlyList<(string Name, double X, double Y, double Z)> MeasuredPoints { get; } =
        [("P1", 0, 0, 0.1), ("P2", 100, 0, 5), ("P3", 0, 100, 5)];

    // Probe 24 (#307 R0): the probe 277 delete group. Only D1 lies within 1 unit of D1;
    // the nearest other point is 5 units away.
    internal static IReadOnlyList<(string Name, double X, double Y, double Z)> DeletePoints { get; } =
    [
        ("D1", 0, 0, 0), ("D2", 5, 5, 5), ("D3", 5, 0, 0), ("D4", 5, 20, 20),
        ("D5", 20, 20, 20), ("D7", 5, 21, 21), ("D8", 5, 8, 8), ("D9", 5, 9, 9)
    ];

    private static void AddCommonGeometry(StepList steps)
    {
        AddPointGroup(steps, FixtureNames.GroupOne, GroupOnePoints);
        AddPointGroup(steps, FixtureNames.GroupRotated, RotatedPoints);
        AddPointGroup(steps, FixtureNames.GroupFit, FitPoints);
        steps.Setup("s-plane-PA", "Construct reference plane PA (normal +Z)", ProbeOperations.ConstructPlane,
            new Api.ConstructPlaneRequest
            {
                PlaneName = Object(FixtureNames.PlaneA, Api.ObjectType.Plane),
                PlaneCenter = Vector(50, 50, 0),
                PlaneNormal = Vector(0, 0, 1),
                PlaneEdgeDimension = 150
            });
        steps.Setup("s-plane-PB", "Construct plane PB at 45 degrees to PA", ProbeOperations.ConstructPlane,
            new Api.ConstructPlaneRequest
            {
                PlaneName = Object(FixtureNames.PlaneB, Api.ObjectType.Plane),
                PlaneCenter = Vector(50, 50, 0),
                PlaneNormal = Vector(0, Math.Sqrt(0.5), Math.Sqrt(0.5)),
                PlaneEdgeDimension = 150
            });
        AddPointGroup(steps, FixtureNames.GroupMeasured, MeasuredPoints);
        var tolerance = new Api.ToleranceVectorOptions
        {
            HighMagnitude = new Api.ToleranceLimit { Enabled = true, Value = 1 },
            LowMagnitude = new Api.ToleranceLimit { Enabled = true, Value = -1 }
        };
        var nominal = new Api.MakePointsToPointsRelationshipRequest
        {
            RelationshipName = new Api.CollectionItemName
            {
                CollectionName = FixtureNames.Collection,
                ItemName = FixtureNames.Relationship,
                ItemType = Api.ItemType.Relationship
            },
            AutoUpdateAVectorGroup = false,
            Tolerance = tolerance,
            Constraint = new Api.ToleranceVectorOptions()
        };
        nominal.NominalPoints.AddRange(MeasuredPoints.Select(static point => Point(FixtureNames.GroupOne, point.Name)));
        nominal.MeasuredPoints.AddRange(MeasuredPoints.Select(static point => Point(FixtureNames.GroupMeasured, point.Name)));
        steps.Setup("s-relationship-R1", "Relate G1 nominals to G3 measured points with a +/-1 magnitude tolerance",
            ProbeOperations.MakePointsToPointsRelationship, nominal);
        steps.Setup("s-vector-group-VG1", "Construct vector group VG1 from relationship R1",
            ProbeOperations.ConstructVectorGroupFromRelationship, new Api.ConstructVectorGroupFromRelationshipRequest
            {
                RelationshipName = Object(FixtureNames.Relationship, Api.ObjectType.Any),
                VectorGroupName = new Api.CollectionVectorGroupName
                {
                    CollectionName = FixtureNames.Collection,
                    VectorGroupName = FixtureNames.VectorGroup
                }
            });
    }

    private static void AddPublicFixtures(StepList steps)
    {
        AddCommonGeometry(steps);
        AddPointGroup(steps, FixtureNames.GroupDelete, DeletePoints);
        steps.Add(CloudSetup("s-cloud-CDELR", FixtureNames.GroupDelete, FixtureNames.DeleteCloudR0,
            FixtureNames.CloudKey(FixtureNames.DeleteCloudR0), steps.Phase));
        steps.Add(CloudSetup("s-cloud-CDELRA", FixtureNames.GroupDelete, FixtureNames.DeleteCloudR0a,
            FixtureNames.CloudKey(FixtureNames.DeleteCloudR0a), steps.Phase));

        // The sentinel is never a destructive target, so it has no fixture key.
        steps.Add(CloudSetup("s-cloud-CDELS", FixtureNames.GroupDelete, FixtureNames.DeleteSentinelCloud, null, steps.Phase));
    }

    private static void AddWorkerFixtures(StepList steps) => AddCommonGeometry(steps);

    private static void AddPublicProbes(StepList steps, FixtureManifest manifest)
    {
        // Programmatic fixtures first; probes that need manual fixtures run after them.
        steps.Probe("p02-value", "2", "Fit the coplanar group GFIT with a seed geometry value (public API)",
            ProbeOperations.FitGeometryToPointGroup, Fit("FIT2V", Object(FixtureNames.PlaneA, Api.ObjectType.Plane)));
        steps.Check("c02-value", "2", "Count planes named FIT2V", ObjectsNamed(FixtureNames.Collection, "FIT2V", Api.ObjectType.Plane));

        steps.Probe("p05-06-value", "5-6", "Bounding planes with both names set (public API)",
            ProbeOperations.ConstructPlanesBoundingPointGroup, Bounding("H56V", "L56V"));
        steps.Check("c05-06-value", "5-6", "Count planes named *56V",
            ObjectsNamed(FixtureNames.Collection, "*56V", Api.ObjectType.Plane),
            new ProbeHypothesis("both named planes created", "match_count", "2"));

        steps.Probe("p12", "12", "Point name getter after Ensure Unique", ProbeOperations.MakePointNameEnsureUnique,
            new Api.MakePointNameEnsureUniqueRequest { PointName = Point(FixtureNames.GroupOne, "P1"), UseNumberSuffix = true });
        steps.Probe("p13", "13", "Collection object name getter after Ensure Unique", ProbeOperations.MakeCollectionObjectNameEnsureUnique,
            new Api.MakeCollectionObjectNameEnsureUniqueRequest
            {
                CollectionObjectName = Object(FixtureNames.GroupOne, Api.ObjectType.PointGroup),
                UseNumberSuffix = true
            });

        steps.Probe("p23", "23", "Vector group properties with a one-third in-tolerance fraction",
            ProbeOperations.GetVectorGroupProperties, new Api.GetVectorGroupPropertiesRequest
            {
                VectorGroupName = Object(FixtureNames.VectorGroup, Api.ObjectType.VectorGroup)
            });

        AddRadialDeleteProbe(steps);
        AddFitSeedOmittedProbe(steps);
        AddPartialSuccessProbe(steps);

        // Manual fixtures.
        steps.Probe("p01-value", "1", "USMN with a nominals group value (public API)",
            ProbeOperations.LocateInstrumentsUsmn, Usmn(manifest));
        steps.Probe("p03-value", "3", "Chart with a template chart value (public API)",
            ProbeOperations.CreateChartFromVectorGroup, Chart(manifest.TemplateChartName, "B277 C3V"));
        steps.Probe("p04-value", "4", "User interface profile with a profile file value (public API)",
            ProbeOperations.SetUserInterfaceProfile, Profile(manifest));
        AddRuntimeSelectProbe(steps, manifest);
    }

    // Probe 31 (#311, #357): the shipped mappings of Fit Geometry to Points and Fit Geometry
    // to Point Group Projected to Plane skip the seed setter when the seed is absent, by
    // analogy with Fit Geometry to Point Group (probe 2). Points also gets a seeded control;
    // the projected circle fit has no circle fixture to seed it.
    private static void AddFitSeedOmittedProbe(StepList steps)
    {
        var created = new ProbeHypothesis("the fit created its object", "match_count", "1");
        steps.Watched(steps.NewProbe("p31-points-value", "31", "Fit a plane to GFIT's four points with a seed geometry value (control)",
            ProbeOperations.FitGeometryToPoints, FitToPoints("FIT31V", Object(FixtureNames.PlaneA, Api.ObjectType.Plane))));
        steps.Watched(steps.NewCheck("c31-points-value", "31", "Count planes named FIT31V",
            ObjectsNamed(FixtureNames.Collection, "FIT31V", Api.ObjectType.Plane)) with { Hypotheses = [created] }, "p31-points-value");
        steps.Watched(steps.NewProbe("p31-points-omitted", "31", "Fit a plane to GFIT's four points with the seed omitted (no setter)",
            ProbeOperations.FitGeometryToPoints, FitToPoints("FIT31O", seed: null)));
        steps.Watched(steps.NewCheck("c31-points-omitted", "31", "Count planes named FIT31O",
            ObjectsNamed(FixtureNames.Collection, "FIT31O", Api.ObjectType.Plane)) with { Hypotheses = [created] }, "p31-points-omitted");
        steps.Watched(steps.NewProbe("p31-projected-omitted", "31", "Fit a circle to GFIT projected to PA with the seed omitted (no setter)",
            ProbeOperations.FitGeometryToPointGroupProjectedToPlane, FitProjectedCircle("FIT31C")));
        steps.Watched(steps.NewCheck("c31-projected-omitted", "31", "Count circles named FIT31C",
            ObjectsNamed(FixtureNames.Collection, "FIT31C", Api.ObjectType.Circle)) with { Hypotheses = [created] }, "p31-projected-omitted");
    }

    // Probe 32 (#351, #360): the one code-4 trigger seen on both targets. Fitting G1, whose
    // points at z = +/-20 exceed the default profile's tolerance, returned MP code 4
    // (PARTIAL SUCCESS) in the 2026-10-07 session and created the plane. The shipped server
    // now reports it as a success with the typed partial-success state.
    private static void AddPartialSuccessProbe(StepList steps)
    {
        steps.Watched(steps.NewProbe("p32-partial", "32", "Fit the non-coplanar group G1 (tolerance exceeded) and read the typed partial success",
            ProbeOperations.FitGeometryToPointGroup, Fit(FixtureNames.GroupOne, "FIT32", Object(FixtureNames.PlaneA, Api.ObjectType.Plane))) with
        {
            Hypotheses =
            [
                new ProbeHypothesis("a typed partial success", ProbeOutcome.FactPrefix + "execution_state", Api.MpExecutionState.PartialSuccess.ToString()),
                new ProbeHypothesis("MP code 4 (PARTIAL SUCCESS)", ProbeOutcome.FactPrefix + "mp_result_code", "4")
            ]
        });
        steps.Watched(steps.NewCheck("c32-partial", "32", "Count planes named FIT32",
            ObjectsNamed(FixtureNames.Collection, "FIT32", Api.ObjectType.Plane)) with
        {
            Hypotheses = [new ProbeHypothesis("the partial fit created its plane", "match_count", "1")]
        }, "p32-partial");
    }

    // Probe 30 (#350, #312): a sample of the 22 remaining runtime selects, one per list getter
    // kind plus two single-value getters. Each prompt names the fixture and the gesture that
    // completed probe 22's selection in licensed session 2: F2, a check in the selection
    // tree, OK, then Enter.
    private static void AddRuntimeSelectProbe(StepList steps, FixtureManifest manifest)
    {
        static bool InHarnessCollection(string collection) => string.Equals(collection, FixtureNames.Collection, StringComparison.Ordinal);
        static bool IsP1(Api.PointName point) => InHarnessCollection(point.CollectionName) &&
            string.Equals(point.GroupName, FixtureNames.GroupOne, StringComparison.Ordinal) && string.Equals(point.TargetName, "P1", StringComparison.Ordinal);
        void Select(string id, string purpose, ProbeOperation operation, IMessage request) =>
            steps.Watched(steps.NewProbe(id, "30", purpose, operation, request) with { RequiresOperator = true });

        var instruments = manifest.UsmnInstruments;
        Select("p30-point-list", "Point name list runtime select (GetPointNameRefListArg); operator selects B277::G1::P1",
            ProbeOperations.PointNameRefListRuntimeSelect(IsP1),
            new Api.MakePointNameRefListRuntimeSelectRequest { UserPrompt = RuntimeSelectPrompt("point B277::G1::P1") });
        Select("p30-object-list", "Plane object name list runtime select (GetCollectionObjectNameRefListArg); operator selects B277::PA",
            ProbeOperations.CollectionObjectNameRefListRuntimeSelect(static plane =>
                InHarnessCollection(plane.CollectionName) && string.Equals(plane.ObjectName, FixtureNames.PlaneA, StringComparison.Ordinal)),
            new Api.MakeCollectionObjectNameRefListRuntimeSelectRequest
            {
                UserPrompt = RuntimeSelectPrompt("plane B277::PA"),
                ObjectType = Api.ObjectType.Plane
            });
        Select("p30-relationship-list", "Relationship list runtime select (GetCollectionObjectNameRefListArg); operator selects B277::R1",
            ProbeOperations.RelationshipRefListRuntimeSelect(static relationship =>
                InHarnessCollection(relationship.CollectionName) && string.Equals(relationship.ItemName, FixtureNames.Relationship, StringComparison.Ordinal)),
            new Api.MakeRelationshipRefListRuntimeSelectRequest { UserPrompt = RuntimeSelectPrompt("relationship B277::R1") });
        Select("p30-vector-list", "Vector name list runtime select (GetVectorNameRefListArg); operator selects one vector of B277::VG1",
            ProbeOperations.VectorNameRefListRuntimeSelect(static vector =>
                InHarnessCollection(vector.CollectionName) && string.Equals(vector.GroupName, FixtureNames.VectorGroup, StringComparison.Ordinal)),
            new Api.MakeVectorNameRefListRuntimeSelectRequest { UserPrompt = RuntimeSelectPrompt("one vector of vector group B277::VG1") });
        Select("p30-instrument-list", "Instrument list runtime select (GetColInstIdRefListArg); operator selects one USMN fixture instrument",
            ProbeOperations.CollectionInstrumentRefListRuntimeSelect(instrument => instruments.Any(fixture =>
                string.Equals(fixture.Collection, instrument.CollectionName, StringComparison.Ordinal) && fixture.InstrumentId == instrument.InstrumentId)),
            new Api.MakeCollectionInstrumentRefListRuntimeSelectRequest { UserPrompt = RuntimeSelectPrompt("one of the USMN fixture instruments") });
        Select("p30-point", "Point name runtime select (GetPointNameArg); operator selects B277::G1::P1",
            ProbeOperations.PointNameRuntimeSelect(IsP1),
            new Api.MakePointNameRuntimeSelectRequest { UserPrompt = RuntimeSelectPrompt("point B277::G1::P1") });
        Select("p30-collection", "Collection name runtime select (GetCollectionNameArg); operator selects B277",
            ProbeOperations.CollectionNameRuntimeSelect(static collection => InHarnessCollection(collection.Name)),
            new Api.MakeCollectionNameRuntimeSelectRequest { UserPrompt = RuntimeSelectPrompt("collection B277") });
    }

    // The prompt SA shows during a probe 30 step. It names a harness fixture, never a manifest value.
    public static string RuntimeSelectPrompt(string fixture) =>
        $"Briosa probe 30: press F2, check {fixture} in the selection tree, click OK, then press Enter.";

    // Probe 24 (#307 R0, and the #310 confound): the sibling radial delete with the same
    // Cloud Names binding and value form as the deleted X Y Z range operation. Each
    // delete has its own disposable cloud, bracketed by point counts, and an untouched
    // sentinel cloud is counted before and after the block.
    private static void AddRadialDeleteProbe(StepList steps)
    {
        var sentinel = Object(FixtureNames.DeleteSentinelCloud, Api.ObjectType.Cloud);
        steps.Add(CloudCount(steps, "c24-sentinel-before", $"Count the sentinel cloud {FixtureNames.DeleteSentinelCloud}; no step targets it",
            sentinel, eightRequired: true));
        AddRadialDelete(steps, "r0", FixtureNames.DeleteCloudR0, Api.ObjectType.Cloud,
            "R0: delete cloud points within 1 of D1, Cloud Names element typed Cloud");
        AddRadialDelete(steps, "r0a", FixtureNames.DeleteCloudR0a, Api.ObjectType.Any,
            "R0a: R0 with the Cloud Names element typed Any");
        steps.Add(CloudCount(steps, "c24-sentinel-after",
            $"Count the sentinel cloud {FixtureNames.DeleteSentinelCloud} again; anything but 8 is a containment failure",
            sentinel, eightRequired: true) with
        {
            Prerequisites = ["c24-sentinel-before", "p24-r0", "p24-r0a"]
        });
    }

    private static void AddRadialDelete(StepList steps, string name, string cloud, Api.ObjectType elementType, string purpose)
    {
        var counted = Object(cloud, Api.ObjectType.Cloud);
        steps.Add(CloudCount(steps, $"c24-{name}-before", $"Count points in disposable cloud {cloud} before deletion",
            counted, eightRequired: true));
        var request = new Api.DeleteCloudPointsByRadialDistanceFromPointsRequest { Radius = 1, DeleteInside = true };
        request.CloudNames.Add(Object(cloud, elementType));
        request.Points.Add(Point(FixtureNames.GroupDelete, "D1"));
        steps.Add(steps.NewProbe($"p24-{name}", "24", purpose, ProbeOperations.DeleteCloudPointsByRadialDistanceFromPoints, request) with
        {
            DestructiveTarget = FixtureNames.CloudKey(cloud),
            Prerequisites = [$"c24-{name}-before"],
            Checkpoints = OperatorCheckpoint.After
        });

        // An emptied or removed cloud may fail the count, so a failure is an observation here.
        steps.Add(CloudCount(steps, $"c24-{name}-after", $"Count points in disposable cloud {cloud} after deletion",
            counted, eightRequired: false) with
        {
            Hypotheses =
            [
                new ProbeHypothesis("D1 deleted and nothing else", "points_count", "7"),
                new ProbeHypothesis("nothing deleted", "points_count", "8")
            ],
            Prerequisites = [$"p24-{name}"]
        });
    }

    // Probe 25 (#313), public part. The baseline step first sets the restore mode, so
    // that the baseline and Silent steps differ only in SA Interaction Mode. The operator
    // records any dialog after every step; the restore step ends the block.
    private static void AddPublicInteractionModeProbe(StepList steps, InteractionModeSettings mode)
    {
        void Watched(ProbeStep step, params string[] prerequisites) =>
            steps.Add(step with { Checkpoints = OperatorCheckpoint.After, Prerequisites = prerequisites });

        Watched(steps.NewControl("p25-baseline", "25", "Set SA Interaction Mode to the restore mode before the baseline steps",
            ProbeOperations.SetInteractionMode, InteractionMode(mode.Restore)));
        Watched(steps.NewCheck("c25-zero-match-baseline", "25", "Wildcard selection that matches nothing, before Silent", NoMatch()),
            "p25-baseline");
        Watched(steps.NewProbe("p25-chart-missing-baseline", "25", "Chart whose template chart matches no chart, before Silent",
            ProbeOperations.CreateChartFromVectorGroup, Chart(MissingTemplateChart, "B277 C25B")), "p25-baseline");
        Watched(steps.NewProbe("p25-ask-baseline", "25", "Ask for String, before Silent (operator: enter B277-entered, OK)",
            ProbeOperations.AskForString, Ask(AskPrompt)) with { RequiresOperator = true }, "p25-baseline");

        Watched(steps.NewControl("p25-silent", "25", "Set SA Interaction Mode to Silent",
            ProbeOperations.SetInteractionMode, InteractionMode(Api.SaInteractionMode.Silent)), "p25-baseline");
        Watched(steps.NewCheck("c25-zero-match-silent", "25", "Wildcard selection that matches nothing, under Silent", NoMatch()),
            "p25-silent");
        Watched(steps.NewProbe("p25-chart-missing-silent", "25", "Chart whose template chart matches no chart, under Silent",
            ProbeOperations.CreateChartFromVectorGroup, Chart(MissingTemplateChart, "B277 C25S")), "p25-silent");
        Watched(steps.NewProbe("p25-ask-silent", "25", "Ask for String under Silent (operator: enter B277-entered, OK, if SA prompts)",
            ProbeOperations.AskForString, Ask(AskSilentPrompt)) with { RequiresOperator = true }, "p25-silent");

        Watched(steps.NewControl("p25-restore", "25", "Restore SA Interaction Mode",
            ProbeOperations.SetInteractionMode, InteractionMode(mode.Restore)), "p25-silent");
    }

    // Probe 25 (#313), worker part: the blank High and Low plane names, which the public
    // API rejects before dispatch, before and under Silent.
    private static void AddWorkerInteractionModeProbe(StepList steps, InteractionModeSettings mode)
    {
        void Watched(ProbeStep step, params string[] prerequisites) =>
            steps.Add(step with { Checkpoints = OperatorCheckpoint.After, Prerequisites = prerequisites });

        Watched(steps.NewControl("p25-baseline", "25", "Set SA Interaction Mode to the restore mode before the baseline steps",
            ProbeOperations.SetInteractionMode, InteractionMode(mode.Restore)));
        Watched(steps.NewVariant("p25-blank-high-baseline", "25", "Bounding planes with a blank High name, before Silent",
            ProbeOperations.ConstructPlanesBoundingPointGroup, Bounding("H25B", "L25B"), CommandVariant.Blank(HighPlaneLabel)),
            "p25-baseline");
        Watched(steps.NewVariant("p25-blank-low-baseline", "25", "Bounding planes with a blank Low name, before Silent",
            ProbeOperations.ConstructPlanesBoundingPointGroup, Bounding("H25C", "L25C"), CommandVariant.Blank(LowPlaneLabel)),
            "p25-baseline");

        Watched(steps.NewControl("p25-silent", "25", "Set SA Interaction Mode to Silent",
            ProbeOperations.SetInteractionMode, InteractionMode(Api.SaInteractionMode.Silent)), "p25-baseline");
        Watched(steps.NewVariant("p25-blank-high-silent", "25", "Bounding planes with a blank High name, under Silent",
            ProbeOperations.ConstructPlanesBoundingPointGroup, Bounding("H25S", "L25S"), CommandVariant.Blank(HighPlaneLabel)),
            "p25-silent");
        Watched(steps.NewCheck("c25-blank-high-silent-named", "25", "Count harness-named bounding planes matching *25S in B277",
            ObjectsNamed(FixtureNames.Collection, "*25S", Api.ObjectType.Plane)), "p25-silent");
        Watched(steps.NewCheck("c25-blank-high-silent-defaults", "25", "Count planes named HighPlane* in every collection",
            ObjectsNamed("*", "HighPlane*", Api.ObjectType.Plane)), "p25-silent");
        Watched(steps.NewVariant("p25-blank-low-silent", "25", "Bounding planes with a blank Low name, under Silent",
            ProbeOperations.ConstructPlanesBoundingPointGroup, Bounding("H25T", "L25T"), CommandVariant.Blank(LowPlaneLabel)),
            "p25-silent");
        Watched(steps.NewCheck("c25-blank-low-silent-named", "25", "Count harness-named bounding planes matching *25T in B277",
            ObjectsNamed(FixtureNames.Collection, "*25T", Api.ObjectType.Plane)), "p25-silent");
        Watched(steps.NewCheck("c25-blank-low-silent-defaults", "25", "Count planes named LowPlane* in every collection",
            ObjectsNamed("*", "LowPlane*", Api.ObjectType.Plane)), "p25-silent");

        Watched(steps.NewControl("p25-restore", "25", "Restore SA Interaction Mode",
            ProbeOperations.SetInteractionMode, InteractionMode(mode.Restore)), "p25-silent");
    }

    // Probe 29 (#313, PR #355): the server applies Silent after Connect, from the setting
    // --Briosa:SpatialAnalyzer:InteractionMode=Silent. Both public triggers should fail at
    // once with no dialog, as under the Silent that probe 25 set. The restore step then
    // returns SA to the operator's mode. The run holds no other probe.
    private static void AddInteractionModeSettingProbe(StepList steps, InteractionModeSettings mode)
    {
        var failed = new ProbeHypothesis("MP failure, as under Silent in licensed session 2", ProbeOutcome.FactPrefix + "mp_result_code", "3");
        void Setting(ProbeStep step) => steps.Watched(step with { ServerInteractionMode = Api.SaInteractionMode.Silent });

        Setting(steps.NewCheck("c29-zero-match-setting", "29", "Wildcard selection that matches nothing, under the server's Silent setting",
            NoMatch()) with { Hypotheses = [failed] });
        Setting(steps.NewProbe("p29-chart-missing-setting", "29", "Chart whose template chart matches no chart, under the server's Silent setting",
            ProbeOperations.CreateChartFromVectorGroup, Chart(MissingTemplateChart, "B277 C29S")) with { Hypotheses = [failed] });
        Setting(steps.NewControl("p29-restore", "29", "Restore SA Interaction Mode after the server's Silent setting",
            ProbeOperations.SetInteractionMode, InteractionMode(mode.Restore)));
    }

    // Probes 26 and 27 (#313, PR #355): with the server setting Unchanged and SA in Manual,
    // a state-triggered dialog blocks the MP until the server's watchdog ends it. The server
    // should then report sa-modal-dialog-open with operator-intervention guidance. Each
    // probe ends its run, so each runs in a run of its own or last.
    private static void AddPublicStallProbes(StepList steps, InteractionModeSettings mode)
    {
        AddStall(steps, "26", "p26-zero-match-stall", "Wildcard selection that matches nothing; leave its dialog open until the server reports",
            ProbeOperations.ObjectWildcardSelection, NoMatch(), CommandVariant.Shipped,
            StallExpectation.ServerReportsModalDialog, PublicStallClientTimeout, mode);
        AddStall(steps, "27", "p27-chart-missing-stall", "Chart whose template chart matches no chart; leave its dialog open until the server reports",
            ProbeOperations.CreateChartFromVectorGroup, Chart(MissingTemplateChart, "B277 C27D"), CommandVariant.Shipped,
            StallExpectation.ServerReportsModalDialog, PublicStallClientTimeout, mode);
    }

    // Probe 28 (#313): the public API rejects a blank plane name before dispatch, so the
    // server can never send the third trigger. The worker phase sends it, and the harness
    // runs the server's own modal-dialog detector when the step's deadline ends it.
    private static void AddWorkerStallProbe(StepList steps, InteractionModeSettings mode) =>
        AddStall(steps, "28", "p28-blank-high-stall", "Bounding planes with a blank High name; leave its dialog open until the step's deadline",
            ProbeOperations.ConstructPlanesBoundingPointGroup, Bounding("H28D", "L28D"), CommandVariant.Blank(HighPlaneLabel),
            StallExpectation.HarnessReportsModalDialog, WorkerStallClientTimeout, mode);

    // The baseline sets the restore mode, which a stall plan requires to be Manual, and the
    // stall step follows it with checkpoints on both sides.
    private static void AddStall(
        StepList steps, string probe, string id, string purpose, ProbeOperation operation, IMessage request, CommandVariant variant,
        StallExpectation stall, TimeSpan clientTimeout, InteractionModeSettings mode)
    {
        var baseline = $"p{probe}-baseline";
        steps.Watched(steps.NewControl(baseline, probe, "Set SA Interaction Mode to the restore mode (Manual) before the stall",
            ProbeOperations.SetInteractionMode, InteractionMode(mode.Restore)));
        steps.Add(new ProbeStep(id, probe, ProbeStepKind.Probe, steps.Phase, purpose, operation, request, variant, ProbeOutcomes.None)
        {
            ExpectedStall = stall,
            Checkpoints = OperatorCheckpoint.Before | OperatorCheckpoint.After,
            ClientTimeout = clientTimeout,
            Prerequisites = [baseline]
        });
    }

    private static void AddWorkerProbes(StepList steps, FixtureManifest manifest)
    {
        // #2 seed geometry: shipped control, skipped setter, blank value.
        var seed = Object(FixtureNames.PlaneA, Api.ObjectType.Plane);
        steps.Control("p02-shipped", "2", "Fit the coplanar group GFIT with a seed geometry value (shipped sequence)",
            ProbeOperations.FitGeometryToPointGroup, Fit("FIT2W", seed));
        steps.Check("c02-shipped", "2", "Count planes named FIT2W", ObjectsNamed(FixtureNames.Collection, "FIT2W", Api.ObjectType.Plane));
        steps.Variant("p02-omit", "2", "Fit with the seed geometry setter skipped",
            ProbeOperations.FitGeometryToPointGroup, Fit("FIT2O", seed), CommandVariant.Omit(SeedGeometryLabel));
        steps.Check("c02-omit", "2", "Count planes named FIT2O", ObjectsNamed(FixtureNames.Collection, "FIT2O", Api.ObjectType.Plane));
        steps.Variant("p02-blank", "2", "Fit with a blank seed geometry",
            ProbeOperations.FitGeometryToPointGroup, Fit("FIT2B", seed), CommandVariant.Blank(SeedGeometryLabel));
        steps.Check("c02-blank", "2", "Count planes named FIT2B", ObjectsNamed(FixtureNames.Collection, "FIT2B", Api.ObjectType.Plane));

        // #5 and #6: the SDK default names are HighPlane and LowPlane.
        steps.Check("c05-defaults-before", "5", "Count planes named HighPlane* in every collection",
            ObjectsNamed("*", "HighPlane*", Api.ObjectType.Plane));
        steps.Check("c06-defaults-before", "6", "Count planes named LowPlane* in every collection",
            ObjectsNamed("*", "LowPlane*", Api.ObjectType.Plane));
        steps.Control("p05-06-shipped", "5-6", "Bounding planes with both names set (shipped sequence)",
            ProbeOperations.ConstructPlanesBoundingPointGroup, Bounding("H56W", "L56W"));
        steps.Variant("p05-omit", "5", "Bounding planes with the High name setter skipped",
            ProbeOperations.ConstructPlanesBoundingPointGroup, Bounding("H5O", "L5O"), CommandVariant.Omit(HighPlaneLabel));
        steps.Check("c05-omit", "5", "Count planes named HighPlane* in every collection", ObjectsNamed("*", "HighPlane*", Api.ObjectType.Plane));
        steps.Check("c05-omit-named", "5", "Count harness-named bounding planes matching *5O in B277",
            ObjectsNamed(FixtureNames.Collection, "*5O", Api.ObjectType.Plane));
        steps.Variant("p05-blank", "5", "Bounding planes with a blank High name",
            ProbeOperations.ConstructPlanesBoundingPointGroup, Bounding("H5B", "L5B"), CommandVariant.Blank(HighPlaneLabel));
        steps.Check("c05-blank", "5", "Count planes named HighPlane* in every collection", ObjectsNamed("*", "HighPlane*", Api.ObjectType.Plane));
        steps.Check("c05-blank-named", "5", "Count harness-named bounding planes matching *5B in B277",
            ObjectsNamed(FixtureNames.Collection, "*5B", Api.ObjectType.Plane));
        steps.Variant("p06-omit", "6", "Bounding planes with the Low name setter skipped",
            ProbeOperations.ConstructPlanesBoundingPointGroup, Bounding("H6O", "L6O"), CommandVariant.Omit(LowPlaneLabel));
        steps.Check("c06-omit", "6", "Count planes named LowPlane* in every collection", ObjectsNamed("*", "LowPlane*", Api.ObjectType.Plane));
        steps.Check("c06-omit-named", "6", "Count harness-named bounding planes matching *6O in B277",
            ObjectsNamed(FixtureNames.Collection, "*6O", Api.ObjectType.Plane));
        steps.Variant("p06-blank", "6", "Bounding planes with a blank Low name",
            ProbeOperations.ConstructPlanesBoundingPointGroup, Bounding("H6B", "L6B"), CommandVariant.Blank(LowPlaneLabel));
        steps.Check("c06-blank", "6", "Count planes named LowPlane* in every collection", ObjectsNamed("*", "LowPlane*", Api.ObjectType.Plane));
        steps.Check("c06-blank-named", "6", "Count harness-named bounding planes matching *6B in B277",
            ObjectsNamed(FixtureNames.Collection, "*6B", Api.ObjectType.Plane));

        // #17-18: labels that differ only by letter case. p02-shipped is the exact-label control for #17,
        // so excluding #2 without #17 is refused.
        steps.Variant("p17-recased", "17", "Fit with 'Group to Fit' instead of the exact 'Group To Fit'",
            ProbeOperations.FitGeometryToPointGroup, Fit("FIT17R", seed), CommandVariant.Recase(GroupToFitLabel, GroupToFitRecased),
            "p02-shipped");
        steps.Check("c17-recased", "17", "Count planes named FIT17R", ObjectsNamed(FixtureNames.Collection, "FIT17R", Api.ObjectType.Plane));
        var unique = new Api.MakePointNameEnsureUniqueRequest { PointName = Point(FixtureNames.GroupOne, "P1"), UseNumberSuffix = true };
        steps.Control("p18-shipped", "18", "Ensure-unique point name with the exact 'Use Number Suffix?' label",
            ProbeOperations.MakePointNameEnsureUnique, unique);
        steps.Variant("p18-recased", "18", "Ensure-unique point name with 'Use number suffix?'",
            ProbeOperations.MakePointNameEnsureUnique, unique, CommandVariant.Recase(NumberSuffixLabel, NumberSuffixRecased));

        // #19-21: SDK step text versus SA 2026 documentation text.
        var orientation = new Api.ComputeGroupToGroupOrientationRxRyRzRequest
        {
            ReferenceGroup = Object(FixtureNames.GroupOne, Api.ObjectType.PointGroup),
            CorrespondingGroup = Object(FixtureNames.GroupRotated, Api.ObjectType.PointGroup)
        };
        steps.Variant("p19-sdk-text", "19", "Group-to-group orientation with SDK step text",
            ProbeOperations.ComputeGroupToGroupOrientation, orientation, CommandVariant.StepText(OrientationSdkStep));
        steps.Variant("p19-doc-text", "19", "Group-to-group orientation with documentation step text",
            ProbeOperations.ComputeGroupToGroupOrientation, orientation, CommandVariant.StepText(OrientationDocumentationStep));
        var angle = new Api.AngleBetweenTwoPlanesNormalsRequest
        {
            PlaneA = Object(FixtureNames.PlaneA, Api.ObjectType.Plane),
            PlaneB = Object(FixtureNames.PlaneB, Api.ObjectType.Plane),
            NominalAngle = 45,
            AngleTolerance = 0
        };
        steps.Variant("p20-sdk-text", "20", "Angle between plane normals with SDK step text (straight apostrophe)",
            ProbeOperations.AngleBetweenTwoPlanesNormals, angle, CommandVariant.StepText(AngleSdkStep));
        steps.Variant("p20-doc-text", "20", "Angle between plane normals with documentation step text (curly apostrophe)",
            ProbeOperations.AngleBetweenTwoPlanesNormals, angle, CommandVariant.StepText(AngleDocumentationStep));
        var showHide = new Api.ShowHidePointsRequest { Show = true };
        showHide.PointNames.Add(Point(FixtureNames.GroupOne, "P1"));
        steps.Variant("p21-sdk-text", "21", "Show/hide points with SDK step text",
            ProbeOperations.ShowHidePoints, showHide, CommandVariant.StepText(ShowHideSdkStep));
        steps.Variant("p21-doc-text", "21", "Show/hide points with documentation step text",
            ProbeOperations.ShowHidePoints, showHide, CommandVariant.StepText(ShowHideDocumentationStep));

        // Manual fixtures run last.
        var usmn = Usmn(manifest);
        steps.Control("p01-shipped", "1", "USMN with a nominals group value (shipped sequence)", ProbeOperations.LocateInstrumentsUsmn, usmn);
        steps.Variant("p01-omit", "1", "USMN with the nominals setter skipped", ProbeOperations.LocateInstrumentsUsmn, usmn,
            CommandVariant.Omit(NominalsLabel));
        steps.Variant("p01-blank", "1", "USMN with a blank nominals group", ProbeOperations.LocateInstrumentsUsmn, usmn,
            CommandVariant.Blank(NominalsLabel));
        steps.Control("p03-shipped", "3", "Chart with a template chart value (shipped sequence)",
            ProbeOperations.CreateChartFromVectorGroup, Chart(manifest.TemplateChartName, "B277 C3W"));
        steps.Variant("p03-omit", "3", "Chart with the template setter skipped",
            ProbeOperations.CreateChartFromVectorGroup, Chart(manifest.TemplateChartName, "B277 C3O"), CommandVariant.Omit(TemplateChartLabel));
        steps.Variant("p03-blank", "3", "Chart with a blank template chart name",
            ProbeOperations.CreateChartFromVectorGroup, Chart(manifest.TemplateChartName, "B277 C3B"), CommandVariant.Blank(TemplateChartLabel));
        steps.Control("p04-shipped", "4", "User interface profile with a profile file (shipped sequence)",
            ProbeOperations.SetUserInterfaceProfile, Profile(manifest));
        steps.Variant("p04-omit", "4", "User interface profile with the file setter skipped",
            ProbeOperations.SetUserInterfaceProfile, Profile(manifest), CommandVariant.Omit(ProfileFileLabel));
        steps.Variant("p04-blank", "4", "User interface profile with a blank file",
            ProbeOperations.SetUserInterfaceProfile, Profile(manifest), CommandVariant.Blank(ProfileFileLabel));
    }

    private static ProbeStep CloudSetup(string id, string group, string cloud, string? fixtureKey, ProbePhase phase) =>
        new(id, "setup", ProbeStepKind.Setup, phase, $"Construct cloud {cloud} from point group {group}",
            ProbeOperations.ConstructCloudFromGroup, new Api.ConstructPointCloudsFromExistingPointGroupRequest
            {
                PointGroupName = Object(group, Api.ObjectType.PointGroup),
                CloudName = Object(cloud, Api.ObjectType.Cloud)
            },
            CommandVariant.Shipped, ProbeOutcomes.Succeeded)
        {
            CreatesFixture = fixtureKey
        };

    // A before-count or sentinel count must succeed with exactly the eight fixture points.
    private static ProbeStep CloudCount(StepList steps, string id, string purpose, Api.CollectionObjectName cloud, bool eightRequired) =>
        new(id, "24", ProbeStepKind.Check, steps.Phase, purpose,
            ProbeOperations.GetCloudPointCount, new Api.GetCloudPointCountRequest { CloudName = cloud },
            CommandVariant.Shipped, eightRequired ? ProbeOutcomes.Succeeded : ProbeOutcomes.Succeeded | ProbeOutcomes.MpFailed)
        {
            Hypotheses = eightRequired ? [new ProbeHypothesis("fixture intact", "points_count", "8")] : [],
            Requirement = eightRequired
                ? new ProbeRequirement("the cloud holds exactly the eight fixture points",
                    static (_, outcome) => outcome.Observations.TryGetValue("points_count", out var count) &&
                        string.Equals(count, "8", StringComparison.Ordinal))
                : null
        };

    private static Api.MakeCollectionObjectNameRefListWildcardSelectionRequest ObjectsNamed(
        string collection, string pattern, Api.ObjectType type) =>
        new()
        {
            CollectionWildcardCriteria = collection,
            ObjectWildcardCriteria = pattern,
            ObjectType = type
        };

    private static Api.MakeCollectionObjectNameRefListWildcardSelectionRequest NoMatch() =>
        ObjectsNamed(FixtureNames.Collection, "NOMATCH25*", Api.ObjectType.Any);

    private static Api.FitGeometryToPointGroupRequest Fit(string result, Api.CollectionObjectName seed) =>
        Fit(FixtureNames.GroupFit, result, seed);

    private static Api.FitGeometryToPointGroupRequest Fit(string group, string result, Api.CollectionObjectName seed) => new()
    {
        GeometryType = Api.GeometryType.Plane,
        GroupToFit = Object(group, Api.ObjectType.PointGroup),
        ResultingObjectName = Object(result, Api.ObjectType.Plane),
        ReportDeviations = false,
        IgnoreOutOfTolerancePoints = false,
        StartingConditionGeometry = seed
    };

    // A null seed leaves the field absent, so the shipped mapping skips its setter (#357).
    private static Api.FitGeometryToPointsRequest FitToPoints(string result, Api.CollectionObjectName? seed)
    {
        var request = new Api.FitGeometryToPointsRequest
        {
            GeometryType = Api.GeometryType.Plane,
            ResultingObjectName = Object(result, Api.ObjectType.Plane),
            ReportDeviations = false,
            IgnoreOutOfTolerancePoints = false
        };
        request.PointsToFit.AddRange(FitPoints.Select(static point => Point(FixtureNames.GroupFit, point.Name)));
        if (seed is not null)
        {
            request.StartingConditionGeometry = seed;
        }

        return request;
    }

    // GFIT's four corners lie on one circle, so a circle fit in PA (z = 0) is exact.
    private static Api.FitGeometryToPointGroupProjectedToPlaneRequest FitProjectedCircle(string result) => new()
    {
        GeometryType = Api.GeometryType.Circle,
        GroupToFit = Object(FixtureNames.GroupFit, Api.ObjectType.PointGroup),
        PlaneName = Object(FixtureNames.PlaneA, Api.ObjectType.Plane),
        ResultingObjectName = Object(result, Api.ObjectType.Circle),
        ReportDeviations = false,
        IgnoreOutOfTolerancePoints = false
    };

    private static Api.ConstructPlanesBoundingPointGroupRequest Bounding(string high, string low) => new()
    {
        ReferencePlaneName = Object(FixtureNames.PlaneA, Api.ObjectType.Plane),
        GroupToBound = Object(FixtureNames.GroupOne, Api.ObjectType.PointGroup),
        ResultingHighPlaneName = Object(high, Api.ObjectType.Plane),
        ResultingLowPlaneName = Object(low, Api.ObjectType.Plane),
        OverrideTargetPointOffsets = false
    };

    private static Api.LocateInstrumentsUsmnRequest Usmn(FixtureManifest manifest)
    {
        var request = new Api.LocateInstrumentsUsmnRequest
        {
            NominalsGroup = new Api.CollectionObjectName
            {
                CollectionName = manifest.UsmnNominalsGroup!.Collection,
                ObjectName = manifest.UsmnNominalsGroup.Name,
                ObjectType = Api.ObjectType.PointGroup
            },
            OutputGroup = Object("USMN1", Api.ObjectType.PointGroup),
            MoveInWorkingFrame = true,
            AutoRejectOutliersAndResolve = false,
            ShowUsmnDialog = Api.ShowUsmnDialog.No,
            MaximumAcceptableRmsError = 0,
            MaximumAcceptableError = 0,
            ExcludeSingleInstrumentPoints = false,
            RunUncertaintyFieldAnalysis = false
        };
        request.Instruments.AddRange(manifest.UsmnInstruments.Select(Instrument));
        // A point group that USMN does not use; the shipped mapping requires one.
        request.ExcludedGroups.Add(Object(FixtureNames.GroupMeasured, Api.ObjectType.PointGroup));
        return request;
    }

    private static Api.CreateChartFromVectorGroupRequest Chart(string templateChartName, string name) => new()
    {
        NewChartName = new Api.ChartName { Name = name },
        VectorGroupName = Object(FixtureNames.VectorGroup, Api.ObjectType.VectorGroup),
        ChartType = Api.ChartType.RunChart,
        DataSetToChart = Api.DatasetType.Magnitude,
        AuxDataSetToChart = Api.DatasetType.Magnitude,
        TemplateChartName = new Api.ChartName { Name = templateChartName },
        ShowInterface = false
    };

    private static Api.SetUserInterfaceProfileRequest Profile(FixtureManifest manifest) => new()
    {
        ProfileName = manifest.UiProfileName,
        ProfileFileName = new Api.FileReference { Path = manifest.UiProfileFile, EmbeddedFile = false }
    };

    private static Api.SetInteractionModeRequest InteractionMode(Api.SaInteractionMode mode) => new()
    {
        SaInteractionMode = mode,
        MeasurementPlanInteractionMode = InteractionModeSettings.MpInteractionMode,
        MeasurementPlanDialogInteractionMode = InteractionModeSettings.MpDialogInteractionMode
    };

    private static Api.AskForStringRequest Ask(string prompt) => new()
    {
        QuestionToAsk = prompt,
        PasswordEntry = false,
        InitialAnswer = AskInitialAnswer
    };

    private static Api.CollectionInstrumentId Instrument(ManifestInstrument instrument) =>
        new() { CollectionName = instrument.Collection, InstrumentId = instrument.InstrumentId };

    private static Api.CollectionObjectName Object(string name, Api.ObjectType type) =>
        new() { CollectionName = FixtureNames.Collection, ObjectName = name, ObjectType = type };

    private static Api.PointName Point(string group, string target) =>
        new() { CollectionName = FixtureNames.Collection, GroupName = group, TargetName = target };

    private static Api.Vector Vector(double x, double y, double z) => new() { X = x, Y = y, Z = z };

    /// <summary>Accumulates steps for one phase with the reviewed acceptance sets.</summary>
    private sealed class StepList(ProbePhase phase)
    {
        private readonly List<ProbeStep> _steps = [];

        public ProbePhase Phase { get; } = phase;

        public void Add(ProbeStep step) => _steps.Add(step);

        // The operator records any dialog after the step, before anything else is sent.
        public void Watched(ProbeStep step, params string[] prerequisites) =>
            _steps.Add(step with { Checkpoints = OperatorCheckpoint.After, Prerequisites = prerequisites });

        public void Guard(string id, string probe, string purpose, ProbeOperation operation, IMessage request) =>
            _steps.Add(new(id, probe, ProbeStepKind.Guard, Phase, purpose, operation, request, CommandVariant.Shipped, ProbeOutcomes.Succeeded));

        public void Setup(string id, string purpose, ProbeOperation operation, IMessage request) =>
            _steps.Add(new(id, "setup", ProbeStepKind.Setup, Phase, purpose, operation, request, CommandVariant.Shipped, ProbeOutcomes.Succeeded));

        public void Check(string id, string probe, string purpose, IMessage request, params ProbeHypothesis[] hypotheses) =>
            _steps.Add(NewCheck(id, probe, purpose, request) with { Hypotheses = hypotheses });

        public void Probe(string id, string probe, string purpose, ProbeOperation operation, IMessage request) =>
            _steps.Add(NewProbe(id, probe, purpose, operation, request));

        public void Variant(
            string id, string probe, string purpose, ProbeOperation operation, IMessage request, CommandVariant variant,
            params string[] prerequisites) =>
            _steps.Add(NewVariant(id, probe, purpose, operation, request, variant) with { Prerequisites = prerequisites });

        public void Control(string id, string probe, string purpose, ProbeOperation operation, IMessage request) =>
            _steps.Add(NewControl(id, probe, purpose, operation, request));

        // Wildcard selections may report an empty match as an MP failure; both are observations.
        public ProbeStep NewCheck(string id, string probe, string purpose, IMessage request) =>
            new(id, probe, ProbeStepKind.Check, Phase, purpose, ProbeOperations.ObjectWildcardSelection, request,
                CommandVariant.Shipped, ProbeOutcomes.Succeeded | ProbeOutcomes.MpFailed);

        // Public probes and worker variants: every determinate SDK outcome is an observation.
        // An outcome whose completion is unknown is never accepted; it stops the session.
        public ProbeStep NewProbe(string id, string probe, string purpose, ProbeOperation operation, IMessage request) =>
            new(id, probe, ProbeStepKind.Probe, Phase, purpose, operation, request, CommandVariant.Shipped,
                ProbeOutcomes.AnyDeterminateSdkOutcome);

        public ProbeStep NewVariant(
            string id, string probe, string purpose, ProbeOperation operation, IMessage request, CommandVariant variant) =>
            new(id, probe, ProbeStepKind.Probe, Phase, purpose, operation, request, variant, ProbeOutcomes.AnyDeterminateSdkOutcome);

        // A control validates the fixture or sets session state; anything but success stops the session.
        public ProbeStep NewControl(string id, string probe, string purpose, ProbeOperation operation, IMessage request) =>
            new(id, probe, ProbeStepKind.Probe, Phase, purpose, operation, request, CommandVariant.Shipped, ProbeOutcomes.Succeeded);

        public List<ProbeStep> ToList() => [.. _steps];
    }
}

/// <summary>
/// The SA Interaction Mode values probe 25 sends (#313). SA persists the SA
/// Interaction Mode and the MP Dialog Interaction Mode across restarts and has no
/// getter, so the operator supplies the mode to restore. The MP modes are fixed.
/// </summary>
internal sealed record InteractionModeSettings(Api.SaInteractionMode Restore, bool Supplied)
{
    // SA documents that the MP Interaction Mode returns to this value when a script exits.
    public const Api.MpInteractionMode MpInteractionMode = Api.MpInteractionMode.HaltOnFailureOrPartialSuccess;

    // Maintainer decision (2026-10-19): the baseline, Silent, and restore steps all allow
    // application interaction, so the operator can use SA's UI during prompts.
    public const Api.MpDialogInteractionMode MpDialogInteractionMode = Api.MpDialogInteractionMode.AllowApplicationInteraction;

    // Dry runs and portable tests only; a licensed run that changes the mode requires the operator's value.
    public static InteractionModeSettings Placeholder { get; } = new(Api.SaInteractionMode.Manual, Supplied: false);

    public static string ModeName(Api.SaInteractionMode mode) => mode switch
    {
        Api.SaInteractionMode.Manual => "Manual",
        Api.SaInteractionMode.Automatic => "Automatic",
        Api.SaInteractionMode.Silent => "Silent",
        _ => "unspecified"
    };
}
