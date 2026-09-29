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

public sealed class TypedInstrumentUncertaintyOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.construct_measured_point_uncertainty_ellipsoids",
        "instrument_operations.get_instrument_base_uncertainty_covariance_matrix_wrt_world",
        "instrument_operations.get_instrument_part_temperature",
        "instrument_operations.get_instrument_weather_setting",
        "instrument_operations.get_xyz_instrument_uncertainties",
        "instrument_operations.set_instrument_base_uncertainty_covariance_matrix_wrt_base",
        "instrument_operations.set_instrument_base_uncertainty_covariance_matrix_wrt_world",
        "instrument_operations.set_instrument_weather_setting",
        "instrument_operations.set_xyz_instrument_uncertainties"
    ];

    [Fact]
    public void Step43OperationsAreRegisteredAndRemovedFromTheGenericCatalog()
    {
        foreach (var operationId in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == operationId);
        }
    }

    [Fact]
    public void CommandsPreserveRequiredValuesAndCatalogDefaults()
    {
        var construct = ConstructMeasuredPointUncertaintyEllipsoidsOperation.CreateCommand(new()
        {
            Measurements = { new Api.PointName { CollectionName = "Parts", GroupName = "Measured", TargetName = "P1" } }
        });
        Assert.Equal(OperationIds[0], construct.OperationId);
        Assert.Equal("P1", construct.InputArguments[0].RequireValue<WorkerPointNameListValue>().Values[0].TargetName);
        Assert.Throws<ArgumentException>(() =>
            ConstructMeasuredPointUncertaintyEllipsoidsOperation.CreateCommand(new()));

        var matrix = CovarianceMatrix();
        var setBase = SetInstrumentBaseUncertaintyCovarianceMatrixWrtBaseOperation.CreateCommand(new()
        {
            Instrument = Instrument(),
            CovarianceMatrix = matrix
        });
        var setWorld = SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.CreateCommand(new()
        {
            Instrument = Instrument(),
            CovarianceMatrix = matrix
        });
        Assert.Equal(7, setBase.InputArguments.Count);
        Assert.Equal(OperationIds[5], setBase.OperationId);
        Assert.Equal(OperationIds[6], setWorld.OperationId);
        for (var row = 0; row < 6; row++)
        {
            var values = setBase.InputArguments[row + 1].RequireValue<WorkerDoubleArrayValue>().Values;
            Assert.Equal(new[] { row + 1.0, row + 1.25, row + 1.5, row + 1.75, row + 2.0, row + 2.25 }, values);
        }
        Assert.Throws<ArgumentException>(() =>
            SetInstrumentBaseUncertaintyCovarianceMatrixWrtBaseOperation.CreateCommand(new()
            {
                Instrument = Instrument()
            }));
        Assert.Throws<ArgumentException>(() =>
            GetInstrumentPartTemperatureOperation.CreateCommand(new()));

        var weatherDefaults = SetInstrumentWeatherSettingOperation.CreateCommand(new()
        {
            Instrument = Instrument()
        });
        Assert.Equal(0, weatherDefaults.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, weatherDefaults.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, weatherDefaults.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(weatherDefaults.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);

        var weatherOverrides = SetInstrumentWeatherSettingOperation.CreateCommand(new()
        {
            Instrument = Instrument(),
            Temperature = 70.5,
            Pressure = 740,
            RelativeHumidity = 42,
            SetAutomatically = true
        });
        Assert.Equal(70.5, weatherOverrides.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(740, weatherOverrides.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(42, weatherOverrides.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.True(weatherOverrides.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);

        var uncertaintyDefaults = SetXyzInstrumentUncertaintiesOperation.CreateCommand(new()
        {
            Instrument = Instrument()
        });
        Assert.Equal(0.0005, uncertaintyDefaults.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.0005, uncertaintyDefaults.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.0005, uncertaintyDefaults.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        var uncertaintyOverrides = SetXyzInstrumentUncertaintiesOperation.CreateCommand(new()
        {
            Instrument = Instrument(),
            XUncertainty = 0.1,
            YUncertainty = 0.2,
            ZUncertainty = 0.3
        });
        Assert.Equal(0.1, uncertaintyOverrides.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.2, uncertaintyOverrides.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.3, uncertaintyOverrides.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientExercisesEveryTypedRouteAndMapsOutputs()
    {
        var worker = new InstrumentUncertaintyWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);

        await client.ConstructMeasuredPointUncertaintyEllipsoidsAsync(new()
        {
            Measurements = { new Api.PointName { CollectionName = "Parts", GroupName = "Measured", TargetName = "P1" } }
        });
        var matrixResult = await client.GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldAsync(new()
        {
            Instrument = Instrument()
        });
        var partTemperature = await client.GetInstrumentPartTemperatureAsync(new() { Instrument = Instrument() });
        var weather = await client.GetInstrumentWeatherSettingAsync(new() { Instrument = Instrument() });
        var xyz = await client.GetXyzInstrumentUncertaintiesAsync(new() { Instrument = Instrument() });
        await client.SetInstrumentBaseUncertaintyCovarianceMatrixWrtBaseAsync(new()
        {
            Instrument = Instrument(),
            CovarianceMatrix = CovarianceMatrix()
        });
        await client.SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldAsync(new()
        {
            Instrument = Instrument(),
            CovarianceMatrix = CovarianceMatrix()
        });
        await client.SetInstrumentWeatherSettingAsync(new() { Instrument = Instrument() });
        await client.SetXyzInstrumentUncertaintiesAsync(new() { Instrument = Instrument() });

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(1, matrixResult.CovarianceMatrix.Row1.Values[0]);
        Assert.Equal(6, matrixResult.CovarianceMatrix.Row6.Values[0]);
        Assert.Equal(68.5, partTemperature.PartTemperature);
        Assert.Equal(71.25, weather.Temperature);
        Assert.Equal(742.5, weather.Pressure);
        Assert.Equal(45, weather.RelativeHumidity);
        Assert.True(weather.SetAutomatically);
        Assert.Equal(0.01, xyz.XUncertainty);
        Assert.Equal(0.02, xyz.YUncertainty);
        Assert.Equal(0.03, xyz.ZUncertainty);
        Assert.Equal(0.0005, worker.Commands[^1].InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
    }

    private static Api.CollectionInstrumentId Instrument() => new()
    {
        CollectionName = "Trackers",
        InstrumentId = 4
    };

    private static Api.UncertaintyCovarianceMatrix CovarianceMatrix() => new()
    {
        Row1 = Row(1),
        Row2 = Row(2),
        Row3 = Row(3),
        Row4 = Row(4),
        Row5 = Row(5),
        Row6 = Row(6)
    };

    private static Api.DoubleVector6 Row(double first) => new()
    {
        Values = { first, first + 0.25, first + 0.5, first + 0.75, first + 1, first + 1.25 }
    };

    private sealed class InstrumentUncertaintyWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var outputs = command.OperationId switch
            {
                "instrument_operations.get_instrument_base_uncertainty_covariance_matrix_wrt_world" =>
                    Enumerable.Range(1, 6)
                        .Select(row => (WorkerMpOutputValue)new WorkerRetrievedOutput($"Covar Row {row}",
                            WorkerMpValueKind.DoubleArray, new WorkerDoubleArrayValue([row])))
                        .ToArray(),
                "instrument_operations.get_instrument_part_temperature" =>
                    [new WorkerRetrievedOutput("Part Temperature (F)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(68.5))],
                "instrument_operations.get_instrument_weather_setting" =>
                [
                    new WorkerRetrievedOutput("Temperature (F)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(71.25)),
                    new WorkerRetrievedOutput("Pressure (mmHg)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(742.5)),
                    new WorkerRetrievedOutput("Humidity (%Rel)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(45)),
                    new WorkerRetrievedOutput("Was Set Automatically? (using Inst or external sensor", WorkerMpValueKind.Logical, new WorkerBooleanValue(true))
                ],
                "instrument_operations.get_xyz_instrument_uncertainties" =>
                [
                    new WorkerRetrievedOutput("X Uncertainty", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.01)),
                    new WorkerRetrievedOutput("Y Uncertainty", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.02)),
                    new WorkerRetrievedOutput("Z Uncertainty", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.03))
                ],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
