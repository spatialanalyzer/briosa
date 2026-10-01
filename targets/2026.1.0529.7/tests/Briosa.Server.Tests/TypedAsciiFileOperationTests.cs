using Briosa.Server.Operations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedAsciiFileOperationTests
{
    private static readonly string[] Ids =
    [
        "file_operations.export_ascii_frame_set", "file_operations.export_ascii_frames",
        "file_operations.export_ascii_point_clouds", "file_operations.export_ascii_point_set",
        "file_operations.export_ascii_points", "file_operations.export_vector_container_to_ascii_file",
        "file_operations.import_ascii_predefined_formats", "file_operations.import_ascii_predefined_frame_set_formats"
    ];

    [Fact]
    public void AsciiOperationsAreRegisteredAndHaveExactTargetRiskMetadata()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Equal(Api.ReplaySafety.Unsafe, ExportAsciiPointCloudsOperation.Descriptor.ReplaySafety);
        Assert.Equal(["fixture_validation_pending"], ExportAsciiPointCloudsOperation.Descriptor.RiskFlags);
        Assert.Equal("Export Vector Container to ASCII File", ExportVectorContainerToAsciiFileOperation.Descriptor.MpStep);
    }

    [Fact]
    public void AsciiMappingsPreserveBindingsRequiredValuesAndDefaults()
    {
        var file = new Api.FileReference { Path = "points.txt" };
        var objectName = new Api.CollectionObjectName { CollectionName = "collection", ObjectName = "frame" };
        var frameSet = ExportAsciiFrameSetOperation.CreateCommand(new()
        {
            AsciiFilePath = file,
            FrameSetContainer = objectName,
            DataDelimiter = Api.ExportDataDelimeterType.Tab,
            FileFormat = Api.AsciiFileFormat.FrameNameXYZRxRyRzTimestamp
        });
        Assert.Equal("SetFilePathArg", frameSet.InputArguments[0].SdkBinding);
        Assert.Equal("SetCollectionObjectNameArg2", frameSet.InputArguments[1].SdkBinding);
        Assert.Equal(WorkerExportDataDelimiterTypeValue.Tab,
            frameSet.InputArguments[2].RequireValue<WorkerChoiceValue<WorkerExportDataDelimiterTypeValue>>().Value);
        Assert.Equal(WorkerAsciiImportFileFormatValue.FrameNameXyzRxRyRzTimestamp,
            frameSet.InputArguments[3].RequireValue<WorkerChoiceValue<WorkerAsciiImportFileFormatValue>>().Value);
        Assert.Equal(6, frameSet.InputArguments[5].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(frameSet.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(frameSet.InputArguments[6].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => ExportAsciiFrameSetOperation.CreateCommand(new()));

        var frames = ExportAsciiFramesOperation.CreateCommand(new()
        {
            AsciiFilePath = file,
            ObjectList = { objectName }
        });
        Assert.Equal("Fixed XYZ", frames.InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.False(frames.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => ExportAsciiFramesOperation.CreateCommand(new() { AsciiFilePath = file }));

        var clouds = ExportAsciiPointCloudsOperation.CreateCommand(new()
        {
            AsciiFilePath = file,
            PointCloudList = { objectName },
            DataDelimiter = Api.ExportDataDelimeterType.Comma,
            IncludeCloudPointLabeling = true,
            IncludeScanDirectionVector = true
        });
        Assert.Equal(7, clouds.InputArguments.Count);
        Assert.True(clouds.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(clouds.InputArguments[6].RequireValue<WorkerBooleanValue>().Value);

        var pointSet = ExportAsciiPointSetOperation.CreateCommand(new()
        {
            AsciiFilePath = file,
            PointSetContainer = objectName,
            DataDelimiter = Api.ExportDataDelimeterType.Space,
            TargetNameFormat = Api.ExportTargetNameFormat.GroupTarget,
            DesiredCoordinateSystem = Api.CoordinateSystemType.Cartesian
        });
        Assert.Equal(13, pointSet.InputArguments.Count);
        Assert.Equal("SetCoordinateSystemTypeArg", pointSet.InputArguments[4].SdkBinding);
        Assert.False(pointSet.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(6, pointSet.InputArguments[11].RequireValue<WorkerIntegerValue>().Value);

        var points = ExportAsciiPointsOperation.CreateCommand(new()
        {
            AsciiFilePath = file,
            GroupNamesToExport = { new Api.CollectionGroupName { CollectionName = "collection", GroupName = "group" } },
            DataDelimiter = Api.ExportDataDelimeterType.Comma,
            TargetNameFormat = Api.ExportTargetNameFormat.Target,
            DesiredCoordinateSystem = Api.CoordinateSystemType.Polar
        });
        Assert.Equal("SetCollectionGroupNameRefListArg", points.InputArguments[1].SdkBinding);
        Assert.Equal(new WorkerCollectionGroupNameValue("collection", "group"),
            Assert.Single(points.InputArguments[1].RequireValue<WorkerCollectionGroupNameListValue>().Values));
        Assert.Equal(18, points.InputArguments.Count);
        Assert.Equal(6, points.InputArguments[16].RequireValue<WorkerIntegerValue>().Value);
        Assert.Throws<ArgumentException>(() => ExportAsciiPointsOperation.CreateCommand(new() { AsciiFilePath = file }));

        var vectorFile = ExportVectorContainerToAsciiFileOperation.CreateCommand(new()
        {
            AsciiFilePath = file,
            VectorGroupsToExport = { new Api.CollectionVectorGroupName { CollectionName = "collection", VectorGroupName = "vectors" } },
            VectorNameFormat = Api.ExportVectorNameFormat.Vector
        });
        Assert.Equal("Ascii File Path", vectorFile.InputArguments[0].Name);
        Assert.True(vectorFile.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(vectorFile.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(vectorFile.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);

        var import = ImportAsciiPredefinedFormatsOperation.CreateCommand(new()
        {
            AsciiFilePath = file,
            FileFormat = Api.AsciiFileFormat.XYZ,
            GroupName = objectName
        });
        Assert.Equal(WorkerDistanceUnitValue.Inches,
            import.InputArguments[2].RequireValue<WorkerDistanceUnitChoice>().Value);
        Assert.Equal(WorkerAngularUnitValue.Degrees,
            import.InputArguments[3].RequireValue<WorkerAngularUnitChoice>().Value);
        Assert.False(import.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(import.InputArguments[6].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(import.InputArguments[7].RequireValue<WorkerBooleanValue>().Value);

        var importFrameSet = ImportAsciiPredefinedFrameSetFormatsOperation.CreateCommand(new()
        {
            AsciiFilePath = file,
            FileFormat = Api.AsciiFileFormat.TransformationMatrixTimestamp,
            FrameSetContainerName = objectName
        });
        Assert.True(importFrameSet.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientRoutesAsciiCallsToTypedWorkerCommands()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<FileOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.FileOperations.FileOperationsClient(host.Channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var file = new Api.FileReference { Path = "points.txt" };
        var objectName = new Api.CollectionObjectName { CollectionName = "collection", ObjectName = "item" };

        await client.ExportAsciiFrameSetAsync(new()
        {
            AsciiFilePath = file, FrameSetContainer = objectName,
            DataDelimiter = Api.ExportDataDelimeterType.Comma, FileFormat = Api.AsciiFileFormat.XYZ
        }, options);
        await client.ExportAsciiFramesAsync(new() { AsciiFilePath = file, ObjectList = { objectName } }, options);
        await client.ExportAsciiPointCloudsAsync(new()
        {
            AsciiFilePath = file, PointCloudList = { objectName }, DataDelimiter = Api.ExportDataDelimeterType.Space
        }, options);
        await client.ExportAsciiPointSetAsync(new()
        {
            AsciiFilePath = file, PointSetContainer = objectName,
            DataDelimiter = Api.ExportDataDelimeterType.Tab, TargetNameFormat = Api.ExportTargetNameFormat.Target,
            DesiredCoordinateSystem = Api.CoordinateSystemType.Cartesian
        }, options);
        await client.ExportAsciiPointsAsync(new()
        {
            AsciiFilePath = file,
            GroupNamesToExport = { new Api.CollectionGroupName { CollectionName = "collection", GroupName = "group" } },
            DataDelimiter = Api.ExportDataDelimeterType.Tab, TargetNameFormat = Api.ExportTargetNameFormat.Target,
            DesiredCoordinateSystem = Api.CoordinateSystemType.Cartesian
        }, options);
        await client.ExportVectorContainerToAsciiFileAsync(new()
        {
            AsciiFilePath = file,
            VectorGroupsToExport = { new Api.CollectionVectorGroupName { CollectionName = "collection", VectorGroupName = "vectors" } },
            VectorNameFormat = Api.ExportVectorNameFormat.Vector
        }, options);
        await client.ImportAsciiPredefinedFormatsAsync(new()
        {
            AsciiFilePath = file, FileFormat = Api.AsciiFileFormat.XYZ, GroupName = objectName
        }, options);
        await client.ImportAsciiPredefinedFrameSetFormatsAsync(new()
        {
            AsciiFilePath = file, FileFormat = Api.AsciiFileFormat.TransformationMatrixTimestamp,
            FrameSetContainerName = objectName
        }, options);

        Assert.Equal(Ids, worker.Commands.Select(command => command.OperationId));
        Assert.Equal("SetAsciiFileFormatArg", worker.Commands[0].InputArguments[3].SdkBinding);
        Assert.Equal("Export ASCII Points", worker.Commands[4].StepName);
        Assert.Equal("SetCollectionVectorGroupNameRefListArg", worker.Commands[5].InputArguments[1].SdkBinding);
    }

}
