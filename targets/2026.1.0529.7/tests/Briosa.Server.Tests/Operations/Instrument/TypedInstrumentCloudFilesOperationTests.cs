using Briosa.Server.Operations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedInstrumentCloudFilesOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.clear_cloud_viewer",
        "instrument_operations.close_auto_correspond_closest_point_dialog",
        "instrument_operations.export_instrument_history_to_xml_file",
        "instrument_operations.load_cloud_viewer_point_cloud_file",
        "instrument_operations.load_instrument_configuration",
        "instrument_operations.save_cloud_viewer_point_cloud_file",
        "instrument_operations.save_instrument_configuration",
        "instrument_operations.send_cloud_to_sa",
        "instrument_operations.set_cloud_viewer_filter"
    ];

    [Fact]
    public void CloudAndConfigurationCommandsPreserveOptionalFileAndFilterInputs()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var export = ExportInstrumentHistoryToXmlFileOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Single(export.InputArguments);
        var filter = SetCloudViewerFilterOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(0, filter.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        var save = SaveCloudViewerPointCloudFileOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(2, save.InputArguments.Count);
        Assert.False(save.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        var configuration = LoadInstrumentConfigurationOperation.CreateCommand(new()
        {
            Instrument = instrument,
            ConfigurationFile = new Api.FileReference { Path = "tracker.cfg", EmbeddedFile = true }
        });
        var file = configuration.InputArguments[1].RequireValue<WorkerFileReferenceValue>();
        Assert.Equal(("tracker.cfg", true), (file.Path, file.EmbeddedFile));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedCloudAndConfigurationRoutes()
    {
        var worker = new InstrumentWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };

        await client.ClearCloudViewerAsync(new() { Instrument = instrument }, options);
        await client.CloseAutoCorrespondClosestPointDialogAsync(new() { Instrument = instrument }, options);
        await client.ExportInstrumentHistoryToXmlFileAsync(new()
        {
            Instrument = instrument,
            FilePath = new Api.FileReference { Path = "history.xml", EmbeddedFile = false }
        }, options);
        await client.LoadCloudViewerPointCloudFileAsync(new() { Instrument = instrument, FilePath = "cloud.ptx" }, options);
        await client.LoadInstrumentConfigurationAsync(new()
        {
            Instrument = instrument,
            ConfigurationFile = new Api.FileReference { Path = "tracker.cfg", EmbeddedFile = true }
        }, options);
        await client.SaveCloudViewerPointCloudFileAsync(new()
        {
            Instrument = instrument,
            FilePath = "cloud-output.ptx",
            SaveAsAscii = true
        }, options);
        await client.SaveInstrumentConfigurationAsync(new()
        {
            Instrument = instrument,
            ConfigurationFile = new Api.FileReference { Path = "tracker-output.cfg", EmbeddedFile = false }
        }, options);
        await client.SendCloudToSaAsync(new() { Instrument = instrument, CloudName = "Cloud A" }, options);
        await client.SetCloudViewerFilterAsync(new() { Instrument = instrument, FilterValue = 3 }, options);

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal("history.xml", worker.Commands[2].InputArguments[1].RequireValue<WorkerFileReferenceValue>().Path);
        Assert.Equal("cloud.ptx", worker.Commands[3].InputArguments[1].RequireValue<WorkerFileReferenceValue>().Path);
        Assert.Equal("tracker.cfg", worker.Commands[4].InputArguments[1].RequireValue<WorkerFileReferenceValue>().Path);
        Assert.True(worker.Commands[5].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("Cloud A", worker.Commands[7].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(3, worker.Commands[8].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
    }

    private sealed class InstrumentWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
