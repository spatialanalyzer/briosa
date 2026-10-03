using System.Text.Json.Nodes;
using Briosa.Server.Operations;
using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Operations.CloudAndMeshOperations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Operations.ReportingOperations;
using Briosa.Server.Operations.ViewControl;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

/// <summary>
/// Pins SDK step text and argument names restored after the #227 regressions (#236) to the
/// committed exact-target inventory: SDK setter argument names and SDK-evidence MP steps, never
/// the documentation text. Also pins the step text corrected under #271.
/// </summary>
public sealed class ExactSdkBindingRegressionTests
{
    private static readonly Lazy<JsonArray> InventoryCommands = new(LoadInventoryCommands);

    [Theory]
    [InlineData("analysis_operations.fit_geometry_to_point_group", 1, "Group To Fit")]
    [InlineData("analysis_operations.fit_geometry_to_point_group_projected_to_plane", 1, "Group To Fit")]
    [InlineData("reporting_operations.add_datums_to_report_bar", 0, "Datum(s)")]
    [InlineData("reporting_operations.add_events_to_report_bar", 0, "Event(s)")]
    [InlineData("reporting_operations.add_feature_checks_to_report_bar", 0, "Feature Check(s)")]
    [InlineData("reporting_operations.add_objects_to_report_bar", 0, "Object(s)")]
    [InlineData("reporting_operations.add_relationships_to_report_bar", 0, "Relationship(s)")]
    [InlineData("reporting_operations.create_chart_from_vector_group", 5, "Template Chart Name (optional)")]
    [InlineData("view_control.set_toolkit_visibility", 0, "Show Toolkit?")]
    [InlineData("view_control.show_hide_instrument_interface", 0, "Instrument's ID")]
    [InlineData("view_control.show_items_in_tree", 0, "Collapse all other Items?")]
    [InlineData("instrument_operations.set_xyz_instrument_uncertainties", 3, "Z Uncertainty)")]
    [InlineData("file_operations.export_vector_container_to_ascii_file", 0, "Ascii File Path")]
    public void RestoredArgumentNamesMatchTheInventorySdkSetter(string operationId, int sdkOrder, string expectedName)
    {
        var command = CreateLabelCommand(operationId);
        var argument = command.InputArguments[sdkOrder];
        var setter = InventoryArgument(command.StepName, sdkOrder)["sdk_binding"]!["setter"]!;

        Assert.Equal("available", setter["status"]!.GetValue<string>());
        Assert.Equal(expectedName, setter["argument_name"]!.GetValue<string>());
        Assert.Equal(expectedName, argument.Name);
        Assert.Equal(setter["method"]!.GetValue<string>(), argument.SdkBinding);
    }

    [Theory]
    [InlineData("analysis_operations.angle_between_two_planes_normals", "Angle Between Two Planes' normals")]
    [InlineData("analysis_operations.compute_group_to_group_orientation_rx_ry_rz",
        "Compute Group to Group Orientation (Rx,Ry,Rz)")]
    [InlineData("file_operations.export_iges_file_entire_model", "Export IGES File  - Entire Model")]
    [InlineData("file_operations.export_vda_fs_file_entire_model", "Export VDA/FS File  - Entire Model")]
    [InlineData("file_operations.import_file_as_embedded_file", "Import File as Embedded File")]
    [InlineData("file_operations.import_mp_file_as_embedded_mp", "Import MP File as Embedded MP")]
    [InlineData("file_operations.import_qdas_catalog_file", "Import QDAS Catalog File")]
    [InlineData("file_operations.save_as", "Save As...")]
    [InlineData("relationship_operations.make_pipe_relationship_cut", "Make Pipe Relationship Cut")]
    [InlineData("reporting_operations.save_chart_to_jpeg_file", "Save Chart to JPeg file")]
    [InlineData("utility_operations.set_wild_card_asterisk_mode", "Set WildCard Asterisk Mode")]
    [InlineData("vector_operations.add_a_vector_to_vector_name_ref_list", "Add a Vector To Vector Name Ref List")]
    [InlineData("view_control.define_point_of_view", "Define point of view")]
    [InlineData("view_control.get_point_of_view_parameters", "Get point of view parameters")]
    [InlineData("view_control.save_point_of_view", "Save point of view")]
    [InlineData("view_control.set_mp_window_state", "Set MP's Window State")]
    [InlineData("view_control.set_point_of_view", "Set point of view")]
    [InlineData("view_control.set_point_of_view_from_frame", "Set Point of View from Frame")]
    [InlineData("view_control.set_point_of_view_from_instrument_updates", "Set Point of View from Instrument Updates")]
    [InlineData("view_control.set_sa_window_pos", "Set SA's Window Pos")]
    [InlineData("view_control.set_sa_window_size", "Set SA's Window Size")]
    [InlineData("view_control.set_sa_window_state", "Set SA's Window State")]
    [InlineData("view_control.show_hide_by_object_type", "Show / Hide by Object Type")]
    [InlineData("view_control.show_hide_callout_view", "Show / Hide Callout View")]
    [InlineData("view_control.show_hide_points", "Show / Hide Points")]
    public void CorrectedStepsMatchTheInventorySdkEvidence(string operationId, string expectedStep)
    {
        var descriptor = Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == operationId);
        var inventoryCommand = Assert.Single(InventoryCommands.Value, candidate => HasSdkStep(candidate!, expectedStep))!;
        var sdkSteps = inventoryCommand["sdk_evidence"]!.AsArray()
            .Select(evidence => evidence!["mp_step"]!.GetValue<string>())
            .Distinct(StringComparer.Ordinal);

