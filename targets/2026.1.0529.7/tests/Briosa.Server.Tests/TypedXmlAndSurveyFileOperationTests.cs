using Briosa.Server.Operations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedXmlAndSurveyFileOperationTests
{
    private static readonly string[] Ids =
    [
        "file_operations.export_hidden_point_bar_xml_file", "file_operations.import_hidden_point_bar_xml_file",
        "file_operations.import_nominals_from_xml_file", "file_operations.merge_measurements_into_xml_file",
        "file_operations.import_leica_gsi_file", "file_operations.import_leica_sdb_file",
        "file_operations.import_vstars_cameras", "file_operations.import_vstars_xyz_file",
        "file_operations.import_e57_file"
    ];

    [Fact]
    public void XmlAndSurveyFileOperationsHaveExactTypedRiskMetadata()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var riskIds = new[]
        {
            ImportE57FileOperation.Descriptor, ImportLeicaGsiFileOperation.Descriptor,
            ImportLeicaSdbFileOperation.Descriptor, ImportVstarsCamerasOperation.Descriptor,
            ImportVstarsXyzFileOperation.Descriptor
        };
        Assert.All(riskIds, descriptor => Assert.Equal(["fixture_validation_pending"], descriptor.RiskFlags));
        Assert.Empty(ExportHiddenPointBarXmlFileOperation.Descriptor.RiskFlags);
    }

    [Fact]
    public void XmlAndSurveyFileMappingsPreserveArgumentsDefaultsAndUnits()
    {
        var xml = new Api.FileReference { Path = "measurements.xml" };
        var exportXml = ExportHiddenPointBarXmlFileOperation.CreateCommand(new() { XmlFilePath = xml });
        var importXml = ImportHiddenPointBarXmlFileOperation.CreateCommand(new() { XmlFilePath = xml });
        Assert.Equal("SetFilePathArg", Assert.Single(exportXml.InputArguments).SdkBinding);
        Assert.False(importXml.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("SetBoolArg", importXml.InputArguments[1].SdkBinding);
        Assert.Equal("SetFilePathArg", Assert.Single(
            ImportNominalsFromXmlFileOperation.CreateCommand(new() { FilePath = xml }).InputArguments).SdkBinding);

        var group = new Api.CollectionObjectName { CollectionName = "C", ObjectName = "G" };
        var merge = MergeMeasurementsIntoXmlFileOperation.CreateCommand(new Api.MergeMeasurementsIntoXmlFileRequest
        {
            FilePath = xml, GroupName = group
        });
        Assert.Equal("SetCollectionObjectNameArg2", merge.InputArguments[1].SdkBinding);

        var instrument = new Api.CollectionInstrumentId { CollectionName = "C", InstrumentId = 4 };
        var gsi = ImportLeicaGsiFileOperation.CreateCommand(new()
        {
            InstrumentId = instrument, GroupName = group, FilePath = new() { Path = "survey.gsi" }
        });
        var sdb = ImportLeicaSdbFileOperation.CreateCommand(new()
        {
            InstrumentId = instrument, ScanCloudName = group, FilePath = new() { Path = "survey.sdb" }
        });
        Assert.Equal("SetColInstIdArg", gsi.InputArguments[0].SdkBinding);
        Assert.Equal(new WorkerCollectionInstrumentIdValue("C", 4),
            gsi.InputArguments[0].RequireValue<WorkerCollectionInstrumentIdValue>());
        Assert.Equal("SetCollectionObjectNameArg2", sdb.InputArguments[1].SdkBinding);
        Assert.Equal("survey.sdb", sdb.InputArguments[2].RequireValue<WorkerFileReferenceValue>().Path);

        var cameras = ImportVstarsCamerasOperation.CreateCommand(new() { FilePath = new() { Path = "cameras.csv" } });
        var vstars = ImportVstarsXyzFileOperation.CreateCommand(new() { FilePath = new() { Path = "targets.xyz" } });
        Assert.Equal("cameras.csv", cameras.InputArguments[0].RequireValue<WorkerFileReferenceValue>().Path);
        Assert.Equal("targets.xyz", vstars.InputArguments[0].RequireValue<WorkerFileReferenceValue>().Path);

        var e57 = ImportE57FileOperation.CreateCommand(new() { E57FilePath = new() { Path = "scan.e57" } });
        Assert.Equal(7, e57.InputArguments.Count);
        Assert.False(e57.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(e57.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(e57.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(e57.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(e57.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerDistanceUnitValue.Inches,
            e57.InputArguments[6].RequireValue<WorkerDistanceUnitChoice>().Value);
        var explicitUnits = ImportE57FileOperation.CreateCommand(new()
        {
            E57FilePath = new() { Path = "scan.e57" }, Units = Api.DistanceUnits.Millimeters,
            UseSquareRootOfIntensity = false
        });
        Assert.Equal(WorkerDistanceUnitValue.Millimeters,
            explicitUnits.InputArguments[6].RequireValue<WorkerDistanceUnitChoice>().Value);
        Assert.False(explicitUnits.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => ExportHiddenPointBarXmlFileOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => ImportLeicaGsiFileOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => ImportE57FileOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => ImportE57FileOperation.CreateCommand(new()
        {
            E57FilePath = new() { Path = "scan.e57" }, Units = Api.DistanceUnits.Unspecified
        }));
    }

    [Fact]
    public async Task GeneratedClientRoutesXmlAndSurveyFileOperationsToTypedCommands()
    {
        var worker = new SurveyFilesWorker();
        var grpcHost = await GrpcTestHost.StartAsync<FileOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.FileOperations.FileOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var file = new Api.FileReference { Path = "measurements.xml" };
        var group = new Api.CollectionObjectName { CollectionName = "C", ObjectName = "G" };
        var instrument = new Api.CollectionInstrumentId { CollectionName = "C", InstrumentId = 4 };

        await client.ExportHiddenPointBarXmlFileAsync(new() { XmlFilePath = file }, options);
        await client.ImportHiddenPointBarXmlFileAsync(new() { XmlFilePath = file, ReplaceExistingEntries = true }, options);
        await client.ImportNominalsFromXmlFileAsync(new() { FilePath = file }, options);
        await client.MergeMeasurementsIntoXmlFileAsync(new() { FilePath = file, GroupName = group }, options);
        await client.ImportLeicaGsiFileAsync(new()
        {
            InstrumentId = instrument, GroupName = group, FilePath = new() { Path = "survey.gsi" }
        }, options);
        await client.ImportLeicaSdbFileAsync(new()
        {
            InstrumentId = instrument, ScanCloudName = group, FilePath = new() { Path = "survey.sdb" }
        }, options);
        await client.ImportVstarsCamerasAsync(new() { FilePath = new() { Path = "cameras.csv" } }, options);
        await client.ImportVstarsXyzFileAsync(new() { FilePath = new() { Path = "targets.xyz" } }, options);
        await client.ImportE57FileAsync(new() { E57FilePath = new() { Path = "scan.e57" } }, options);

        Assert.Equal(Ids, worker.Commands.Select(command => command.OperationId));
        Assert.True(worker.Commands[1].InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("SetColInstIdArg", worker.Commands[4].InputArguments[0].SdkBinding);
        Assert.Equal(WorkerDistanceUnitValue.Inches,
            worker.Commands[8].InputArguments[6].RequireValue<WorkerDistanceUnitChoice>().Value);
    }

    private sealed class SurveyFilesWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var result = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, result, null, "completed", 1));
        }
    }
}
