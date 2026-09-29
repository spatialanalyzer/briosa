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

public sealed class TypedInstrumentTransformOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.transform_instrument_frame_to_frame",
        "instrument_operations.set_instrument_transform",
        "instrument_operations.get_instrument_transform"
    ];

    private static readonly double[] TransformValues = Enumerable.Range(0, 16).Select(value => (double)value).ToArray();

    [Fact]
    public void TransformCommandsKeepExactBindingsAndDefaults()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var initialFrame = new Api.CollectionObjectName { CollectionName = "Frames", ObjectName = "Initial" };
        var destinationFrame = new Api.CollectionObjectName { CollectionName = "Frames", ObjectName = "Destination" };
        var frameToFrame = TransformInstrumentFrameToFrameOperation.CreateCommand(new()
        {
            Instrument = instrument,
            InitialFrame = initialFrame,
            DestinationFrame = destinationFrame
        });
        Assert.Equal("SetColInstIdArg", frameToFrame.InputArguments[0].SdkBinding);
        Assert.Equal("SetCollectionObjectNameArg2", frameToFrame.InputArguments[1].SdkBinding);
        Assert.Equal(WorkerObjectTypeValue.Any,
            frameToFrame.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(0, frameToFrame.InputArguments[3].RequireValue<WorkerIntegerValue>().Value);

        var setTransform = SetInstrumentTransformOperation.CreateCommand(new()
        {
            Instrument = instrument,
            DestinationTransform = new() { Values = { TransformValues } },
            ReferenceFrame = destinationFrame,
            NumberOfSteps = 7
        });
        Assert.Equal(TransformValues, setTransform.InputArguments[1].RequireValue<WorkerTransformValue>().Values);
        Assert.Equal(WorkerObjectTypeValue.Frame,
            setTransform.InputArguments[2].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(7, setTransform.InputArguments[3].RequireValue<WorkerIntegerValue>().Value);

        var getTransform = GetInstrumentTransformOperation.CreateCommand(new()
        {
            Instrument = instrument,
            ReferenceFrame = destinationFrame
        });
        Assert.Equal(WorkerObjectTypeValue.Frame,
            getTransform.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal("GetTransformArg", Assert.Single(getTransform.OutputArguments).SdkBinding);
        Assert.Throws<ArgumentException>(() => SetInstrumentTransformOperation.CreateCommand(new() { Instrument = instrument }));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedTransformRoutes()
    {
        var worker = new InstrumentWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var referenceFrame = new Api.CollectionObjectName { CollectionName = "Frames", ObjectName = "Reference" };

        await client.TransformInstrumentFrameToFrameAsync(new()
        {
            Instrument = instrument,
            InitialFrame = new() { CollectionName = "Frames", ObjectName = "Initial" },
            DestinationFrame = referenceFrame
        }, options);
        await client.SetInstrumentTransformAsync(new()
        {
            Instrument = instrument,
            DestinationTransform = new() { Values = { TransformValues } },
            ReferenceFrame = referenceFrame
        }, options);
        var result = await client.GetInstrumentTransformAsync(new()
        {
            Instrument = instrument,
            ReferenceFrame = referenceFrame
        }, options);

        Assert.Equal(TransformValues, result.Transform.Values);
        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(WorkerObjectTypeValue.Frame,
            worker.Commands[1].InputArguments[2].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(0, worker.Commands[0].InputArguments[3].RequireValue<WorkerIntegerValue>().Value);
    }

    private sealed class InstrumentWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId == "instrument_operations.get_instrument_transform"
                ? [new WorkerRetrievedOutput("Transform", WorkerMpValueKind.Transform, new WorkerTransformValue(TransformValues))]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
