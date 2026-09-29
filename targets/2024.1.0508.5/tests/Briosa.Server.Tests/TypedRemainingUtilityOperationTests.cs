using Briosa.Server.Operations;
using Briosa.Server.Operations.UtilityOperations;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedRemainingUtilityOperationTests
{
    private static readonly string[] Ids =
    [
        "utility_operations.get_screen_resolution",
        "utility_operations.move_instruments_drag_graphically",
        "utility_operations.move_objects_drag_graphically",
        "utility_operations.scale_objects",
        "utility_operations.set_view_idle_update_frequency",
        "utility_operations.trim_log_file",
        "utility_operations.write_to_log"
    ];

    [Fact]
    public void AllRemainingUtilityRoutesAreTypedAndRegistered()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, descriptor => descriptor.OperationId == id);
        }
        Assert.Contains("fixture_validation_pending", MoveInstrumentsDragGraphicallyOperation.Descriptor.RiskFlags);
        Assert.Contains("fixture_validation_pending", MoveObjectsDragGraphicallyOperation.Descriptor.RiskFlags);
    }

    [Fact]
    public void CommandsPreserveBindingsDefaultsAndRequiredLists()
    {
        var screen = GetScreenResolutionOperation.CreateCommand(new());
        Assert.Equal(-1, screen.InputArguments[0].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal("SetIntegerArg", screen.InputArguments[0].SdkBinding);
        Assert.Equal(6, screen.OutputArguments.Count);
        Assert.All(screen.OutputArguments, output => Assert.Equal("GetIntegerArg", output.SdkBinding));
        Assert.Equal(3, GetScreenResolutionOperation.CreateCommand(new() { Display = 3 })
            .InputArguments[0].RequireValue<WorkerIntegerValue>().Value);

        Assert.Throws<ArgumentException>(() => MoveInstrumentsDragGraphicallyOperation.CreateCommand(new()));
        var instruments = MoveInstrumentsDragGraphicallyOperation.CreateCommand(new()
        {
            Instruments = { new Api.CollectionInstrumentId { CollectionName = "C", InstrumentId = 7 } }
        });
        Assert.Equal("SetColInstIdRefListArg", instruments.InputArguments[0].SdkBinding);
        Assert.Equal(7, Assert.Single(instruments.InputArguments[0]
            .RequireValue<WorkerCollectionInstrumentIdListValue>().Values).InstrumentId);

        Assert.Throws<ArgumentException>(() => MoveObjectsDragGraphicallyOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => ScaleObjectsOperation.CreateCommand(new()));
        var moved = MoveObjectsDragGraphicallyOperation.CreateCommand(new()
        {
            Objects = { new Api.CollectionObjectName { CollectionName = "C", ObjectName = "O" } }
        });
        Assert.Equal("SetCollectionObjectNameRefListArg", moved.InputArguments[0].SdkBinding);
        var scaled = ScaleObjectsOperation.CreateCommand(new()
        {
            Objects = { new Api.CollectionObjectName { CollectionName = "C", ObjectName = "O" } },
            ScaleFactor = 2.5
        });
        Assert.Equal(["SetCollectionObjectNameRefListArg", "SetDoubleArg"],
            scaled.InputArguments.Select(input => input.SdkBinding));
        Assert.Equal(2.5, scaled.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, SetViewIdleUpdateFrequencyOperation.CreateCommand(new())
            .InputArguments[0].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(10, TrimLogFileOperation.CreateCommand(new())
            .InputArguments[0].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(0, TrimLogFileOperation.CreateCommand(new() { NumberOfEntriesToKeep = 0 })
            .InputArguments[0].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal("", WriteToLogOperation.CreateCommand(new())
            .InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("SetStringArg", WriteToLogOperation.CreateCommand(new() { LogEntry = "entry" })
            .InputArguments[0].SdkBinding);
    }

    [Fact]
    public void ScreenOutputMapsAllSixValuesInOrder()
    {
        string[] names =
        [
            "Integer Window Top Left X Position", "Integer Window Top Left Y Position",
            "Integer Width", "Integer Height", "View Width", "View Height"
        ];
        var outputs = Enumerable.Range(1, 6).Select((value, index) =>
            new WorkerRetrievedOutput(names[index], WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(value))).ToArray();
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
        var result = GetScreenResolutionOperation.CreateResult(new(execution, new Api.MpExecutionDetails()));
        Assert.Equal([1, 2, 3, 4, 5, 6], new[]
        {
            result.IntegerWindowTopLeftXPosition, result.IntegerWindowTopLeftYPosition,
            result.IntegerWidth, result.IntegerHeight, result.ViewWidth, result.ViewHeight
        });
    }

    [Fact]
    public async Task GeneratedClientUsesTypedUtilityRoute()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<UtilityOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.UtilityOperations.UtilityOperationsClient(host.Channel);
        var result = await client.TrimLogFileAsync(new(), new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal("utility_operations.trim_log_file", Assert.Single(worker.Commands).OperationId);
        Assert.Equal(10, worker.Commands[0].InputArguments[0].RequireValue<WorkerIntegerValue>().Value);
    }

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
