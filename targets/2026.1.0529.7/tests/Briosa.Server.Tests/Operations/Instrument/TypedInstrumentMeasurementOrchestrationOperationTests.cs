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

public sealed class TypedInstrumentMeasurementOrchestrationOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.configure_and_measure",
        "instrument_operations.synchronized_measurement_master_slave",
        "instrument_operations.track_tape_measurement"
    ];

    [Fact]
    public void CommandsPreserveBindingsRequiredValuesAndDocumentedDefaults()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 3 };
        var point = new Api.PointName { CollectionName = "Points", GroupName = "Nominal", TargetName = "P1" };
        var configure = ConfigureAndMeasureOperation.CreateCommand(new() { Instrument = instrument, Target = point });
        Assert.Equal(
            ["SetColInstIdArg", "SetPointNameArg", "SetStringArg", "SetBoolArg", "SetBoolArg", "SetDoubleArg"],
            configure.InputArguments.Select(argument => argument.SdkBinding));
        Assert.Equal(string.Empty, configure.InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.False(configure.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(configure.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0d, configure.InputArguments[5].RequireValue<WorkerDoubleValue>().Value);

        var synchronized = SynchronizedMeasurementMasterSlaveOperation.CreateCommand(new()
        {
            MasterInstrument = instrument,
            SlaveInstrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 }
        });
        Assert.Equal("_Slave", synchronized.InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.True(synchronized.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(synchronized.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(synchronized.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);

        var trackTape = TrackTapeMeasurementOperation.CreateCommand(new()
        {
            Instrument = instrument,
            PointOnTape = point,
            PointOnPart = point,
            DirectionPoint = point,
            TerminationPoint = point,
            PointGroup = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Tape" }
        });
        Assert.Equal(8, trackTape.InputArguments.Count);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            trackTape.InputArguments[6].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(string.Empty, trackTape.InputArguments[5].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(string.Empty, trackTape.InputArguments[7].RequireValue<WorkerTextValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedMeasurementOrchestrationRoutes()
    {
        var worker = new MeasurementOrchestrationWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 3 };
        var slave = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var point = new Api.PointName { CollectionName = "Points", GroupName = "Nominal", TargetName = "P1" };

        await client.ConfigureAndMeasureAsync(new()
        {
            Instrument = instrument,
            Target = point,
            MeasureImmediately = true,
            WaitForCompletion = false,
            TimeoutSeconds = 12.5
        }, options);
        await client.SynchronizedMeasurementMasterSlaveAsync(new()
        {
            MasterInstrument = instrument,
            SlaveInstrument = slave
        }, options);
        await client.TrackTapeMeasurementAsync(new()
        {
            Instrument = instrument,
            PointOnTape = point,
            PointOnPart = point,
            DirectionPoint = point,
            TerminationPoint = point,
            PointGroup = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Tape" }
        }, options);

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.True(worker.Commands[0].InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(worker.Commands[0].InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(12.5, worker.Commands[0].InputArguments[5].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("_Slave", worker.Commands[1].InputArguments[2].RequireValue<WorkerTextValue>().Value);
    }

    private sealed class MeasurementOrchestrationWorker : IWorkerCommandExecutor
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
