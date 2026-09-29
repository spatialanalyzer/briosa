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

public sealed class TypedCadAndMeshFileOperationTests
{
    private static readonly string[] Ids =
    [
        "file_operations.direct_cad_access", "file_operations.export_dxf",
        "file_operations.export_iges_file_entire_model", "file_operations.export_iges_file_partial_model",
        "file_operations.export_scan_stripe_mesh_to_stl_file", "file_operations.export_step_file_entire_model",
        "file_operations.export_step_file_partial_model", "file_operations.export_vda_fs_file_entire_model",
        "file_operations.export_vda_fs_file_partial_model", "file_operations.import_iges_file",
        "file_operations.import_polyworks_file", "file_operations.import_sat_file",
        "file_operations.import_step_file", "file_operations.import_stl_file", "file_operations.import_vda_fs_file"
    ];

    [Fact]
    public void CadAndMeshOperationsAreRegisteredAndRemovedFromGenericCatalog()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Equal(["fixture_validation_pending"], DirectCadAccessOperation.Descriptor.RiskFlags);
        Assert.Equal(["fixture_validation_pending"], ExportScanStripeMeshToStlFileOperation.Descriptor.RiskFlags);
        Assert.Equal(["fixture_validation_pending"], ImportPolyworksFileOperation.Descriptor.RiskFlags);
        Assert.Equal("Export IGES File - Entire Model", ExportIgesFileEntireModelOperation.Descriptor.MpStep);
        Assert.Equal("Export VDA/FS File - Entire Model", ExportVdaFsFileEntireModelOperation.Descriptor.MpStep);
    }

    [Fact]
    public void CadAndMeshMappingsPreserveRequiredListsDefaultsAndOutputs()
    {
        var cad = DirectCadAccessOperation.CreateCommand(new()
        {
            CadFileName = new Api.FileReference { Path = "model.step" },
            SurfaceCompatibilityMode = false
        });
        Assert.Equal(30, cad.InputArguments.Count);
        Assert.Equal("SetFilePathArg", cad.InputArguments[0].SdkBinding);
        Assert.True(cad.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("CAD pts", cad.InputArguments[7].RequireValue<WorkerTextValue>().Value);
        Assert.False(cad.InputArguments[14].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(1, cad.InputArguments[19].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(cad.InputArguments[26].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(4, cad.OutputArguments.Count);
        var cadDefaults = DirectCadAccessOperation.CreateCommand(new()
            { CadFileName = new Api.FileReference { Path = "default.step" } });
        Assert.True(cadDefaults.InputArguments[26].RequireValue<WorkerBooleanValue>().Value);
        var cadResult = DirectCadAccessOperation.CreateResult(Success(
        [
            new WorkerRetrievedOutput("Import Warnings", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
            new WorkerRetrievedOutput("Import Warning Messages", WorkerMpValueKind.Text, new WorkerTextValue("warnings")),
            new WorkerRetrievedOutput("Extents Min", WorkerMpValueKind.Vector, new WorkerVectorValue(1, 2, 3)),
            new WorkerRetrievedOutput("Extents Max", WorkerMpValueKind.Vector, new WorkerVectorValue(4, 5, 6))
        ]));
        Assert.True(cadResult.ImportWarnings);
        Assert.Equal("warnings", cadResult.ImportWarningMessages);
        Assert.Equal(new Api.Vector { X = 1, Y = 2, Z = 3 }, cadResult.ExtentsMin);
        Assert.Throws<ArgumentException>(() => DirectCadAccessOperation.CreateCommand(new()));

        var file = new Api.FileReference { Path = "model.dxf" };
        var point = new Api.PointName { CollectionName = "c", GroupName = "g", TargetName = "p" };
        var cloud = new Api.CollectionObjectName { CollectionName = "c", ObjectName = "cloud" };
        var dxf = ExportDxfOperation.CreateCommand(new()
        {
            DxfFilePath = file, PointNames = { point }, CloudNames = { cloud }
        });
        Assert.Equal("SetPointNameRefListArg", dxf.InputArguments[1].SdkBinding);
        Assert.Equal("SetCollectionObjectNameRefListArg", dxf.InputArguments[2].SdkBinding);
        Assert.True(dxf.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => ExportDxfOperation.CreateCommand(new()));

        var partialObject = new Api.CollectionObjectName { CollectionName = "c", ObjectName = "surface" };
        var iges = ExportIgesFilePartialModelOperation.CreateCommand(new()
        {
            IgesFilePath = file, ObjectNameList = { partialObject }
        });
        Assert.Single(iges.InputArguments[1].RequireValue<WorkerCollectionObjectNameListValue>().Values);
        var step = ExportStepFilePartialModelOperation.CreateCommand(new()
        {
            StepFilePath = file, ObjectNameList = { partialObject }
        });
        Assert.Equal("SetCollectionObjectNameRefListArg", step.InputArguments[1].SdkBinding);
        var vda = ExportVdaFsFilePartialModelOperation.CreateCommand(new()
        {
            VdaFsFilePath = file, ObjectNameList = { partialObject }
        });
        Assert.Single(vda.InputArguments[1].RequireValue<WorkerCollectionObjectNameListValue>().Values);
        Assert.Throws<ArgumentException>(() => ExportIgesFilePartialModelOperation.CreateCommand(new() { IgesFilePath = file }));

        var mesh = ExportScanStripeMeshToStlFileOperation.CreateCommand(new()
        {
            StlFilePath = file, Mesh = partialObject
        });
        Assert.Equal("SetCollectionObjectNameArg2", mesh.InputArguments[1].SdkBinding);
        Assert.Throws<ArgumentException>(() => ExportScanStripeMeshToStlFileOperation.CreateCommand(new()));
        Assert.Equal("SetFilePathArg", ExportIgesFileEntireModelOperation.CreateCommand(new()
            { IgesFilePath = file }).InputArguments[0].SdkBinding);
        Assert.Equal("SetFilePathArg", ExportStepFileEntireModelOperation.CreateCommand(new()
            { StepFilePath = file }).InputArguments[0].SdkBinding);
        Assert.Equal("SetFilePathArg", ExportVdaFsFileEntireModelOperation.CreateCommand(new()
            { VdaFsFilePath = file }).InputArguments[0].SdkBinding);

        Assert.Equal("SetFilePathArg", ImportIgesFileOperation.CreateCommand(new()
            { IgesFilePath = file }).InputArguments[0].SdkBinding);
        var polyworks = ImportPolyworksFileOperation.CreateCommand(new() { CloudName = cloud, FilePath = file });
        Assert.Equal("SetCollectionObjectNameArg2", polyworks.InputArguments[0].SdkBinding);
        Assert.Equal("SetFilePathArg", ImportSatFileOperation.CreateCommand(new()
            { SatFilePath = file }).InputArguments[0].SdkBinding);
        var importStep = ImportStepFileOperation.CreateCommand(new() { StepFilePath = file });
        Assert.False(importStep.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(importStep.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        var importStl = ImportStlFileOperation.CreateCommand(new() { StlFilePath = file });
        Assert.Equal(WorkerDistanceUnitValue.Millimeters,
            importStl.InputArguments[1].RequireValue<WorkerDistanceUnitChoice>().Value);
        Assert.True(importStl.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(importStl.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("SetFilePathArg", ImportVdaFsFileOperation.CreateCommand(new()
            { VdaFsFilePath = file }).InputArguments[0].SdkBinding);
    }

    [Fact]
    public async Task GeneratedClientRoutesCadAndMeshCallsToTypedWorkerCommands()
    {
        var worker = new RecordingWorker();
        var grpcHost = await GrpcTestHost.StartAsync<FileOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.FileOperations.FileOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(30));
        var file = new Api.FileReference { Path = "model.cad" };
        var objectName = new Api.CollectionObjectName { CollectionName = "c", ObjectName = "item" };

        await client.DirectCadAccessAsync(new() { CadFileName = file, SurfaceCompatibilityMode = true }, options);
        await client.ExportDxfAsync(new()
        {
            DxfFilePath = file,
            PointNames = { new Api.PointName { CollectionName = "c", GroupName = "g", TargetName = "p" } },
            CloudNames = { objectName }
        }, options);
        await client.ExportIgesFileEntireModelAsync(new() { IgesFilePath = file }, options);
        await client.ExportIgesFilePartialModelAsync(new() { IgesFilePath = file, ObjectNameList = { objectName } }, options);
        await client.ExportScanStripeMeshToStlFileAsync(new() { StlFilePath = file, Mesh = objectName }, options);
        await client.ExportStepFileEntireModelAsync(new() { StepFilePath = file }, options);
        await client.ExportStepFilePartialModelAsync(new() { StepFilePath = file, ObjectNameList = { objectName } }, options);
        await client.ExportVdaFsFileEntireModelAsync(new() { VdaFsFilePath = file }, options);
        await client.ExportVdaFsFilePartialModelAsync(new() { VdaFsFilePath = file, ObjectNameList = { objectName } }, options);
        await client.ImportIgesFileAsync(new() { IgesFilePath = file }, options);
        await client.ImportPolyworksFileAsync(new() { CloudName = objectName, FilePath = file }, options);
        await client.ImportSatFileAsync(new() { SatFilePath = file }, options);
        await client.ImportStepFileAsync(new() { StepFilePath = file }, options);
        await client.ImportStlFileAsync(new() { StlFilePath = file }, options);
        await client.ImportVdaFsFileAsync(new() { VdaFsFilePath = file }, options);

        Assert.Equal(Ids, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(30, worker.Commands[0].InputArguments.Count);
        Assert.Equal("SetPointNameRefListArg", worker.Commands[1].InputArguments[1].SdkBinding);
        Assert.Equal("SetCollectionObjectNameArg2", worker.Commands[4].InputArguments[1].SdkBinding);
        Assert.Equal("Import STEP File", worker.Commands[12].StepName);
    }

    private static SuccessfulOperationExecution Success(IReadOnlyList<WorkerMpOutputValue> outputs)
    {
        var result = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
        return new(result, new Api.MpExecutionDetails());
    }

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];
        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId == "file_operations.direct_cad_access" ?
            [
                new WorkerRetrievedOutput("Import Warnings", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
                new WorkerRetrievedOutput("Import Warning Messages", WorkerMpValueKind.Text, new WorkerTextValue(string.Empty)),
                new WorkerRetrievedOutput("Extents Min", WorkerMpValueKind.Vector, new WorkerVectorValue(0, 0, 0)),
                new WorkerRetrievedOutput("Extents Max", WorkerMpValueKind.Vector, new WorkerVectorValue(0, 0, 0))
            ] : [];
            var result = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, result, null, "completed", 1));
        }
    }
}
