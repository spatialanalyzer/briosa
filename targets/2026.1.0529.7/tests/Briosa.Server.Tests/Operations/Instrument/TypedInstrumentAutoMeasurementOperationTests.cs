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

public sealed class TypedInstrumentAutoMeasurementOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.auto_measure_batch_of_features",
        "instrument_operations.auto_measure_points",
        "instrument_operations.auto_measure_specified_geometry",
        "instrument_operations.auto_measure_surface_vector_intersections",
        "instrument_operations.auto_measure_vectors"
    ];

    [Fact]
    public void AutoMeasurementCommandsPreserveExactBindingsAndDefaults()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var batch = AutoMeasureBatchOfFeaturesOperation.CreateCommand(new()
        {
            Instrument = instrument,
            Features = { new Api.CollectionItemName { CollectionName = "Features", ItemName = "Circle" } }
        });
        var feature = Assert.Single(batch.InputArguments[1].RequireValue<WorkerCollectionObjectNameListValue>().Values);
        Assert.Equal("Circle", feature.ObjectName);
        Assert.Equal(WorkerObjectTypeValue.Any, feature.ObjectType);
        Assert.True(batch.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => AutoMeasureBatchOfFeaturesOperation.CreateCommand(new()
        {
            Instrument = instrument,
            Features = { new Api.CollectionItemName { ItemName = "Circle", ItemType = (Api.ItemType)99 } }
        }));

        var points = AutoMeasurePointsOperation.CreateCommand(new()
        {
            Instrument = instrument,
            ReferenceGroup = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Nominal" },
            ActualsGroup = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Measured" }
        });
        Assert.Equal(
            [false, false, true, false],
            points.InputArguments.Skip(3).Select(argument => argument.RequireValue<WorkerBooleanValue>().Value));
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            points.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);

        var geometry = AutoMeasureSpecifiedGeometryOperation.CreateCommand(new()
        {
            Instrument = instrument,
            Geometry = new Api.CollectionObjectName { CollectionName = "Models", ObjectName = "Part" }
        });
        Assert.Equal(WorkerObjectTypeValue.Any,
            geometry.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(string.Empty, geometry.InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.False(geometry.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);

        var intersections = AutoMeasureSurfaceVectorIntersectionsOperation.CreateCommand(new()
        {
            Instrument = instrument,
            VectorGroup = new Api.CollectionObjectName { CollectionName = "Vectors", ObjectName = "Nominal" },
            ResultantGroup = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Measured" }
        });
        Assert.Equal(WorkerObjectTypeValue.VectorGroup,
            intersections.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            intersections.InputArguments[2].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.True(intersections.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);

        var vectors = AutoMeasureVectorsOperation.CreateCommand(new()
        {
            Instrument = instrument,
            VectorGroup = new Api.CollectionObjectName { CollectionName = "Vectors", ObjectName = "Nominal" },
            ActualsGroup = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Measured" }
        });
        Assert.False(vectors.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal([0d, 0d, 0d], vectors.InputArguments.Skip(4)
            .Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedAutoMeasurementRoutes()
    {
        var worker = new AutoMeasurementWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };

        await client.AutoMeasureBatchOfFeaturesAsync(new()
        {
            Instrument = instrument,
            Features = { new Api.CollectionItemName { CollectionName = "Features", ItemName = "Circle" } }
        }, options);
        await client.AutoMeasurePointsAsync(new()
        {
            Instrument = instrument,
            ReferenceGroup = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Nominal" },
            ActualsGroup = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Measured" }
        }, options);
        await client.AutoMeasureSpecifiedGeometryAsync(new()
        {
            Instrument = instrument,
            Geometry = new Api.CollectionObjectName { CollectionName = "Models", ObjectName = "Part" }
        }, options);
        await client.AutoMeasureSurfaceVectorIntersectionsAsync(new()
        {
            Instrument = instrument,
            VectorGroup = new Api.CollectionObjectName { CollectionName = "Vectors", ObjectName = "Nominal" },
            ResultantGroup = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Measured" }
        }, options);
        await client.AutoMeasureVectorsAsync(new()
        {
            Instrument = instrument,
            VectorGroup = new Api.CollectionObjectName { CollectionName = "Vectors", ObjectName = "Nominal" },
            ActualsGroup = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Measured" }
        }, options);

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal("SetCollectionObjectNameRefListArg", worker.Commands[0].InputArguments[1].SdkBinding);
        Assert.True(worker.Commands[0].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(worker.Commands[1].InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(worker.Commands[3].InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
    }

    private sealed class AutoMeasurementWorker : IWorkerCommandExecutor
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
