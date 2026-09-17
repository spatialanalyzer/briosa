using Briosa.Server.Operations;
using Briosa.Server.Operations.WaveA;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class LegacyTargetCompatibilityTests
{
    [Fact]
    public void RuntimeSurfaceSelectionRequiresSevenExplicitBooleanChoices()
    {
        var operation = MpOperationCatalog.Get("construction_operations.construct_objects_from_surface_faces_runtime_select");
        var request = new Api.ConstructObjectsFromSurfaceFacesRuntimeSelectRequest
        {
            ConstructPlanes = true,
            ConstructCylinders = false,
            ConstructSpheres = false,
            ConstructCones = false,
            ConstructLines = false,
            ConstructPoints = false,
            ConstructCircles = false
        };
        var command = operation.CreateCommand(request);
        Assert.Equal(
            ["Construct Planes?", "Construct Cylinders?", "Construct Spheres?", "Construct Cones?", "Construct Lines?", "Construct Points?", "Construct Circles?"],
            command.InputArguments.Select(argument => argument.Name));
        Assert.All(command.InputArguments, argument => Assert.Equal("SetBoolArg", argument.SdkBinding));
        Assert.True(command.InputArguments[0].BooleanValue);
        Assert.All(command.InputArguments.Skip(1), argument => Assert.False(argument.BooleanValue));
        request.ClearConstructCircles();
        Assert.Throws<ArgumentException>(() => operation.CreateCommand(request));
    }

    [Fact]
    public void DirectCadAccessRequiresAnExplicitCompatibilityChoiceIncludingFalse()
    {
        var operation = MpOperationCatalog.Get("file_operations.direct_cad_access");
        var request = new Api.DirectCadAccessRequest { CadFileName = new Api.FileReference { Path = "fixture.step" } };
        Assert.Throws<ArgumentException>(() => operation.CreateCommand(request));
        request.SurfaceCompatibilityMode = false;
        var command = operation.CreateCommand(request);
        Assert.False(Assert.Single(command.InputArguments, argument => argument.Name == "Surface Compatibility Mode").BooleanValue);
    }

    [Theory]
    [InlineData("file_operations.prepare_qdas_data_list")]
    [InlineData("file_operations.export_qdas_characteristics")]
    public void CapturedQdasTimestampsAreNotDefaults(string operationId)
    {
        var timestamp = Assert.Single(MpOperationCatalog.Get(operationId).Inputs,
            input => input.MpName == "K0004: Date Time Stamp");
        Assert.True(timestamp.Required);
        Assert.Equal("Required", timestamp.DefaultValue);
    }

    [Fact]
    public void LegacyLabelsDoNotInheritLaterSemantics()
    {
        var statistics = MpOperationCatalog.Get("relationship_operations.get_general_relationship_statistics");
        Assert.Equal("Max Deviation", statistics.Outputs[0].MpName);
        Assert.Equal("max_deviation", statistics.Outputs[0].FieldName);
        var targets = MpOperationCatalog.Get("instrument_operations.get_instrument_targets_and_mode_profiles");
        Assert.Equal("Instrument to set", Assert.Single(targets.Inputs).MpName);
    }

    [Theory]
    [InlineData("analysis_operations.get_cone_properties", "Cut Length from Apex")]
    [InlineData("analysis_operations.re_compute_calculated_items", "Refresh Filtered Cloud Data?")]
    [InlineData("analysis_operations.make_cylinder_fit_profile", "Constrain to Nominal Axis?")]
    [InlineData("relationship_operations.do_relationship_fit", "Enable Randomized Start")]
    [InlineData("relationship_operations.get_points_to_objects_relationship_statistics", "Avg Deviation")]
    [InlineData("gdt_operations.get_feature_check_reporting_options", "Only Create Failed Vectors?")]
    [InlineData("gdt_operations.set_feature_check_reporting_options", "Only Create Failed Vectors?")]
    [InlineData("construction_operations.construct_point_cloud_from_existing_clouds", "Set Cloud Point RGB from Voxels?")]
    [InlineData("file_operations.export_ascii_point_clouds", "Include Cloud Point Labeling?")]
    public void LaterArgumentsAreAbsentFromTheWorkerCommandContract(string operationId, string label)
    {
        var operation = MpOperationCatalog.Get(operationId);
        Assert.DoesNotContain(operation.Inputs.Concat(operation.Outputs), argument => argument.MpName == label);
    }

    [Theory]
    [InlineData("ScanWithinPerimeter")]
    [InlineData("EditScanPerimeterProfile")]
    [InlineData("ScanCadFaces")]
    [InlineData("GetInstrumentGroupAndTarget")]
    public void UnavailableOperationsAreAbsentFromProtocolAndDiscovery(string rpc)
    {
        Assert.DoesNotContain(Api.InstrumentOperations.Descriptor.Methods, method => method.Name == rpc);
        Assert.DoesNotContain(SpatialAnalyzerApi.Operations, operation => operation.Rpc == rpc);
    }

    [Fact]
    public void SystemUserNameIsDistinctAndLaterEnumNumbersAreRejected()
    {
        var operation = MpOperationCatalog.Get("construction_operations.make_system_string");
        var command = operation.CreateCommand(new Api.MakeSystemStringRequest { StringContent = Api.SystemString.UserName });
        Assert.Equal(10, command.InputArguments[0].SpecializedEnumValue!.Value);
        foreach (var number in new[] { 12, 13, 14 })
        {
            Assert.Throws<ArgumentException>(() => operation.CreateCommand(new Api.MakeSystemStringRequest { StringContent = (Api.SystemString)number }));
        }
        Assert.False(Enum.IsDefined((Api.ObjectType)5));
        Assert.False(Enum.IsDefined((Api.ItemType)10));
        Assert.False(Enum.IsDefined((WorkerObjectTypeValue)5));
        Assert.False(Enum.IsDefined((WorkerItemTypeValue)10));
    }

    [Fact]
    public void LaterObjectAndItemTypesAreRejectedBeforeWorkerAdmission()
    {
        var project = MpOperationCatalog.Get("instrument_operations.project_objects");
        var request = new Api.ProjectObjectsRequest
        {
            Instrument = new Api.CollectionInstrumentId { CollectionName = "fixture", InstrumentId = 1 }
        };
        request.ObjectsToProject.Add(new Api.CollectionObjectName
        {
            CollectionName = "fixture", ObjectName = "cloud", ObjectType = (Api.ObjectType)5
        });
        Assert.Throws<ArgumentException>(() => project.CreateCommand(request));

        var statistics = MpOperationCatalog.Get("relationship_operations.get_general_relationship_statistics");
        Assert.Throws<ArgumentException>(() => statistics.CreateCommand(new Api.GetGeneralRelationshipStatisticsRequest
        {
            RelationshipName = new Api.CollectionItemName
            {
                CollectionName = "fixture", ItemName = "relationship", ItemType = (Api.ItemType)10
            }
        }));
    }

    [Fact]
    public void CribSheetAndProjectionUseCompleteExactBindingsWithoutWorkflowOwnership()
    {
        var instrument = new Api.CollectionInstrumentId { CollectionName = "fixture", InstrumentId = 1 };
        var crib = MpOperationCatalog.Get("instrument_operations.run_crib_sheet");
        var cribCommand = crib.CreateCommand(new Api.RunCribSheetRequest
        {
            Collection = new Api.CollectionName { Name = "fixture" },
            CribSheetName = "reviewed-crib",
            Instrument = instrument
        });
        Assert.Equal(["Collection Name", "Crib Sheet Name", "Instrument ID"], cribCommand.InputArguments.Select(argument => argument.Name));
        Assert.Equal(["SetCollectionNameArg", "SetStringArg", "SetColInstIdArg"], cribCommand.InputArguments.Select(argument => argument.SdkBinding));

        var project = MpOperationCatalog.Get("instrument_operations.project_objects");
        var request = new Api.ProjectObjectsRequest { Instrument = instrument };
        Assert.Throws<ArgumentException>(() => project.CreateCommand(request));
        request.ObjectsToProject.Add(new Api.CollectionObjectName { CollectionName = "fixture", ObjectName = "line", ObjectType = Api.ObjectType.Line });
        var projectCommand = project.CreateCommand(request);
        Assert.Equal(["SetColInstIdArg", "SetCollectionObjectNameRefListArg"], projectCommand.InputArguments.Select(argument => argument.SdkBinding));
        Assert.Equal(WorkerObjectTypeValue.Line, Assert.Single(projectCommand.InputArguments[1].CollectionObjectNameListValue!.Values).ObjectType);

        var stop = MpOperationCatalog.Get("instrument_operations.stop_projection");
        var stopCommand = stop.CreateCommand(new Api.StopProjectionRequest { Instrument = instrument });
        Assert.Equal("Instrument ID", Assert.Single(stopCommand.InputArguments).Name);
        foreach (var operation in new[] { crib, project, stop })
        {
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.Descriptor.ReplaySafety);
            Assert.Contains("at-risk-no-runtime-validation", operation.Descriptor.RiskFlags);
            Assert.Empty(operation.Outputs);
        }
    }
}
