using Briosa.Server.Operations;
using Briosa.Server.Operations.UtilityOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedUtilityNotesOperationTests
{
    private static readonly string[] Ids =
    [
        "utility_operations.get_collection_notes", "utility_operations.get_folder_notes",
        "utility_operations.get_object_notes", "utility_operations.get_point_notes",
        "utility_operations.set_collection_notes", "utility_operations.set_folder_notes",
        "utility_operations.set_object_notes", "utility_operations.set_point_notes"
    ];

    [Fact]
    public void EachNoteOperationHasOneTypedRegistrationAndNoCatalogEntry()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    [Fact]
    public void GetNotesUseExactIdentityBindingsAndEditTextOutput()
    {
        var commands = new[]
        {
            GetCollectionNotesOperation.CreateCommand(new() { Collection = new() { Name = "C" } }),
            GetFolderNotesOperation.CreateCommand(new() { FolderPath = "F" }),
            GetObjectNotesOperation.CreateCommand(new() { Object = new() { ObjectName = "O" } }),
            GetPointNotesOperation.CreateCommand(new() { Point = new() { TargetName = "P" } })
        };
        Assert.Equal(["SetCollectionNameArg", "SetStringArg", "SetCollectionObjectNameArg2", "SetPointNameArg"],
            commands.Select(item => Assert.Single(item.InputArguments).SdkBinding));
        Assert.All(commands, command =>
        {
            var output = Assert.Single(command.OutputArguments);
            Assert.Equal("Notes", output.Name);
            Assert.Equal(WorkerMpValueKind.EditText, output.Kind);
            Assert.Equal("GetEditTextArg", output.SdkBinding);
        });
        var outputValues = new WorkerMpOutputValue[]
        {
            new WorkerRetrievedOutput("Notes", WorkerMpValueKind.EditText, new WorkerStringListValue(["first", "second"]))
        };
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputValues, "completed");
        var result = GetFolderNotesOperation.CreateResult(
            new SuccessfulOperationExecution(execution, new Api.MpExecutionDetails()));
        Assert.Equal(["first", "second"], result.Notes);
    }

    [Fact]
    public void SetNotesRequireTextAndPreserveAppendDefault()
    {
        Assert.Throws<ArgumentException>(() => GetCollectionNotesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetObjectNotesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetPointNotesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetCollectionNotesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetFolderNotesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetObjectNotesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetPointNotesOperation.CreateCommand(new()));

        var commands = new[]
        {
            SetCollectionNotesOperation.CreateCommand(new() { Collection = new() { Name = "C" }, Notes = { "line" } }),
            SetFolderNotesOperation.CreateCommand(new() { Notes = { "line" } }),
            SetObjectNotesOperation.CreateCommand(new() { Object = new() { ObjectName = "O" }, Notes = { "line" } }),
            SetPointNotesOperation.CreateCommand(new() { Point = new() { TargetName = "P" }, Notes = { "line" } })
        };
        Assert.All(commands, command =>
        {
            Assert.Equal("SetEditTextArg", command.InputArguments[1].SdkBinding);
            Assert.Equal(WorkerMpValueKind.EditText, command.InputArguments[1].Kind);
            Assert.Equal(["line"], command.InputArguments[1].RequireValue<WorkerStringListValue>().Values);
            Assert.True(command.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        });
        var overwrite = SetFolderNotesOperation.CreateCommand(new() { Notes = { "line" }, Append = false });
        Assert.False(overwrite.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientRoutesGetAndSetNotesToTypedWorkerCommands()
    {
        var worker = new NotesWorker();
        var grpcHost = await GrpcTestHost.StartAsync<UtilityOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.UtilityOperations.UtilityOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var fetched = await client.GetFolderNotesAsync(new() { FolderPath = "F" }, options);
        var written = await client.SetFolderNotesAsync(new() { FolderPath = "F", Notes = { "new" } }, options);

        Assert.Equal(["existing"], fetched.Notes);
        Assert.Equal(Api.MpExecutionState.Succeeded, fetched.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, written.Execution.State);
        Assert.Equal(["utility_operations.get_folder_notes", "utility_operations.set_folder_notes"],
            worker.Commands.Select(command => command.OperationId));
    }

    private sealed class NotesWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId == "utility_operations.get_folder_notes"
                ? [new WorkerRetrievedOutput("Notes", WorkerMpValueKind.EditText,
                    new WorkerStringListValue(["existing"]))]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
