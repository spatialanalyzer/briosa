using Briosa.Server.Operations;
using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class LegacyTargetCompatibilityTests
{
    [Fact]
    public void DirectCadAccessRequiresAnExplicitCompatibilityChoiceIncludingFalse()
    {
        var request = new Api.DirectCadAccessRequest { CadFileName = new Api.FileReference { Path = "fixture.step" } };
        Assert.Throws<ArgumentException>(() => DirectCadAccessOperation.CreateCommand(request));
        request.SurfaceCompatibilityMode = false;
        var command = DirectCadAccessOperation.CreateCommand(request);
        Assert.False(((Assert.Single(command.InputArguments, argument => argument.Name == "Surface Compatibility Mode").Value as WorkerBooleanValue)?.Value));
    }

    [Fact]
    public void CapturedQdasTimestampsAreNotDefaults()
    {
        Assert.Contains("k0004_date_time_stamp", Assert.Throws<ArgumentException>(() =>
            PrepareQdasDataListOperation.CreateCommand(new())).Message, StringComparison.Ordinal);
        Assert.Contains("k0004_date_time_stamp", Assert.Throws<ArgumentException>(() =>
            ExportQdasCharacteristicsOperation.CreateCommand(new())).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacyLabelsDoNotInheritLaterSemantics()
    {
        Assert.Equal("Max Deviation", GetGeneralRelationshipStatisticsOperation.OutputContracts[0].ArgumentName);
        Assert.Equal("max_deviation", GetGeneralRelationshipStatisticsOperation.OutputContracts[0].FieldName);
        var targets = GetInstrumentTargetsAndModeProfilesOperation.CreateCommand(new()
        {
            Instrument = new Api.CollectionInstrumentId { CollectionName = "fixture", InstrumentId = 1 }
        });
        Assert.Equal("Instrument to set", Assert.Single(targets.InputArguments).Name);
    }

    [Fact]
    public void LaterArgumentsAreAbsentFromTheWorkerCommandContract()
    {
        var objectName = new Api.CollectionObjectName { CollectionName = "fixture", ObjectName = "item" };
        var relationship = new Api.CollectionItemName { CollectionName = "fixture", ItemName = "relationship" };
        var featureCheck = new Api.CollectionItemName { CollectionName = "fixture", ItemName = "check" };
        (WorkerMpCommand Command, string AbsentLabel)[] cases =
        [
            (GetConePropertiesOperation.CreateCommand(new() { ConeName = objectName }), "Cut Length from Apex"),
            (ReComputeCalculatedItemsOperation.CreateCommand(new()), "Refresh Filtered Cloud Data?"),
            (MakeCylinderFitProfileOperation.CreateCommand(new()), "Constrain to Nominal Axis?"),
            (GetPointsToObjectsRelationshipStatisticsOperation.CreateCommand(new() { RelationshipName = relationship }), "Avg Deviation"),
            (GetFeatureCheckReportingOptionsOperation.CreateCommand(new() { FeatureCheck = featureCheck }), "Only Create Failed Vectors?"),
            (SetFeatureCheckReportingOptionsOperation.CreateCommand(new() { FeatureCheck = featureCheck }), "Only Create Failed Vectors?"),
            (ConstructPointCloudFromExistingCloudsOperation.CreateCommand(new()
            {
                ExistingPointCloudList = { objectName }, NewCloudName = objectName
            }), "Set Cloud Point RGB from Voxels?"),
            (ExportAsciiPointCloudsOperation.CreateCommand(new()
            {
                AsciiFilePath = new Api.FileReference { Path = "fixture.csv" },
                PointCloudList = { objectName }, DataDelimiter = Api.ExportDataDelimeterType.Comma
            }), "Include Cloud Point Labeling?")
        ];

        foreach (var (command, absentLabel) in cases)
        {
            Assert.DoesNotContain(command.InputArguments.Select(argument => argument.Name)
                .Concat(command.OutputArguments.Select(argument => argument.Name)),
                name => name == absentLabel);
        }
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
        var command = MakeSystemStringOperation.CreateCommand(new Api.MakeSystemStringRequest { StringContent = Api.SystemString.UserName });
        Assert.Equal(WorkerSystemStringValue.UserName, command.InputArguments[0].RequireValue<WorkerChoiceValue<WorkerSystemStringValue>>().Value);
        foreach (var number in new[] { 12, 13, 14 })
        {
            Assert.Throws<ArgumentException>(() => MakeSystemStringOperation.CreateCommand(new Api.MakeSystemStringRequest { StringContent = (Api.SystemString)number }));
        }
        Assert.False(Enum.IsDefined((Api.ObjectType)5));
        Assert.False(Enum.IsDefined((Api.ItemType)10));
        Assert.False(Enum.IsDefined((WorkerObjectTypeValue)5));
        Assert.False(Enum.IsDefined((WorkerItemTypeValue)10));
    }

    [Fact]
    public void LaterObjectAndItemTypesAreRejectedBeforeWorkerAdmission()
    {
        var request = new Api.ProjectObjectsRequest
        {
            Instrument = new Api.CollectionInstrumentId { CollectionName = "fixture", InstrumentId = 1 }
        };
        request.ObjectsToProject.Add(new Api.CollectionObjectName
        {
            CollectionName = "fixture",
            ObjectName = "cloud",
            ObjectType = (Api.ObjectType)5
        });
        Assert.Throws<ArgumentException>(() => ProjectObjectsOperation.CreateCommand(request));

        Assert.Throws<ArgumentException>(() => GetGeneralRelationshipStatisticsOperation.CreateCommand(new Api.GetGeneralRelationshipStatisticsRequest
        {
            RelationshipName = new Api.CollectionItemName
            {
                CollectionName = "fixture",
                ItemName = "relationship",
                ItemType = (Api.ItemType)10
            }
        }));
    }

    [Fact]
    public void CribSheetAndProjectionUseCompleteExactBindingsWithoutWorkflowOwnership()
    {
        var instrument = new Api.CollectionInstrumentId { CollectionName = "fixture", InstrumentId = 1 };
        var cribCommand = RunCribSheetOperation.CreateCommand(new Api.RunCribSheetRequest
        {
            Collection = new Api.CollectionName { Name = "fixture" },
            CribSheetName = "reviewed-crib",
            Instrument = instrument
        });
        Assert.Equal(["Collection Name", "Crib Sheet Name", "Instrument ID"], cribCommand.InputArguments.Select(argument => argument.Name));
        Assert.Equal(["SetCollectionNameArg", "SetStringArg", "SetColInstIdArg"], cribCommand.InputArguments.Select(argument => argument.SdkBinding));

        var request = new Api.ProjectObjectsRequest { Instrument = instrument };
        Assert.Throws<ArgumentException>(() => ProjectObjectsOperation.CreateCommand(request));
        request.ObjectsToProject.Add(new Api.CollectionObjectName { CollectionName = "fixture", ObjectName = "line", ObjectType = Api.ObjectType.Line });
        var projectCommand = ProjectObjectsOperation.CreateCommand(request);
        Assert.Equal(["SetColInstIdArg", "SetCollectionObjectNameRefListArg"], projectCommand.InputArguments.Select(argument => argument.SdkBinding));
        Assert.Equal(WorkerObjectTypeValue.Line, Assert.Single((projectCommand.InputArguments[1].Value as WorkerCollectionObjectNameListValue)!.Values).ObjectType);

        var stopCommand = StopProjectionOperation.CreateCommand(new Api.StopProjectionRequest { Instrument = instrument });
        Assert.Equal("Instrument ID", Assert.Single(stopCommand.InputArguments).Name);
        foreach (var descriptor in new[] { RunCribSheetOperation.Descriptor, ProjectObjectsOperation.Descriptor, StopProjectionOperation.Descriptor })
        {
            Assert.Equal(Api.ReplaySafety.Unsafe, descriptor.ReplaySafety);
            Assert.Contains("at-risk-no-runtime-validation", descriptor.RiskFlags);
        }
        Assert.Empty(RunCribSheetOperation.OutputContracts);
        Assert.Empty(ProjectObjectsOperation.OutputContracts);
        Assert.Empty(StopProjectionOperation.OutputContracts);
    }
}
