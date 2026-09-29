using Briosa.Server.Operations;
using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedMeasurementDataOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.get_measurement_auxiliary_data",
        "analysis_operations.get_measurement_info_data",
        "analysis_operations.get_measurement_weather_data",
        "analysis_operations.set_measurement_auxiliary_data",
        "analysis_operations.temperature_compensate_a_group"
    ];

    [Fact]
    public void EachMeasurementHasOneTypedRegistrationAndPreservesValidationStatus()
    {
        foreach (var id in MigratedIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }

        Assert.Contains("fixture_validation_pending", GetMeasurementAuxiliaryDataOperation.Descriptor.RiskFlags);
        Assert.Contains("fixture_validation_pending", GetMeasurementInfoDataOperation.Descriptor.RiskFlags);
        Assert.Contains("fixture_validation_pending", GetMeasurementWeatherDataOperation.Descriptor.RiskFlags);
        Assert.Contains("fixture_validation_pending", SetMeasurementAuxiliaryDataOperation.Descriptor.RiskFlags);
    }

    [Fact]
    public void RequiredPointAndFrameNamesAndOptionalDefaultsMatchCatalog()
    {
        Assert.Throws<ArgumentException>(() => GetMeasurementAuxiliaryDataOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetMeasurementInfoDataOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetMeasurementWeatherDataOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetMeasurementAuxiliaryDataOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => TemperatureCompensateAGroupOperation.CreateCommand(new()));

        var point = new Api.PointName { CollectionName = "collection", GroupName = "group", TargetName = "P1" };
        var auxiliary = GetMeasurementAuxiliaryDataOperation.CreateCommand(new() { PointName = point });
        Assert.Equal("SetPointNameArg", auxiliary.InputArguments[0].SdkBinding);
        Assert.Equal("SetStringArg", auxiliary.InputArguments[1].SdkBinding);
        Assert.Equal(string.Empty, auxiliary.InputArguments[1].RequireValue<WorkerTextValue>().Value);

        var setAuxiliary = SetMeasurementAuxiliaryDataOperation.CreateCommand(new() { PointName = point });
        Assert.Equal(4, setAuxiliary.InputArguments.Count);
        Assert.Equal(string.Empty, setAuxiliary.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0, setAuxiliary.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(string.Empty, setAuxiliary.InputArguments[3].RequireValue<WorkerTextValue>().Value);

        var compensation = TemperatureCompensateAGroupOperation.CreateCommand(new()
        {
            OriginalGroup = new Api.CollectionObjectName { ObjectName = "original" },
            ScalingOrigin = new Api.FrameName { Name = "frame" },
            ScaledGroupName = new Api.CollectionObjectName { ObjectName = "scaled" }
        });
        Assert.Equal("SetCollectionObjectNameArg2", compensation.InputArguments[0].SdkBinding);
        Assert.Equal("SetFrameNameArg", compensation.InputArguments[1].SdkBinding);
        Assert.Equal(0, compensation.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, compensation.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, compensation.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("SetCollectionObjectNameArg2", compensation.InputArguments[5].SdkBinding);
        Assert.Equal(Api.ReplaySafety.Unsafe, TemperatureCompensateAGroupOperation.Descriptor.ReplaySafety);
    }

    [Fact]
    public void MeasurementResultsPreserveUnitsWeatherAndCompletion()
    {
        var auxiliary = GetMeasurementAuxiliaryDataOperation.CreateResult(Completed(
            Output("Value", new WorkerDoubleValue(12.5)), Output("Units", new WorkerTextValue("mm"))));
        var info = GetMeasurementInfoDataOperation.CreateResult(Completed(
            Output("Info Data", new WorkerTextValue("station-a"))));
        var weather = GetMeasurementWeatherDataOperation.CreateResult(Completed(
            Output("Temperature (deg F)", new WorkerDoubleValue(68)),
            Output("Pressure (in. Hg)", new WorkerDoubleValue(29.92)),
            Output("Humidity (% RH)", new WorkerDoubleValue(50))));
        var set = SetMeasurementAuxiliaryDataOperation.CreateResult(Completed());
        var compensated = TemperatureCompensateAGroupOperation.CreateResult(Completed());

        Assert.Equal(12.5, auxiliary.Value);
        Assert.Equal("mm", auxiliary.Units);
        Assert.Equal("station-a", info.InfoData);
        Assert.Equal(68, weather.Temperature);
        Assert.Equal(29.92, weather.Pressure);
        Assert.Equal(50, weather.Humidity);
        Assert.NotNull(set.Execution);
        Assert.NotNull(compensated.Execution);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedMeasurementInfoRoute()
    {
        var worker = new MeasurementInfoWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var result = await new Api.AnalysisOperations.AnalysisOperationsClient(channel)
            .GetMeasurementInfoDataAsync(new()
            {
                PointName = new Api.PointName { CollectionName = "collection", GroupName = "group", TargetName = "P1" }
            }, new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));

        Assert.Equal("fake measurement info", result.InfoData);
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(MigratedIds[1], Assert.Single(worker.Commands).OperationId);
    }

    private static WorkerRetrievedOutput Output(string name, WorkerMpValue value) => new(
        name, value switch
        {
            WorkerDoubleValue => WorkerMpValueKind.FloatingPoint,
            WorkerTextValue => WorkerMpValueKind.Text,
            _ => throw new InvalidOperationException()
        }, value);

    private static SuccessfulOperationExecution Completed(params WorkerMpOutputValue[] outputs) => new(
        WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed"),
        new Api.MpExecutionDetails());

    private sealed class MeasurementInfoWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            Assert.Equal("Get Measurement Info Data", command.StepName);
            var outputs = new[] { Output("Info Data", new WorkerTextValue("fake measurement info")) };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
