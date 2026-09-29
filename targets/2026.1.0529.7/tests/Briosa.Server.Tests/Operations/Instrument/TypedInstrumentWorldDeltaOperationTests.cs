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

public sealed class TypedInstrumentWorldDeltaOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.transform_instrument_by_delta",
        "instrument_operations.transform_multiple_instruments_by_delta"
    ];

    private static readonly double[] TransformValues = Enumerable.Range(0, 16).Select(value => (double)value).ToArray();

    [Fact]
    public void WorldDeltaCommandsPreserveScaleAndInstrumentDefaults()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var single = TransformInstrumentByDeltaOperation.CreateCommand(new()
        {
            Instrument = instrument,
            DeltaTransform = new() { Transform = new() { Values = { TransformValues } } }
        });
        var delta = single.InputArguments[1].RequireValue<WorkerWorldTransformValue>();
        Assert.Equal(TransformValues, delta.Transform.Values);
        Assert.Equal(1d, delta.ScaleFactor);
        Assert.False(single.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);

        var multiple = TransformMultipleInstrumentsByDeltaOperation.CreateCommand(new()
        {
            Instruments = { instrument, new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 5 } },
            DeltaTransform = new() { Transform = new() { Values = { TransformValues } }, ScaleFactor = 2.5 }
        });
        var instruments = multiple.InputArguments[0].RequireValue<WorkerCollectionInstrumentIdListValue>();
        Assert.Equal([4, 5], instruments.Values.Select(value => value.InstrumentId));
        Assert.Equal(2.5d, multiple.InputArguments[1].RequireValue<WorkerWorldTransformValue>().ScaleFactor);
        Assert.Throws<ArgumentException>(() => TransformMultipleInstrumentsByDeltaOperation.CreateCommand(new()
        {
            DeltaTransform = new() { Transform = new() { Values = { TransformValues } } }
        }));
        Assert.Throws<ArgumentException>(() => TransformInstrumentByDeltaOperation.CreateCommand(new() { Instrument = instrument }));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedWorldDeltaRoutes()
    {
        var worker = new InstrumentWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var transform = new Api.WorldTransform { Transform = new() { Values = { TransformValues } }, ScaleFactor = 1.5 };

        await client.TransformInstrumentByDeltaAsync(new() { Instrument = instrument, DeltaTransform = transform }, options);
        await client.TransformMultipleInstrumentsByDeltaAsync(new()
        {
            Instruments = { instrument, new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 5 } },
            DeltaTransform = transform,
            ApplyScaleToInstruments = true
        }, options);

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(1.5d, worker.Commands[0].InputArguments[1].RequireValue<WorkerWorldTransformValue>().ScaleFactor);
        Assert.True(worker.Commands[1].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
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