        Assert.Equal(expectedStep, Assert.Single(sdkSteps));
        Assert.Equal(expectedStep, descriptor.MpStep);
    }

    [Fact]
    public void DeleteCloudPointsByXYZRangeUsesTheExactSdkStep()
    {
        var command = DeleteCloudPointsByXYZRangeOperation.CreateCommand(new() { CloudNames = { Cloud() } });

        Assert.Equal("Delete Cloud Points by X Y Z Range", DeleteCloudPointsByXYZRangeOperation.Descriptor.MpStep);
        Assert.Equal(DeleteCloudPointsByXYZRangeOperation.Descriptor.MpStep, command.StepName);
        Assert.Single(InventoryCommands.Value, candidate => HasSdkStep(candidate!, command.StepName));
    }

    [Fact]
    public void DeleteCloudPointsByXYZRangeOmitsEveryAbsentBound()
    {
        var command = DeleteCloudPointsByXYZRangeOperation.CreateCommand(new() { CloudNames = { Cloud() } });

        Assert.Equal(["Cloud Names", "Delete Inside"], command.InputArguments.Select(argument => argument.Name));
        Assert.Equal("SetCollectionObjectNameRefListArg", command.InputArguments[0].SdkBinding);
        Assert.Equal("SetBoolArg", command.InputArguments[1].SdkBinding);
        Assert.False(command.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => DeleteCloudPointsByXYZRangeOperation.CreateCommand(new()));
    }

    [Fact]
    public void DeleteCloudPointsByXYZRangeSendsEveryPresentBoundInSdkOrder()
    {
        var command = DeleteCloudPointsByXYZRangeOperation.CreateCommand(new()
        {
            CloudNames = { Cloud() },
            XMin = -1.5,
            XMax = 1.5,
            YMin = -2.5,
            YMax = 2.5,
            ZMin = -3.5,
            ZMax = 3.5,
            DeleteInside = true
        });
        var inventoryNames = InventoryCommands.Value
            .Single(candidate => HasSdkStep(candidate!, command.StepName))!["arguments"]!.AsArray()
            .OrderBy(argument => argument!["sdk_order"]!.GetValue<int>())
            .Select(argument => argument!["sdk_binding"]!["setter"]!["argument_name"]!.GetValue<string>());

        Assert.Equal(inventoryNames, command.InputArguments.Select(argument => argument.Name));
        Assert.Equal(
            ["Cloud Names", "X Min", "X Max", "Y Min", "Y Max", "Z Min", "Z Max", "Delete Inside"],
            command.InputArguments.Select(argument => argument.Name));
        var bounds = command.InputArguments.Skip(1).Take(6).ToArray();
        Assert.All(bounds, argument =>
        {
            Assert.Equal("SetDoubleArg", argument.SdkBinding);
            Assert.Equal(WorkerMpValueKind.FloatingPoint, argument.Kind);
        });
        Assert.Equal([-1.5, 1.5, -2.5, 2.5, -3.5, 3.5],
            bounds.Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));
        Assert.True(command.InputArguments[7].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public void DeleteCloudPointsByXYZRangeSendsOnlyThePresentSubsetIncludingExplicitZero()
    {
        var command = DeleteCloudPointsByXYZRangeOperation.CreateCommand(new()
        {
            CloudNames = { Cloud() },
            XMin = 1,
            YMin = 0,
            ZMax = 5
        });

        Assert.Equal(["Cloud Names", "X Min", "Y Min", "Z Max", "Delete Inside"],
            command.InputArguments.Select(argument => argument.Name));
        Assert.Equal([1d, 0d, 5d],
            command.InputArguments.Skip(1).Take(3).Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));
        Assert.False(command.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
    }

    private static WorkerMpCommand CreateLabelCommand(string operationId) => operationId switch
    {
        "analysis_operations.fit_geometry_to_point_group" => FitGeometryToPointGroupOperation.CreateCommand(new()
        {
            GeometryType = Api.GeometryType.Circle,
            GroupToFit = Object("Group"),
            ResultingObjectName = Object("Fit"),
            StartingConditionGeometry = Object("Seed")
        }),
        "analysis_operations.fit_geometry_to_point_group_projected_to_plane" =>
            FitGeometryToPointGroupProjectedToPlaneOperation.CreateCommand(new()
            {
                GeometryType = Api.GeometryType.Circle,
                GroupToFit = Object("Group"),
                PlaneName = Object("Plane"),
                ResultingObjectName = Object("Fit"),
                StartingConditionGeometry = Object("Seed")
            }),
        "reporting_operations.add_datums_to_report_bar" =>
            AddDatumsToReportBarOperation.CreateCommand(new() { Datums = { Object("Datum") } }),
        "reporting_operations.add_events_to_report_bar" =>
            AddEventsToReportBarOperation.CreateCommand(new() { Events = { Item("Event") } }),
        "reporting_operations.add_feature_checks_to_report_bar" =>
            AddFeatureChecksToReportBarOperation.CreateCommand(new() { FeatureChecks = { Item("Check") } }),
        "reporting_operations.add_objects_to_report_bar" =>
            AddObjectsToReportBarOperation.CreateCommand(new() { Objects = { Object("Plane") } }),
        "reporting_operations.add_relationships_to_report_bar" =>
            AddRelationshipsToReportBarOperation.CreateCommand(new() { Relationships = { Item("Alignment") } }),
        "reporting_operations.create_chart_from_vector_group" => CreateChartFromVectorGroupOperation.CreateCommand(new()
        {
            NewChartName = new Api.ChartName { Name = "Run" },
            VectorGroupName = Object("Vectors"),
            ChartType = Api.ChartType.RunChart,
            DataSetToChart = Api.DatasetType.X,
            AuxDataSetToChart = Api.DatasetType.Magnitude,
            TemplateChartName = new Api.ChartName { Name = "Template" }
        }),
        "view_control.set_toolkit_visibility" => SetToolkitVisibilityOperation.CreateCommand(new()),
        "view_control.show_hide_instrument_interface" =>
            ShowHideInstrumentInterfaceOperation.CreateCommand(new() { InstrumentId = Instrument() }),
        "view_control.show_items_in_tree" => ShowItemsInTreeOperation.CreateCommand(new()
        {
            Points = { new Api.PointName { CollectionName = "Parts", GroupName = "Targets", TargetName = "P1" } },
            Objects = { Object("Plane") },
            Instruments = { Instrument() },
            FeatureChecks = { Item("Check") },
            Datums = { Object("Datum") },
            Collections = { "Parts" }
        }),
        "instrument_operations.set_xyz_instrument_uncertainties" =>
            SetXyzInstrumentUncertaintiesOperation.CreateCommand(new() { Instrument = Instrument() }),
        "file_operations.export_vector_container_to_ascii_file" => ExportVectorContainerToAsciiFileOperation.CreateCommand(new()
        {
            AsciiFilePath = new Api.FileReference { Path = "vectors.txt" },
            VectorGroupsToExport = { new Api.CollectionVectorGroupName { CollectionName = "Parts", VectorGroupName = "Vectors" } },
            VectorNameFormat = Api.ExportVectorNameFormat.Vector
        }),
        _ => throw new ArgumentOutOfRangeException(nameof(operationId), operationId, "No request fixture is defined.")
    };

    private static JsonNode InventoryArgument(string mpStep, int sdkOrder)
    {
        var inventoryCommand = InventoryCommands.Value.Single(candidate => HasSdkStep(candidate!, mpStep))!;
        return inventoryCommand["arguments"]!.AsArray()
            .Single(argument => argument!["sdk_order"]?.GetValue<int?>() == sdkOrder)!;
    }

    private static bool HasSdkStep(JsonNode inventoryCommand, string mpStep) =>
        inventoryCommand["sdk_evidence"]!.AsArray()
            .Any(evidence => string.Equals(evidence!["mp_step"]!.GetValue<string>(), mpStep, StringComparison.Ordinal));

    private static JsonArray LoadInventoryCommands()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Briosa.slnx")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        var path = Path.Combine(root.FullName, "inventory", "sa", SpatialAnalyzerApi.TargetVersion, "inventory.json");
        return JsonNode.Parse(File.ReadAllText(path))!["commands"]!.AsArray();
    }

    private static Api.CollectionObjectName Cloud() => new()
    {
        CollectionName = "Scans",
        ObjectName = "CloudA"
    };

    private static Api.CollectionObjectName Object(string name) => new()
    {
        CollectionName = "Parts",
        ObjectName = name
    };

    private static Api.CollectionItemName Item(string name) => new()
    {
        CollectionName = "Parts",
        ItemName = name
    };

    private static Api.CollectionInstrumentId Instrument() => new()
    {
        CollectionName = "Parts",
        InstrumentId = 3
    };
}
