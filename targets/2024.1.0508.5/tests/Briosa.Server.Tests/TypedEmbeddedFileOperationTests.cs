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

public sealed class TypedEmbeddedFileOperationTests
{
    private static readonly string[] Ids =
    [
        "file_operations.export_embedded_file", "file_operations.import_file_as_embedded_file",
        "file_operations.import_mp_file_as_embedded_mp", "file_operations.make_embedded_file_name_list"
    ];

    [Fact]
    public void EmbeddedFileOperationsHaveTypedRegistrationsAndPreserveTargetStepNames()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Equal(Api.ReplaySafety.Unsafe, ExportEmbeddedFileOperation.Descriptor.ReplaySafety);
        Assert.Equal(Api.ReplaySafety.Unsafe, MakeEmbeddedFileNameListOperation.Descriptor.ReplaySafety);
        Assert.Equal(SpatialAnalyzerApi.TargetVersion.StartsWith("2024", StringComparison.Ordinal)
                ? "Import File as Embedded File"
                : "Import File As Embedded File",
            ImportFileAsEmbeddedFileOperation.Descriptor.MpStep);
        Assert.Equal(SpatialAnalyzerApi.TargetVersion.StartsWith("2024", StringComparison.Ordinal)
                ? "Import MP File as Embedded MP"
                : "Import MP File As Embedded MP",
            ImportMpFileAsEmbeddedMpOperation.Descriptor.MpStep);
    }

    [Fact]
    public void EmbeddedFileMappingsPreserveRequiredInputsAndDefaults()
    {
        var file = new Api.FileReference { Path = "document.pdf" };
        var export = ExportEmbeddedFileOperation.CreateCommand(new()
        {
            EmbeddedFileCollectionName = new() { Name = "Docs" },
            ExternalFileName = file
        });
        Assert.Equal("SetCollectionNameArg", export.InputArguments[0].SdkBinding);
        Assert.Equal("Docs", export.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("SetFilePathArg", export.InputArguments[2].SdkBinding);
        Assert.False(export.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);

        var import = ImportFileAsEmbeddedFileOperation.CreateCommand(new() { ExternalFileName = file });
        var importMp = ImportMpFileAsEmbeddedMpOperation.CreateCommand(new()
        {
            ExternalMpFileName = new Api.FileReference { Path = "routine.mp" }
        });
        Assert.False(import.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(importMp.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("SetBoolArg", importMp.InputArguments[1].SdkBinding);

        var embeddedFiles = MakeEmbeddedFileNameListOperation.CreateCommand(new());
        Assert.Equal("*", embeddedFiles.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("*.*", embeddedFiles.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("GetStringRefListArg", embeddedFiles.OutputArguments[0].SdkBinding);
        var explicitEmpty = MakeEmbeddedFileNameListOperation.CreateCommand(new()
        {
            CollectionWildcardCriteria = string.Empty,
            FileNamePattern = string.Empty
        });
        Assert.Equal(string.Empty, explicitEmpty.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(string.Empty, explicitEmpty.InputArguments[1].RequireValue<WorkerTextValue>().Value);

        Assert.Throws<ArgumentException>(() => ExportEmbeddedFileOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => ExportEmbeddedFileOperation.CreateCommand(new()
        {
            EmbeddedFileCollectionName = new() { Name = "Docs" }
        }));
        Assert.Throws<ArgumentException>(() => ImportFileAsEmbeddedFileOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => ImportMpFileAsEmbeddedMpOperation.CreateCommand(new()));
    }

    [Fact]
    public async Task GeneratedClientRoutesEmbeddedFileOperationsToTypedCommands()
    {
        var worker = new EmbeddedFileWorker();
        var grpcHost = await GrpcTestHost.StartAsync<FileOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.FileOperations.FileOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        await client.ExportEmbeddedFileAsync(new()
        {
            EmbeddedFileCollectionName = new() { Name = "Docs" },
            EmbeddedFileName = "manual.pdf",
            ExternalFileName = new() { Path = "manual.pdf" },
            ReplaceExisting = true
        }, options);
        await client.ImportFileAsEmbeddedFileAsync(new()
        {
            ExternalFileName = new() { Path = "image.png" }, ReplaceExisting = true
        }, options);
        await client.ImportMpFileAsEmbeddedMpAsync(new()
        {
            ExternalMpFileName = new() { Path = "routine.mp" }
        }, options);
        var listed = await client.MakeEmbeddedFileNameListAsync(new(), options);

        Assert.Equal(["manual.pdf", "routine.mp"], listed.EmbeddedFiles);
        Assert.Equal(Ids, worker.Commands.Select(command => command.OperationId));
        Assert.True(worker.Commands[0].InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(worker.Commands[1].InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("*.*", worker.Commands[3].InputArguments[1].RequireValue<WorkerTextValue>().Value);
    }

    private sealed class EmbeddedFileWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId == "file_operations.make_embedded_file_name_list"
                ? [new WorkerRetrievedOutput("Embedded Files", WorkerMpValueKind.StringList,
                    new WorkerStringListValue(["manual.pdf", "routine.mp"]))]
                : [];
            var result = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, result, null, "completed", 1));
        }
    }
}
