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

public sealed class TypedInstrumentMeasurementObservationOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.delete_measurement_observation",
        "instrument_operations.delete_measurements",
        "instrument_operations.get_number_of_observations_on_target",
        "instrument_operations.get_observation_info",
        "instrument_operations.move_measurement_observation"
    ];

    [Fact]
    public void ObservationCommandsPreserveDefaultsRiskAndNestedOutputShape()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var point = new Api.PointName { CollectionName = "Points", GroupName = "Measured", TargetName = "P1" };

        var deleteObservation = DeleteMeasurementObservationOperation.CreateCommand(new() { PointName = point });
        Assert.Equal(0, deleteObservation.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(deleteObservation.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("destructive", Assert.Single(DeleteMeasurementObservationOperation.Descriptor.RiskFlags));

        var deleteMeasurements = DeleteMeasurementsOperation.CreateCommand(new() { Instrument = instrument, PointName = point });
        Assert.False(deleteMeasurements.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("destructive", Assert.Single(DeleteMeasurementsOperation.Descriptor.RiskFlags));

        var count = GetNumberOfObservationsOnTargetOperation.CreateCommand(new() { Point = point });
        Assert.Equal(global::Briosa.OperationEffect.ReadOnly,
            GetNumberOfObservationsOnTargetOperation.Descriptor.Effect);
        Assert.Equal("GetIntegerArg", Assert.Single(count.OutputArguments).SdkBinding);

        var info = GetObservationInfoOperation.CreateCommand(new() { Point = point });
        Assert.Equal(0, info.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(9, info.OutputArguments.Count);
        Assert.Equal("GetVectorArg", info.OutputArguments[1].SdkBinding);

        var move = MoveMeasurementObservationOperation.CreateCommand(new()
        {
            SourcePointName = point,
            DestinationPointName = new Api.PointName { CollectionName = "Points", GroupName = "Measured", TargetName = "P2" }
        });
        Assert.Equal(0, move.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(move.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(move.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedMeasurementObservationRoutes()
    {
        var worker = new ObservationWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var point = new Api.PointName { CollectionName = "Points", GroupName = "Measured", TargetName = "P1" };

        await client.DeleteMeasurementObservationAsync(new() { PointName = point }, options);
        await client.DeleteMeasurementsAsync(new() { Instrument = instrument, PointName = point }, options);
        var count = await client.GetNumberOfObservationsOnTargetAsync(new() { Point = point }, options);
        var info = await client.GetObservationInfoAsync(new() { Point = point }, options);
        await client.MoveMeasurementObservationAsync(new()
        {
            SourcePointName = point,
            DestinationPointName = new Api.PointName { CollectionName = "Points", GroupName = "Measured", TargetName = "P2" }
        }, options);

        Assert.Equal(3, count.ObservationCount);
        Assert.Equal(4, info.Observation.Instrument.InstrumentId);
        Assert.Equal(new Api.ObservationSphericalValues { Distance = 10, Azimuth = 20, Elevation = 30 },
            info.Observation.SphericalValues);
        Assert.True(info.Observation.Active);
        Assert.Equal("2026-09-26T12:00:00Z", info.Observation.Timestamp);
        Assert.Equal([0.25, 70, 29.92, 45], [info.Observation.RmsError, info.Observation.Temperature,
            info.Observation.Pressure, info.Observation.RelativeHumidity]);
        Assert.Equal("probe", info.Observation.InfoData);
        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(0, worker.Commands[0].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(worker.Commands[0].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(worker.Commands[4].InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
    }

    private sealed class ObservationWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "instrument_operations.get_number_of_observations_on_target" =>
                    [new WorkerRetrievedOutput("Number of Shots", WorkerMpValueKind.WholeNumber,
                        new WorkerIntegerValue(3))],
                "instrument_operations.get_observation_info" =>
                [
                    new WorkerRetrievedOutput("Resulting Instrument", WorkerMpValueKind.CollectionInstrumentId,
                        new WorkerCollectionInstrumentIdValue("Trackers", 4)),
                    new WorkerRetrievedOutput("Resultant Vector", WorkerMpValueKind.Vector,
                        new WorkerVectorValue(10, 20, 30)),
                    new WorkerRetrievedOutput("Active?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("Timestamp", WorkerMpValueKind.Text,
                        new WorkerTextValue("2026-09-26T12:00:00Z")),
                    new WorkerRetrievedOutput("RMS Error", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.25)),
                    new WorkerRetrievedOutput("Temperature (deg F)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(70)),
                    new WorkerRetrievedOutput("Pressure (in. Hg)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(29.92)),
                    new WorkerRetrievedOutput("Humidity (% RH)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(45)),
                    new WorkerRetrievedOutput("Info Data", WorkerMpValueKind.Text, new WorkerTextValue("probe"))
                ],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
