using System.Globalization;
using Google.Protobuf;
using Api = global::Briosa;

namespace Briosa.LicensedProbes;

/// <summary>
/// The maintainer-approved probe list from #277, expressed as immutable steps.
/// The public phase runs every probe the public API can express; the worker
/// phase runs only what the public API intentionally cannot express (a skipped
/// setter, a blank required value, a recased label, alternate step text).
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

    public static IReadOnlyList<string> ProbeNumbers { get; } =
        [.. Enumerable.Range(1, 23).Select(static number => number.ToString(CultureInfo.InvariantCulture))];

    public static IReadOnlyList<ProbeStep> Create(ProbePhase phase, FixtureManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var steps = new StepList(phase);
        AddCollectionGuard(steps);
        if (phase == ProbePhase.PublicApi)
        {
            AddPublicFixtures(steps, manifest);
            AddPublicProbes(steps, manifest);
        }
        else
        {
            AddWorkerFixtures(steps);
            AddWorkerProbes(steps, manifest);
        }

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
                static (history, outcome) => CollectionCountIncreased(history, outcome))
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

    // Six points, non-coplanar so bounding planes and fits are well defined.
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

    // Measured points: one within a +/-1 magnitude tolerance and two outside,
    // so the in-tolerance percentage is one third (a fractional value).
    internal static IReadOnlyList<(string Name, double X, double Y, double Z)> MeasuredPoints { get; } =
        [("P1", 0, 0, 0.1), ("P2", 100, 0, 5), ("P3", 0, 100, 5)];

    // Every point has X = 5 except the two outside the X range. The deleted
    // count distinguishes how omitted Y/Z bounds are treated (#16):
    // unbounded deletes six, bounds retained from #15 delete two, zero bounds
    // delete one, and an ignored range deletes none.
    internal static IReadOnlyList<(string Name, double X, double Y, double Z)> DeletePoints { get; } =
    [
        ("D1", 0, 0, 0), ("D2", 5, 5, 5), ("D3", 5, 0, 0), ("D4", 5, 20, 20),
        ("D5", 20, 20, 20), ("D7", 5, 21, 21), ("D8", 5, 8, 8), ("D9", 5, 9, 9)
    ];

    private static void AddCommonGeometry(StepList steps)
    {
        AddPointGroup(steps, FixtureNames.GroupOne, GroupOnePoints);
        AddPointGroup(steps, FixtureNames.GroupRotated, RotatedPoints);
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

    private static void AddPublicFixtures(StepList steps, FixtureManifest manifest)
    {
        AddCommonGeometry(steps);
        AddPointGroup(steps, FixtureNames.GroupDelete, DeletePoints);
        steps.Setup("s-grid-GRID", "Lay out a planar 5 x 5 point grid for the polygonize cloud",
            ProbeOperations.ConstructPointsOnGrid, new Api.ConstructPointsLayoutOnGridRequest
            {
                GroupName = Object(FixtureNames.GroupGrid, Api.ObjectType.PointGroup),
                PointPrefix = "Q",
                XMin = 0, XMax = 40, XCount = 5,
                YMin = 0, YMax = 40, YCount = 5,
                ZMin = 0, ZMax = 0, ZCount = 1
            });
        steps.Add(CloudSetup("s-cloud-CPOLY", FixtureNames.GroupGrid, FixtureNames.PolygonizeCloud, null, steps.Phase));
        steps.Add(CloudSetup("s-cloud-CDEL15", FixtureNames.GroupDelete, FixtureNames.DeleteCloudAllBounds,
            FixtureNames.DeleteCloudAllBoundsKey, steps.Phase));
        steps.Add(CloudSetup("s-cloud-CDEL16", FixtureNames.GroupDelete, FixtureNames.DeleteCloudPartialBounds,
            FixtureNames.DeleteCloudPartialBoundsKey, steps.Phase));
        steps.Setup("s-frame-F1", "Construct identity frame F1", ProbeOperations.ConstructFrame,
            new Api.ConstructFrameRequest
            {
                NewFrameName = Object(FixtureNames.Frame, Api.ObjectType.Frame),
                TransformInWorkingCoordinates = Identity()
            });
        if (manifest.CircleLineSurface is null)
        {
            steps.Setup("s-cylinder-CY1", "Construct cylinder CY1 away from the other fixtures", ProbeOperations.ConstructCylinder,
                new Api.ConstructCylinderRequest
                {
                    CylinderName = Object(FixtureNames.Cylinder, Api.ObjectType.Cylinder),
                    CylinderEndPoint = Vector(200, 0, 0),
                    CylinderAxis = Vector(0, 0, 1),
                    CylinderDiameter = 20,
                    CylinderLength = 30
                });
            steps.Setup("s-surface-SCYL", "Construct surface SCYL from cylinder CY1", ProbeOperations.ConstructSurfaceFromCylinder,
                new Api.ConstructSurfaceFromCylinderRequest
                {
                    ResultingSurfaceName = Object(FixtureNames.CylinderSurface, Api.ObjectType.Surface),
                    CylinderName = Object(FixtureNames.Cylinder, Api.ObjectType.Cylinder)
                });
        }

        if (manifest.CalloutView is null)
        {
            var callout = new Api.CreateTextCalloutRequest
            {
                DestinationCalloutView = CalloutView(manifest),
                CalloutAnchorPoint = Point(FixtureNames.GroupOne, "P1")
            };
            callout.Text.Add("B277");
            steps.Setup("s-callout-V1", "Create a text callout in callout view V1", ProbeOperations.CreateTextCallout, callout);
        }
    }

    private static void AddWorkerFixtures(StepList steps) => AddCommonGeometry(steps);

    private static void AddPublicProbes(StepList steps, FixtureManifest manifest)
    {
        // Programmatic fixtures first; probes that need manual fixtures run last.
        steps.Probe("p02-value", "2", "Fit with a seed geometry value (public API)",
            ProbeOperations.FitGeometryToPointGroup, Fit("FIT2V", Object(FixtureNames.PlaneA, Api.ObjectType.Plane)));
        steps.Check("c02-value", "2", "Count planes named FIT2V", ObjectsNamed(FixtureNames.Collection, "FIT2V", Api.ObjectType.Plane));

        steps.Probe("p05-06-value", "5-6", "Bounding planes with both names set (public API)",
            ProbeOperations.ConstructPlanesBoundingPointGroup, Bounding("H56V", "L56V"));
        steps.Check("c05-06-value", "5-6", "Count planes named *56V",
            ObjectsNamed(FixtureNames.Collection, "*56V", Api.ObjectType.Plane),
            new ProbeHypothesis("both named planes created", "match_count", "2"));

        steps.Probe("p07", "7", "Callout view wildcard output via the substitute list getter",
            ProbeOperations.CalloutViewWildcardSelection, new Api.MakeCalloutViewRefListWildcardSelectionRequest
            {
                CollectionWildcardCriteria = CalloutView(manifest).CollectionName,
                CalloutViewWildcardCriteria = "*"
            });
        var properties = new Api.SetCalloutViewPropertiesRequest { Properties = new Api.CalloutViewProperties() };
        properties.CalloutViews.Add(CalloutView(manifest));
        steps.Probe("p08", "8", "Callout view properties via the substitute list setter",
            ProbeOperations.SetCalloutViewProperties, properties);
        steps.Probe("p09-circle", "9", "Circle Line Mode = Circle via SetStringArg",
            ProbeOperations.ConstructCirclesLinesFromSurfaces, CirclesLines(manifest, Api.CircleLineMode.Circle, "CIR9"));
        steps.Probe("p09-line", "9", "Circle Line Mode = Line via SetStringArg",
            ProbeOperations.ConstructCirclesLinesFromSurfaces, CirclesLines(manifest, Api.CircleLineMode.Line, "LIN9"));
        var mirror = new Api.MirrorObjectsRequest
        {
            FrameName = Object(FixtureNames.Frame, Api.ObjectType.Frame),
            FramePlaneToMirrorAround = Api.MirrorFramePlane.Yz,
            Copy = true
        };
        mirror.Objects.Add(Object(FixtureNames.GroupOne, Api.ObjectType.PointGroup));
        steps.Probe("p10", "10", "Mirror frame plane via SetStringArg (copy)", ProbeOperations.MirrorObjects, mirror);
        var polygonize = new Api.ConstructPolygonizedSurfaceFromPointCloudsRequest
        {
            MeshOrientation = Api.MeshOrientationType.UseCurrentWorkingFrame,
            PolygonizedSurfaceName = Object("MESH11", Api.ObjectType.ScanStripeMesh)
        };
        polygonize.PointCloudList.Add(Object(FixtureNames.PolygonizeCloud, Api.ObjectType.Cloud));
        steps.Probe("p11", "11", "Mesh Orientation via SetStringArg", ProbeOperations.ConstructPolygonizedSurface, polygonize);
        steps.Check("c11", "11", "Count objects named MESH11*", ObjectsNamed(FixtureNames.Collection, "MESH11*", Api.ObjectType.Any));

        steps.Probe("p12", "12", "Point name getter after Ensure Unique", ProbeOperations.MakePointNameEnsureUnique,
            new Api.MakePointNameEnsureUniqueRequest { PointName = Point(FixtureNames.GroupOne, "P1"), UseNumberSuffix = true });
        steps.Probe("p13", "13", "Collection object name getter after Ensure Unique", ProbeOperations.MakeCollectionObjectNameEnsureUnique,
            new Api.MakeCollectionObjectNameEnsureUniqueRequest
            {
                CollectionObjectName = Object(FixtureNames.GroupOne, Api.ObjectType.PointGroup),
                UseNumberSuffix = true
            });

        AddDeleteProbe(steps, "15", FixtureNames.DeleteCloudAllBounds, FixtureNames.DeleteCloudAllBoundsKey,
            "All six bounds set, Delete Inside = TRUE",
            static request =>
            {
                request.XMin = 4; request.XMax = 6;
                request.YMin = 15; request.YMax = 25;
                request.ZMin = 15; request.ZMax = 25;
            },
            [
                new ProbeHypothesis("all six bounds honored (two points inside)", "points_count", "6"),
                new ProbeHypothesis("nothing deleted", "points_count", "8")
            ]);
        AddDeleteProbe(steps, "16", FixtureNames.DeleteCloudPartialBounds, FixtureNames.DeleteCloudPartialBoundsKey,
            "Only X bounds set (Y and Z omitted), Delete Inside = TRUE",
            static request =>
            {
                request.XMin = 4; request.XMax = 6;
            },
            [
                new ProbeHypothesis("omitted Y/Z bounds not applied (unbounded)", "points_count", "2"),
                new ProbeHypothesis("omitted Y/Z bounds retained from #15", "points_count", "6"),
                new ProbeHypothesis("omitted Y/Z bounds treated as zero", "points_count", "7"),
                new ProbeHypothesis("nothing deleted", "points_count", "8")
            ]);

        steps.Probe("p23", "23", "Vector group properties with a one-third in-tolerance fraction",
            ProbeOperations.GetVectorGroupProperties, new Api.GetVectorGroupPropertiesRequest
            {
                VectorGroupName = Object(FixtureNames.VectorGroup, Api.ObjectType.VectorGroup)
            });

        // Manual fixtures.
        steps.Probe("p01-value", "1", "USMN with a nominals group value (public API)",
            ProbeOperations.LocateInstrumentsUsmn, Usmn(manifest));
        steps.Probe("p03-value", "3", "Chart with a template chart value (public API)",
            ProbeOperations.CreateChartFromVectorGroup, Chart(manifest, "B277 C3V"));
        var instruments = new Api.AddCollectionInstrumentsToRefListWildcardSelectionRequest
        {
            CollectionWildcardCriteria = "*",
            InstrumentWildcardCriteria = "*"
        };
        instruments.CollectionInstrumentRefList.Add(Instrument(manifest.UsmnInstruments[0]));
        steps.Probe("p14", "14", "Instrument reference list getter after wildcard add",
            ProbeOperations.AddCollectionInstrumentsWildcardSelection, instruments);
        steps.Probe("p04-value", "4", "User interface profile with a profile file value (public API)",
            ProbeOperations.SetUserInterfaceProfile, Profile(manifest));
        steps.Add(new ProbeStep("p22", "22", ProbeStepKind.Probe, steps.Phase,
            "Runtime-select vector group list getter (operator selects B277::VG1)",
            ProbeOperations.VectorGroupRuntimeSelect, new Api.MakeCollectionVectorGroupNameRefListRuntimeSelectRequest
            {
                UserPrompt = "Briosa #277 probe: select vector group B277::VG1 and confirm."
            },
            CommandVariant.Shipped, ProbeOutcomes.AnyDeterminateSdkOutcome)
        {
            RequiresOperator = true
        });
    }

    private static void AddWorkerProbes(StepList steps, FixtureManifest manifest)
    {
        // #2 seed geometry: shipped control, skipped setter, blank value.
        var seed = Object(FixtureNames.PlaneA, Api.ObjectType.Plane);
        steps.Control("p02-shipped", "2", "Fit with a seed geometry value (shipped sequence)",
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

        // #17-18: labels that differ only by letter case. p02-shipped is the exact-label control for #17.
        steps.Variant("p17-recased", "17", "Fit with 'Group to Fit' instead of the exact 'Group To Fit'",
            ProbeOperations.FitGeometryToPointGroup, Fit("FIT17R", seed), CommandVariant.Recase(GroupToFitLabel, GroupToFitRecased));
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
            ProbeOperations.CreateChartFromVectorGroup, Chart(manifest, "B277 C3W"));
        steps.Variant("p03-omit", "3", "Chart with the template setter skipped",
            ProbeOperations.CreateChartFromVectorGroup, Chart(manifest, "B277 C3O"), CommandVariant.Omit(TemplateChartLabel));
        steps.Variant("p03-blank", "3", "Chart with a blank template chart name",
            ProbeOperations.CreateChartFromVectorGroup, Chart(manifest, "B277 C3B"), CommandVariant.Blank(TemplateChartLabel));
        steps.Control("p04-shipped", "4", "User interface profile with a profile file (shipped sequence)",
            ProbeOperations.SetUserInterfaceProfile, Profile(manifest));
        steps.Variant("p04-omit", "4", "User interface profile with the file setter skipped",
            ProbeOperations.SetUserInterfaceProfile, Profile(manifest), CommandVariant.Omit(ProfileFileLabel));
        steps.Variant("p04-blank", "4", "User interface profile with a blank file",
            ProbeOperations.SetUserInterfaceProfile, Profile(manifest), CommandVariant.Blank(ProfileFileLabel));
    }

    private static void AddDeleteProbe(
        StepList steps,
        string probe,
        string cloud,
        string fixtureKey,
        string purpose,
        Action<Api.DeleteCloudPointsByXYZRangeRequest> bounds,
        IReadOnlyList<ProbeHypothesis> hypotheses)
    {
        var cloudName = Object(cloud, Api.ObjectType.Cloud);
        steps.Add(new ProbeStep($"c{probe}-before", probe, ProbeStepKind.Check, steps.Phase,
            $"Count points in disposable cloud {cloud} before deletion",
            ProbeOperations.GetCloudPointCount, new Api.GetCloudPointCountRequest { CloudName = cloudName },
            CommandVariant.Shipped, ProbeOutcomes.Succeeded)
        {
            Hypotheses = [new ProbeHypothesis("fixture intact", "points_count", "8")],
            Requirement = new ProbeRequirement("the disposable cloud holds exactly the eight fixture points",
                static (_, outcome) => outcome.Observations.TryGetValue("points_count", out var count) &&
                    string.Equals(count, "8", StringComparison.Ordinal))
        });
        var request = new Api.DeleteCloudPointsByXYZRangeRequest { DeleteInside = true };
        request.CloudNames.Add(cloudName);
        bounds(request);
        steps.Add(new ProbeStep($"p{probe}", probe, ProbeStepKind.Probe, steps.Phase, purpose,
            ProbeOperations.DeleteCloudPointsByXyzRange, request, CommandVariant.Shipped, ProbeOutcomes.AnyDeterminateSdkOutcome)
        {
            DestructiveTarget = fixtureKey
        });
        steps.Add(new ProbeStep($"c{probe}-after", probe, ProbeStepKind.Check, steps.Phase,
            $"Count points in disposable cloud {cloud} after deletion",
            ProbeOperations.GetCloudPointCount, new Api.GetCloudPointCountRequest { CloudName = cloudName },
            CommandVariant.Shipped, ProbeOutcomes.Succeeded)
        {
            Hypotheses = hypotheses
        });
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

    private static Api.MakeCollectionObjectNameRefListWildcardSelectionRequest ObjectsNamed(
        string collection, string pattern, Api.ObjectType type) =>
        new()
        {
            CollectionWildcardCriteria = collection,
            ObjectWildcardCriteria = pattern,
            ObjectType = type
        };

    private static Api.FitGeometryToPointGroupRequest Fit(string result, Api.CollectionObjectName seed) => new()
    {
        GeometryType = Api.GeometryType.Plane,
        GroupToFit = Object(FixtureNames.GroupOne, Api.ObjectType.PointGroup),
        ResultingObjectName = Object(result, Api.ObjectType.Plane),
        ReportDeviations = false,
        IgnoreOutOfTolerancePoints = false,
        StartingConditionGeometry = seed
    };

    private static Api.ConstructPlanesBoundingPointGroupRequest Bounding(string high, string low) => new()
    {
        ReferencePlaneName = Object(FixtureNames.PlaneA, Api.ObjectType.Plane),
        GroupToBound = Object(FixtureNames.GroupOne, Api.ObjectType.PointGroup),
        ResultingHighPlaneName = Object(high, Api.ObjectType.Plane),
        ResultingLowPlaneName = Object(low, Api.ObjectType.Plane),
        OverrideTargetPointOffsets = false
    };

    private static Api.ConstructCirclesLinesFromSurfacesRequest CirclesLines(FixtureManifest manifest, Api.CircleLineMode mode, string baseName)
    {
        var request = new Api.ConstructCirclesLinesFromSurfacesRequest
        {
            CircleLineMode = mode,
            DestinationCollectionName = new Api.CollectionName { Name = FixtureNames.Collection },
            BaseName = baseName
        };
        request.Surfaces.Add(manifest.CircleLineSurface is { } surface
            ? new Api.CollectionObjectName { CollectionName = surface.Collection, ObjectName = surface.Name, ObjectType = Api.ObjectType.Surface }
            : Object(FixtureNames.CylinderSurface, Api.ObjectType.Surface));
        return request;
    }

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

    private static Api.CreateChartFromVectorGroupRequest Chart(FixtureManifest manifest, string name) => new()
    {
        NewChartName = new Api.ChartName { Name = name },
        VectorGroupName = Object(FixtureNames.VectorGroup, Api.ObjectType.VectorGroup),
        ChartType = Api.ChartType.RunChart,
        DataSetToChart = Api.DatasetType.Magnitude,
        AuxDataSetToChart = Api.DatasetType.Magnitude,
        TemplateChartName = new Api.ChartName { Name = manifest.TemplateChartName },
        ShowInterface = false
    };

    private static Api.SetUserInterfaceProfileRequest Profile(FixtureManifest manifest) => new()
    {
        ProfileName = manifest.UiProfileName,
        ProfileFileName = new Api.FileReference { Path = manifest.UiProfileFile, EmbeddedFile = false }
    };

    private static Api.CollectionItemName CalloutView(FixtureManifest manifest) =>
        manifest.CalloutView is { } view
            ? new Api.CollectionItemName { CollectionName = view.Collection, ItemName = view.Name, ItemType = Api.ItemType.CalloutView }
            : new Api.CollectionItemName
            {
                CollectionName = FixtureNames.Collection,
                ItemName = FixtureNames.CalloutView,
                ItemType = Api.ItemType.CalloutView
            };

    private static Api.CollectionInstrumentId Instrument(ManifestInstrument instrument) =>
        new() { CollectionName = instrument.Collection, InstrumentId = instrument.InstrumentId };

    private static Api.CollectionObjectName Object(string name, Api.ObjectType type) =>
        new() { CollectionName = FixtureNames.Collection, ObjectName = name, ObjectType = type };

    private static Api.PointName Point(string group, string target) =>
        new() { CollectionName = FixtureNames.Collection, GroupName = group, TargetName = target };

    private static Api.Vector Vector(double x, double y, double z) => new() { X = x, Y = y, Z = z };

    private static Api.Transform Identity()
    {
        var transform = new Api.Transform();
        transform.Values.AddRange([1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1]);
        return transform;
    }

    /// <summary>Accumulates steps for one phase with the reviewed acceptance sets.</summary>
    private sealed class StepList(ProbePhase phase)
    {
        private readonly List<ProbeStep> _steps = [];

        public ProbePhase Phase { get; } = phase;

        public void Add(ProbeStep step) => _steps.Add(step);

        public void Guard(string id, string probe, string purpose, ProbeOperation operation, IMessage request) =>
            _steps.Add(new(id, probe, ProbeStepKind.Guard, Phase, purpose, operation, request, CommandVariant.Shipped, ProbeOutcomes.Succeeded));

        public void Setup(string id, string purpose, ProbeOperation operation, IMessage request) =>
            _steps.Add(new(id, "setup", ProbeStepKind.Setup, Phase, purpose, operation, request, CommandVariant.Shipped, ProbeOutcomes.Succeeded));

        // Wildcard selections may report an empty match as an MP failure; both are observations.
        public void Check(string id, string probe, string purpose, IMessage request, params ProbeHypothesis[] hypotheses) =>
            _steps.Add(new(id, probe, ProbeStepKind.Check, Phase, purpose, ProbeOperations.ObjectWildcardSelection, request,
                CommandVariant.Shipped, ProbeOutcomes.Succeeded | ProbeOutcomes.MpFailed)
            {
                Hypotheses = hypotheses
            });

        // Public probes and worker variants: every determinate SDK outcome is an observation.
        // An outcome whose completion is unknown is never accepted; it stops the session.
        public void Probe(string id, string probe, string purpose, ProbeOperation operation, IMessage request) =>
            _steps.Add(new(id, probe, ProbeStepKind.Probe, Phase, purpose, operation, request, CommandVariant.Shipped,
                ProbeOutcomes.AnyDeterminateSdkOutcome));

        public void Variant(string id, string probe, string purpose, ProbeOperation operation, IMessage request, CommandVariant variant) =>
            _steps.Add(new(id, probe, ProbeStepKind.Probe, Phase, purpose, operation, request, variant,
                ProbeOutcomes.AnyDeterminateSdkOutcome));

        // A worker control validates the fixture; anything but success stops the session.
        public void Control(string id, string probe, string purpose, ProbeOperation operation, IMessage request) =>
            _steps.Add(new(id, probe, ProbeStepKind.Probe, Phase, purpose, operation, request, CommandVariant.Shipped,
                ProbeOutcomes.Succeeded));

        public List<ProbeStep> ToList() => [.. _steps];
    }
}
