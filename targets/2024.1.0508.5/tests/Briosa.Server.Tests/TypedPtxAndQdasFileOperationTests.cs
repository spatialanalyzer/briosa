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

public sealed class TypedPtxAndQdasFileOperationTests
{
    private static readonly string[] OperationIds =
    [
        "file_operations.export_ptx_point_clouds",
        "file_operations.export_qdas_characteristics",
        "file_operations.export_qdas_data_list",
        "file_operations.get_qdas_catalog_entries",
        "file_operations.import_qdas_catalog_file",
        "file_operations.prepare_qdas_data_list"
    ];

    [Fact]
    public void PtxAndQdasOperationsHaveTypedTargetRegistrations()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Equal(["fixture_validation_pending"], ExportPtxPointCloudsOperation.Descriptor.RiskFlags);
        Assert.Equal(["fixture_validation_pending"], ExportQdasCharacteristicsOperation.Descriptor.RiskFlags);
        Assert.Equal(["fixture_validation_pending"], ExportQdasDataListOperation.Descriptor.RiskFlags);
        Assert.Equal(["fixture_validation_pending"], GetQdasCatalogEntriesOperation.Descriptor.RiskFlags);
        Assert.Equal(["fixture_validation_pending"], ImportQdasCatalogFileOperation.Descriptor.RiskFlags);
        Assert.Equal(["fixture_validation_pending"], PrepareQdasDataListOperation.Descriptor.RiskFlags);
        Assert.Equal("Import QDAS Catalog File", ImportQdasCatalogFileOperation.Descriptor.MpStep);
    }

    [Fact]
    public void FileMappingsPreserveBindingsDefaultsAndCallerRequiredTimestamp()
    {
        var ptx = ExportPtxPointCloudsOperation.CreateCommand(new()
        {
            PtxFilePath = new() { Path = "cloud.ptx" },
            PointCloudList = { new Api.CollectionObjectName { CollectionName = "Scans", ObjectName = "Cloud 1" } }
        });
        Assert.Equal("SetFilePathArg", ptx.InputArguments[0].SdkBinding);
        Assert.Equal("SetCollectionObjectNameRefListArg", ptx.InputArguments[1].SdkBinding);
        Assert.False(ptx.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(ptx.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => ExportPtxPointCloudsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => ExportPtxPointCloudsOperation.CreateCommand(new()
        {
            PtxFilePath = new() { Path = "cloud.ptx" }
        }));

        var export = ExportQdasCharacteristicsOperation.CreateCommand(ValidExportRequest());
        Assert.Equal(18, export.InputArguments.Count);
        Assert.Equal("SetFilePathArg", export.InputArguments[0].SdkBinding);
        Assert.Equal(string.Empty, export.InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("2026-09-26/12:00:00", export.InputArguments[11].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(-1, export.InputArguments[12].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal("SetCollectionObjectNameRefListArg", export.InputArguments[15].SdkBinding);
        Assert.Throws<ArgumentException>(() => ExportQdasCharacteristicsOperation.CreateCommand(new()
        {
            QdasExportFilePath = new() { Path = "export.dfd" },
            K0004DateTimeStamp = string.Empty
        }));
        Assert.Throws<ArgumentException>(() => ExportQdasCharacteristicsOperation.CreateCommand(new()
        {
            QdasExportFilePath = new() { Path = "export.dfd" },
            K0004DateTimeStamp = "2026-09-26/12:00:00"
        }));

        Assert.Equal("SetFilePathArg", Assert.Single(
            ExportQdasDataListOperation.CreateCommand(new() { QdasExportFilePath = new() { Path = "data.dfd" } })
                .InputArguments).SdkBinding);
        Assert.Throws<ArgumentException>(() => ExportQdasDataListOperation.CreateCommand(new()));
        Assert.Equal(string.Empty, GetQdasCatalogEntriesOperation.CreateCommand(new())
            .InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("GetStringRefListArg", Assert.Single(
            GetQdasCatalogEntriesOperation.CreateCommand(new()).OutputArguments).SdkBinding);
        Assert.Throws<ArgumentException>(() => ImportQdasCatalogFileOperation.CreateCommand(new()));
        Assert.Equal("SetFilePathArg", Assert.Single(ImportQdasCatalogFileOperation.CreateCommand(new()
        {
            QdasDfdFilePath = new() { Path = "catalog.dfd" }
        }).InputArguments).SdkBinding);

        var prepare = PrepareQdasDataListOperation.CreateCommand(ValidPrepareRequest());
        Assert.Equal(17, prepare.InputArguments.Count);
        Assert.Equal("2026-09-26/12:00:00", prepare.InputArguments[10].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(-1, prepare.InputArguments[11].RequireValue<WorkerIntegerValue>().Value);
        Assert.Throws<ArgumentException>(() => PrepareQdasDataListOperation.CreateCommand(new()));
    }

    [Fact]
    public async Task GeneratedClientRoutesPtxAndQdasCallsAndReturnsCatalogEntries()
    {
        var worker = new PtxAndQdasWorker();
        var grpcHost = await GrpcTestHost.StartAsync<FileOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.FileOperations.FileOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        await client.ExportPtxPointCloudsAsync(new()
        {
            PtxFilePath = new() { Path = "cloud.ptx" },
            PointCloudList = { new Api.CollectionObjectName { CollectionName = "Scans", ObjectName = "Cloud 1" } }
        }, options);
        await client.ExportQdasCharacteristicsAsync(ValidExportRequest(), options);
        await client.ExportQdasDataListAsync(new()
        {
            QdasExportFilePath = new() { Path = "data.dfd" }
        }, options);
        var catalog = await client.GetQdasCatalogEntriesAsync(new() { KFieldTarget = "K1001" }, options);
        await client.ImportQdasCatalogFileAsync(new()
        {
            QdasDfdFilePath = new() { Path = "catalog.dfd" }
        }, options);
        await client.PrepareQdasDataListAsync(ValidPrepareRequest(), options);

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(["Part Number", "Supplier Number"], catalog.CatalogEntries);
        Assert.Equal("2026-09-26/12:00:00",
            worker.Commands[1].InputArguments[11].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("K1001", worker.Commands[3].InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("SetFilePathArg", worker.Commands[4].InputArguments[0].SdkBinding);
    }

    private static Api.ExportQdasCharacteristicsRequest ValidExportRequest() => new()
    {
        QdasExportFilePath = new() { Path = "export.dfd" },
        K1001PartNumber = "part-1",
        K0004DateTimeStamp = "2026-09-26/12:00:00",
        RelationshipList = { new Api.CollectionItemName { CollectionName = "Parts", ItemName = "Relation 1" } },
        FeatureCheckList = { new Api.CollectionItemName { CollectionName = "Parts", ItemName = "Check 1" } },
        VectorGroupList = { new Api.CollectionObjectName { CollectionName = "Parts", ObjectName = "Vectors" } }
    };

    private static Api.PrepareQdasDataListRequest ValidPrepareRequest() => new()
    {
        K0004DateTimeStamp = "2026-09-26/12:00:00",
        RelationshipList = { new Api.CollectionItemName { CollectionName = "Parts", ItemName = "Relation 1" } },
        FeatureCheckList = { new Api.CollectionItemName { CollectionName = "Parts", ItemName = "Check 1" } },
        VectorGroupList = { new Api.CollectionObjectName { CollectionName = "Parts", ObjectName = "Vectors" } }
    };

    private sealed class PtxAndQdasWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            IReadOnlyList<WorkerMpOutputValue> outputs = command.OperationId == "file_operations.get_qdas_catalog_entries"
                ? [WorkerMpOutputValue.FromRetrieval("Catalog Entries", WorkerMpValueKind.StringList, true,
                    new WorkerStringListValue(["Part Number", "Supplier Number"]))]
                : [];
            var result = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, result, null, "completed", 1));
        }
    }
}
