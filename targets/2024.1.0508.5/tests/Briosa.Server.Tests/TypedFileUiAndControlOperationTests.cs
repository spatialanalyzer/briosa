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

public sealed class TypedFileUiAndControlOperationTests
{
    private static readonly string[] CommonIds =
    [
        "file_operations.import_file_as_picture", "file_operations.import_sa_file",
        "file_operations.import_sa_windows_placement", "file_operations.load_html_form",
        "file_operations.load_html_form_in_edge_browser", "file_operations.pop_poly_bay_analysis_window",
        "file_operations.terminate_all_running_mps"
    ];

    private static readonly string[] Ids = CommonIds;

    [Fact]
    public void FileUiAndControlOperationsHaveExactTargetRegistrations()
    {
        foreach (var id in CommonIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Equal(["fixture_validation_pending"], PopPolyBayAnalysisWindowOperation.Descriptor.RiskFlags);
    }

    [Fact]
    public void FileUiMappingsPreserveDefaultsRequiredInputsAndOptionalListContract()
    {
        var file = new Api.FileReference { Path = "file.bin" };
        var picture = ImportFileAsPictureOperation.CreateCommand(new() { ExternalFileName = file });
        Assert.Equal("SetFilePathArg", picture.InputArguments[0].SdkBinding);
        Assert.False(picture.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => ImportFileAsPictureOperation.CreateCommand(new()));

        var saImport = ImportSaFileOperation.CreateCommand(new()
        {
            SaFileName = new() { Path = "project.sa" }, SelectedCollections = { "A", "B" }
        });
        Assert.False(saImport.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("SetStringRefListArg", saImport.InputArguments[2].SdkBinding);
        Assert.Equal(["A", "B"], saImport.InputArguments[2].RequireValue<WorkerStringListValue>().Values);
        Assert.Throws<ArgumentException>(() => ImportSaFileOperation.CreateCommand(new()
        {
            SaFileName = new() { Path = "project.sa" }
        }));
        Assert.Throws<ArgumentException>(() => ImportSaFileOperation.CreateCommand(new()
        {
            SelectedCollections = { "A" }
        }));
        var placement = ImportSaWindowsPlacementOperation.CreateCommand(new() { FilePath = file });
        Assert.Equal("SetFilePathArg", Assert.Single(placement.InputArguments).SdkBinding);

        var html = LoadHtmlFormOperation.CreateCommand(new()
        {
            InputHtmlFormPath = new() { Path = "form.html" },
            InputDataShareFilePath = new() { Path = "input.data" },
            OutputDataShareFilePath = new() { Path = "output.data" }
        });
        Assert.Equal(9, html.InputArguments.Count);
        Assert.Equal(1000, html.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(800, html.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(html.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("Save", html.InputArguments[6].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("Cancel", html.InputArguments[7].RequireValue<WorkerTextValue>().Value);
        Assert.False(html.InputArguments[8].RequireValue<WorkerBooleanValue>().Value);
        var customized = LoadHtmlFormOperation.CreateCommand(new()
        {
            InputHtmlFormPath = new() { Path = "form.html" }, WindowWidth = 640,
            InputDataShareFilePath = new() { Path = "input.data" },
            OutputDataShareFilePath = new() { Path = "output.data" }, SaveButtonText = string.Empty
        });
        Assert.Equal(640, customized.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(string.Empty, customized.InputArguments[6].RequireValue<WorkerTextValue>().Value);
        Assert.Throws<ArgumentException>(() => LoadHtmlFormOperation.CreateCommand(new()));

        var edge = LoadHtmlFormInEdgeBrowserOperation.CreateCommand(new()
        {
            InputHtmlFormPath = new() { Path = "form.html" },
            InputDataShareFilePath = new() { Path = "input.data" },
            OutputDataShareFilePath = new() { Path = "output.data" }
        });
        Assert.Equal(6, edge.InputArguments.Count);
        Assert.Equal(1000, edge.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(800, edge.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.Throws<ArgumentException>(() => LoadHtmlFormInEdgeBrowserOperation.CreateCommand(new()));

        var polyBay = PopPolyBayAnalysisWindowOperation.CreateCommand(new());
        Assert.Equal(string.Empty, polyBay.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(string.Empty, polyBay.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Empty(TerminateAllRunningMPsOperation.CreateCommand(new()).InputArguments);
    }

    [Fact]
    public async Task GeneratedClientRoutesFileUiAndControlCallsToTypedCommands()
    {
        var worker = new UiAndControlWorker();
        var grpcHost = await GrpcTestHost.StartAsync<FileOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.FileOperations.FileOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        await client.ImportFileAsPictureAsync(new() { ExternalFileName = new() { Path = "image.png" } }, options);
        await client.ImportSaFileAsync(new()
        {
            SaFileName = new() { Path = "project.sa" }, SelectedCollections = { "C" }
        }, options);
        await client.ImportSaWindowsPlacementAsync(new() { FilePath = new() { Path = "placement.xml" } }, options);
        await client.LoadHtmlFormAsync(new()
        {
            InputHtmlFormPath = new() { Path = "form.html" },
            InputDataShareFilePath = new() { Path = "input.data" },
            OutputDataShareFilePath = new() { Path = "output.data" }
        }, options);
        await client.LoadHtmlFormInEdgeBrowserAsync(new()
        {
            InputHtmlFormPath = new() { Path = "form.html" },
            InputDataShareFilePath = new() { Path = "input.data" },
            OutputDataShareFilePath = new() { Path = "output.data" }
        }, options);
        await client.PopPolyBayAnalysisWindowAsync(new(), options);
        await client.TerminateAllRunningMPsAsync(new(), options);
        Assert.Equal(Ids, worker.Commands.Select(command => command.OperationId));
        Assert.Equal("SetStringRefListArg", worker.Commands[1].InputArguments[2].SdkBinding);
        Assert.Equal(1000, worker.Commands[3].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Empty(worker.Commands[6].InputArguments);
    }

    private sealed class UiAndControlWorker : IWorkerCommandExecutor
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
