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

public sealed class TypedFilePathsAndNativeProjectOperationTests
{
    private static readonly string[] Ids =
    [
        "file_operations.backup_now", "file_operations.copy_general_file", "file_operations.delete_general_file",
        "file_operations.find_files_in_directory", "file_operations.find_sub_directories_in_directory",
        "file_operations.get_working_directory", "file_operations.new_sa_file", "file_operations.open_sa_file",
        "file_operations.open_template_file", "file_operations.rename_general_file", "file_operations.save",
        "file_operations.save_as_read_only_template", "file_operations.save_as",
        "file_operations.verify_general_file_exists", "file_operations.verify_mp_file_exists"
    ];

    [Fact]
    public void FilePathAndNativeProjectOperationsAreRegisteredAndRemovedFromCatalog()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Equal("Save As...", SaveAsOperation.Descriptor.MpStep);
        Assert.Equal(Api.ReplaySafety.Safe, VerifyGeneralFileExistsOperation.Descriptor.ReplaySafety);
        Assert.Equal(Api.ReplaySafety.Safe, VerifyMpFileExistsOperation.Descriptor.ReplaySafety);
        Assert.Equal(Api.ReplaySafety.Unsafe, CopyGeneralFileOperation.Descriptor.ReplaySafety);
    }

    [Fact]
    public void MappingsPreservePathBindingsDefaultsAndListResults()
    {
        var copy = CopyGeneralFileOperation.CreateCommand(new()
        {
            SourceFileName = new Api.FileReference { Path = "source.sa", EmbeddedFile = true },
            DestinationFileName = new Api.FileReference { Path = "copy.sa" }
        });
        Assert.Equal("SetFilePathArg", copy.InputArguments[0].SdkBinding);
        Assert.Equal(new WorkerFileReferenceValue("source.sa", true),
            copy.InputArguments[0].RequireValue<WorkerFileReferenceValue>());
        Assert.Equal(new WorkerFileReferenceValue("copy.sa", false),
            copy.InputArguments[1].RequireValue<WorkerFileReferenceValue>());
        Assert.False(copy.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => CopyGeneralFileOperation.CreateCommand(new()));

        var findFiles = FindFilesInDirectoryOperation.CreateCommand(new());
        Assert.Equal(string.Empty, findFiles.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("*.*", findFiles.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.False(findFiles.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("GetStringRefListArg", Assert.Single(findFiles.OutputArguments).SdkBinding);
        var foundFiles = FindFilesInDirectoryOperation.CreateResult(Success(
            [new WorkerRetrievedOutput("Files", WorkerMpValueKind.StringList,
                new WorkerStringListValue(["a.sa", "b.sa"]))]));
        Assert.Equal(["a.sa", "b.sa"], foundFiles.Files);

        var subdirectories = FindSubDirectoriesInDirectoryOperation.CreateCommand(new()
            { Directory = "D:\\data", Recursive = true });
        Assert.Equal("D:\\data", subdirectories.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.True(subdirectories.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        var foundDirectories = FindSubDirectoriesInDirectoryOperation.CreateResult(Success(
            [new WorkerRetrievedOutput("Sub-Directories", WorkerMpValueKind.StringList,
                new WorkerStringListValue(["one", "two"]))]));
        Assert.Equal(["one", "two"], foundDirectories.SubDirectories);

        var saveAs = SaveAsOperation.CreateCommand(new()
            { FileName = new Api.FileReference { Path = "new.sa" } });
        Assert.False(saveAs.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0, saveAs.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.Throws<ArgumentException>(() => OpenSaFileOperation.CreateCommand(new()));

        Assert.Empty(BackupNowOperation.CreateCommand(new()).InputArguments);
        Assert.Empty(NewSaFileOperation.CreateCommand(new()).InputArguments);
        Assert.Empty(SaveOperation.CreateCommand(new()).InputArguments);
    }

    [Fact]
    public async Task GeneratedClientRoutesFileCallsToTypedWorkerCommands()
    {
        var worker = new FileOperationsWorker();
        var grpcHost = await GrpcTestHost.StartAsync<FileOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.FileOperations.FileOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var files = await client.FindFilesInDirectoryAsync(new()
            { Directory = "D:\\data", FileNamePattern = "*.sa", Recursive = true }, options);
        await client.CopyGeneralFileAsync(new()
        {
            SourceFileName = new Api.FileReference { Path = "source.sa" },
            DestinationFileName = new Api.FileReference { Path = "copy.sa" },
            Overwrite = true
        }, options);
        await client.SaveAsAsync(new()
            { FileName = new Api.FileReference { Path = "saved.sa" }, OptionalNumber = 3 }, options);

        Assert.Equal(["found.sa"], files.Files);
        Assert.Equal(
        [
            "file_operations.find_files_in_directory", "file_operations.copy_general_file", "file_operations.save_as"
        ], worker.Commands.Select(command => command.OperationId));
        Assert.Equal("*.sa", worker.Commands[0].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.True(worker.Commands[1].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(3, worker.Commands[2].InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal("Save As...", worker.Commands[2].StepName);
    }

    private static SuccessfulOperationExecution Success(IReadOnlyList<WorkerMpOutputValue> outputs)
    {
        var result = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
        return new(result, new Api.MpExecutionDetails());
    }

    private sealed class FileOperationsWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId == "file_operations.find_files_in_directory"
                ? [new WorkerRetrievedOutput("Files", WorkerMpValueKind.StringList,
                    new WorkerStringListValue(["found.sa"]))]
                : [];
            var result = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, result, null, "completed", 1));
        }
    }
}
