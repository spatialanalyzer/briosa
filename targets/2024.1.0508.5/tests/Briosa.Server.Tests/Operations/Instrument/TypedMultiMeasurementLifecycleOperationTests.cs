using Briosa.Server.Operations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedMultiMeasurementLifecycleOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.multi_measurement_initiate",
        "instrument_operations.multi_measurement_stop"
    ];

    [Fact]
    public void MultiMeasurementCommandsPreserveRequiredInstrumentListsAndDefaults()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instruments = new[]
        {
            new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 },
            new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 5 }
        };
        var initiate = MultiMeasurementInitiateOperation.CreateCommand(new()
        {
            Instruments = { instruments[0], instruments[1] }
        });
        Assert.Equal([4, 5], initiate.InputArguments[0].RequireValue<WorkerCollectionInstrumentIdListValue>()
            .Values.Select(instrument => instrument.InstrumentId));
        Assert.Equal("SetColInstIdRefListArg", initiate.InputArguments[0].SdkBinding);
        Assert.Equal(string.Empty, initiate.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.False(initiate.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => MultiMeasurementInitiateOperation.CreateCommand(new()));

        var stop = MultiMeasurementStopOperation.CreateCommand(new()
        {
            Instruments = { instruments[0], instruments[1] }
        });
        Assert.Equal([4, 5], stop.InputArguments[0].RequireValue<WorkerCollectionInstrumentIdListValue>()
            .Values.Select(instrument => instrument.InstrumentId));
        Assert.Throws<ArgumentException>(() => MultiMeasurementStopOperation.CreateCommand(new()));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedMultiMeasurementRoutes()
    {
        var worker = new MultiMeasurementWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instruments = new[]
        {
            new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 },
            new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 5 }
        };

        await client.MultiMeasurementInitiateAsync(new()
        {
            Instruments = { instruments[0], instruments[1] }, MeasurementMode = "Laser", WaitForCompletion = true
        }, options);
        await client.MultiMeasurementStopAsync(new()
        {
            Instruments = { instruments[0], instruments[1] }
        }, options);

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal("Laser", worker.Commands[0].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.True(worker.Commands[0].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("SetColInstIdRefListArg", worker.Commands[1].InputArguments[0].SdkBinding);
    }

    private sealed class MultiMeasurementWorker : IWorkerCommandExecutor
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
