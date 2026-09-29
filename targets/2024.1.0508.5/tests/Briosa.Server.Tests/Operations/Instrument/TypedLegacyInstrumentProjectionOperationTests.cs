using Briosa.Server.Operations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedLegacyInstrumentProjectionOperationTests
{
    private static readonly string[] Ids =
    [
        "instrument_operations.run_crib_sheet",
        "instrument_operations.project_objects",
        "instrument_operations.stop_projection"
    ];

    [Fact]
    public void LegacyRoutesPreserveExactBindingsAndRequiredInputs()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, descriptor => descriptor.OperationId == id);
        }
        var instrument = new Api.CollectionInstrumentId { CollectionName = "C", InstrumentId = 2 };
        var crib = RunCribSheetOperation.CreateCommand(new()
        {
            Collection = new Api.CollectionName { Name = "C" },
            CribSheetName = "Check",
            Instrument = instrument
        });
        Assert.Equal("SetCollectionNameArg,SetStringArg,SetColInstIdArg",
            string.Join(',', crib.InputArguments.Select(argument => argument.SdkBinding)));
        Assert.Contains("long-running", RunCribSheetOperation.Descriptor.RiskFlags);
        Assert.Throws<ArgumentException>(() => RunCribSheetOperation.CreateCommand(new()
            { Collection = new Api.CollectionName { Name = "C" }, Instrument = instrument }));
        var project = ProjectObjectsOperation.CreateCommand(new()
        {
            Instrument = instrument,
            ObjectsToProject = { new Api.CollectionObjectName { CollectionName = "C", ObjectName = "O" } }
        });
        Assert.Equal("SetCollectionObjectNameRefListArg", project.InputArguments[1].SdkBinding);
        Assert.Single(project.InputArguments[1].RequireValue<WorkerCollectionObjectNameListValue>().Values);
        Assert.Throws<ArgumentException>(() => ProjectObjectsOperation.CreateCommand(new() { Instrument = instrument }));
        Assert.Equal("SetColInstIdArg", Assert.Single(StopProjectionOperation.CreateCommand(new()
            { Instrument = instrument }).InputArguments).SdkBinding);
        Assert.Throws<ArgumentException>(() => StopProjectionOperation.CreateCommand(new()));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedProjectionRoute()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(host.Channel);
        var result = await client.StopProjectionAsync(new()
        {
            Instrument = new Api.CollectionInstrumentId { CollectionName = "C", InstrumentId = 2 }
        }, new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal("instrument_operations.stop_projection", Assert.Single(worker.Commands).OperationId);
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
