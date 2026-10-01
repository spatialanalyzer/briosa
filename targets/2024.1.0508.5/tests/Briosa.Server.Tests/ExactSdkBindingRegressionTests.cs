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
/// Pins SDK step text and argument names restored after the #227 regressions (#236). This target
/// has no committed exact-target inventory, so each literal cites the reviewed v0.8.0 catalog
/// under targets/2024.1.0508.5/src/Briosa.Server/Operations (git show v0.8.0:&lt;path&gt;).
/// </summary>
public sealed class ExactSdkBindingRegressionTests
{
    // Provenance (v0.8.0): WaveA/WaveAOperationCatalog.cs lines 136, 154, 1837, 3520, 3532, 3544, 3572,
    // 3596, 3695, 5945, 6052 and 6131; WaveB/InstrumentWaveBOperationCatalog.cs line 1352.
    [Theory]
    [InlineData("analysis_operations.fit_geometry_to_point_group", 1, "Group To Fit", "SetCollectionObjectNameArg2")]
    [InlineData("analysis_operations.fit_geometry_to_point_group_projected_to_plane", 1, "Group To Fit", "SetCollectionObjectNameArg2")]
    [InlineData("reporting_operations.add_datums_to_report_bar", 0, "Datum(s)", "SetCollectionObjectNameRefListArg")]
    [InlineData("reporting_operations.add_events_to_report_bar", 0, "Event(s)", "SetCollectionObjectNameRefListArg")]
    [InlineData("reporting_operations.add_feature_checks_to_report_bar", 0, "Feature Check(s)", "SetCollectionObjectNameRefListArg")]
    [InlineData("reporting_operations.add_objects_to_report_bar", 0, "Object(s)", "SetCollectionObjectNameRefListArg")]
    [InlineData("reporting_operations.add_relationships_to_report_bar", 0, "Relationship(s)", "SetCollectionObjectNameRefListArg")]
    [InlineData("reporting_operations.create_chart_from_vector_group", 5, "Template Chart Name (optional)", "SetChartNameArg")]
    [InlineData("view_control.set_toolkit_visibility", 0, "Show Toolkit?", "SetBoolArg")]
    [InlineData("view_control.show_hide_instrument_interface", 0, "Instrument's ID", "SetColInstIdArg")]
    [InlineData("view_control.show_items_in_tree", 0, "Collapse all other Items?", "SetBoolArg")]
    [InlineData("instrument_operations.set_xyz_instrument_uncertainties", 3, "Z Uncertainty)", "SetDoubleArg")]
    [InlineData("file_operations.export_vector_container_to_ascii_file", 0, "Ascii File Path", "SetFilePathArg")]
    public void RestoredArgumentNamesMatchTheReviewedCatalog(
        string operationId, int sdkOrder, string expectedName, string expectedBinding)
    {
        var argument = CreateLabelCommand(operationId).InputArguments[sdkOrder];

        Assert.Equal(expectedName, argument.Name);
        Assert.Equal(expectedBinding, argument.SdkBinding);
    }

    // Provenance (v0.8.0): WaveB/CloudAndMeshOperationCatalog.cs lines 221-233 (step text, SDK order,
    // OmittedDouble bounds) and 264-266 (OmittedDouble sets OmitWhenAbsent).
    [Fact]
    public void DeleteCloudPointsByXYZRangeUsesTheExactSdkStep()
    {
        var command = DeleteCloudPointsByXYZRangeOperation.CreateCommand(new() { CloudNames = { Cloud() } });

        Assert.Equal("Delete Cloud Points by X Y Z Range", DeleteCloudPointsByXYZRangeOperation.Descriptor.MpStep);
        Assert.Equal(DeleteCloudPointsByXYZRangeOperation.Descriptor.MpStep, command.StepName);
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
