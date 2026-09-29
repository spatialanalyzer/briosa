using Briosa.Server.Operations;
using Briosa.Server.Operations.ReportingOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedPictureOperationTests
{
    private static readonly string[] Ids =
    [
        "reporting_operations.add_pictures_to_report_bar", "reporting_operations.capture_current_view",
        "reporting_operations.capture_screen_to_file_bmp_jpg_png_gif_tiff", "reporting_operations.delete_picture",
        "reporting_operations.rename_picture", "reporting_operations.save_current_view_bmp_jpg_png_gif_tiff",
        "reporting_operations.set_scale_for_picture"
    ];

    [Fact]
    public void PictureOperationsAreRegisteredAndPreserveAtRiskStatus()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Contains("fixture_validation_pending", CaptureCurrentViewOperation.Descriptor.RiskFlags);
        Assert.Contains("fixture_validation_pending", CaptureScreenToFileBmpJpgPngGifTiffOperation.Descriptor.RiskFlags);
        Assert.Contains("fixture_validation_pending", SaveCurrentViewBmpJpgPngGifTiffOperation.Descriptor.RiskFlags);
    }

    [Fact]
    public void PictureMappingsPreserveBindingsDefaultsAndPresence()
    {
        Assert.Throws<ArgumentException>(() => AddPicturesToReportBarOperation.CreateCommand(new()));
        var add = AddPicturesToReportBarOperation.CreateCommand(new() { Pictures = { Item("Top") } });
        Assert.Equal("Top", add.InputArguments[0].RequireValue<WorkerCollectionItemNameListValue>().Values[0].ItemName);
        Assert.False(add.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => CaptureCurrentViewOperation.CreateCommand(new()));
        Assert.Equal("SetCollectionObjectNameArg2", CaptureCurrentViewOperation.CreateCommand(new()
        {
            PictureName = Item("View")
        }).InputArguments[0].SdkBinding);

        Assert.Throws<ArgumentException>(() => CaptureScreenToFileBmpJpgPngGifTiffOperation.CreateCommand(new()));
        Assert.Equal("image.png", CaptureScreenToFileBmpJpgPngGifTiffOperation.CreateCommand(new()
        {
            FileToSaveTo = File("image.png")
        }).InputArguments[0].RequireValue<WorkerFileReferenceValue>().Path);

        Assert.Throws<ArgumentException>(() => DeletePictureOperation.CreateCommand(new()));
        Assert.Equal("SetCollectionObjectNameArg2", DeletePictureOperation.CreateCommand(new()
        {
            PictureName = Item("Old")
        }).InputArguments[0].SdkBinding);

        Assert.Throws<ArgumentException>(() => RenamePictureOperation.CreateCommand(new()));
        var rename = RenamePictureOperation.CreateCommand(new()
        {
            OriginalPictureName = Item("Old"),
            NewPictureName = Item("New")
        });
        Assert.Equal(["Old", "New"], rename.InputArguments.Take(2)
            .Select(argument => argument.RequireValue<WorkerCollectionItemNameValue>().ItemName));
        Assert.False(rename.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => SaveCurrentViewBmpJpgPngGifTiffOperation.CreateCommand(new()));
        var saveDefault = SaveCurrentViewBmpJpgPngGifTiffOperation.CreateCommand(new()
        {
            FileToSaveTo = File("view.png")
        });
        Assert.Equal(1d, saveDefault.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        var saveZero = SaveCurrentViewBmpJpgPngGifTiffOperation.CreateCommand(new()
        {
            FileToSaveTo = File("view.png"),
            RenderScaleFactor = 0d
        });
        Assert.Equal(0d, saveZero.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);

        Assert.Throws<ArgumentException>(() => SetScaleForPictureOperation.CreateCommand(new()));
        var scaleDefault = SetScaleForPictureOperation.CreateCommand(new() { PictureName = Item("View") });
        Assert.Equal(100d, scaleDefault.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        var scaleZero = SetScaleForPictureOperation.CreateCommand(new()
        {
            PictureName = Item("View"),
            Scale = 0d
        });
        Assert.Equal(0d, scaleZero.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientRoutesPictureOperationsThroughTypedMappings()
    {
        var worker = new PictureWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ReportingOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ReportingOperations.ReportingOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        await client.AddPicturesToReportBarAsync(new() { Pictures = { Item("Top") } }, options);
        await client.CaptureCurrentViewAsync(new() { PictureName = Item("View") }, options);
        await client.CaptureScreenToFileBmpJpgPngGifTiffAsync(new() { FileToSaveTo = File("screen.png") }, options);
        await client.DeletePictureAsync(new() { PictureName = Item("Top") }, options);
        await client.RenamePictureAsync(new()
        {
            OriginalPictureName = Item("Old"), NewPictureName = Item("New"), OverwriteIfExists = true
        }, options);
        await client.SaveCurrentViewBmpJpgPngGifTiffAsync(new()
        {
            FileToSaveTo = File("view.png"), RenderScaleFactor = 1.5d
        }, options);
        await client.SetScaleForPictureAsync(new() { PictureName = Item("View"), Scale = 2d }, options);

        Assert.Equal(Ids, worker.Commands.Select(command => command.OperationId));
    }

    private static Api.CollectionItemName Item(string name) => new()
    {
        CollectionName = "Pictures",
        ItemName = name
    };

    private static Api.FileReference File(string path) => new() { Path = path };

    private sealed class PictureWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
