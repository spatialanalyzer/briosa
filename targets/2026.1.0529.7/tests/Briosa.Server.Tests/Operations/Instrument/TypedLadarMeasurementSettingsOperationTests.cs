using Briosa.Server.Operations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedLadarMeasurementSettingsOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.set_ladar_auto_meas_point",
        "instrument_operations.set_ladar_auto_meas_sphere",
        "instrument_operations.set_ladar_feature_meas_circle",
        "instrument_operations.set_ladar_feature_meas_cylinder",
        "instrument_operations.set_ladar_feature_meas_slot",
        "instrument_operations.set_ladar_feature_meas_sphere"
    ];

    [Fact]
    public void LadarSettingsAreRegisteredAsTypedOperations()
    {
        foreach (var operationId in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == operationId);
        }
    }

    [Fact]
    public void CommandsPreserveLadarDocumentedDefaultsAndOverrides()
    {
        var instrument = Instrument();
        var point = SetLadarAutoMeasPointOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(0, point.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);

        var sphere = SetLadarAutoMeasSphereOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(1.1875, sphere.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.05, sphere.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.True(sphere.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(sphere.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(sphere.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);

        var circle = SetLadarFeatureMeasCircleOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(0.05, circle.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, circle.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        var cylinder = SetLadarFeatureMeasCylinderOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(0.05, cylinder.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, cylinder.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        var slot = SetLadarFeatureMeasSlotOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(0.05, slot.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, slot.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        var featureSphere = SetLadarFeatureMeasSphereOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(0.05, featureSphere.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);

        var overrides = SetLadarAutoMeasSphereOperation.CreateCommand(new()
        {
            Instrument = instrument,
            SphereRadius = 2.25,
            ScanLineSpacing = 0.1,
            SendCenterPoint = false,
            SendSphere = true,
            SendMeasuredCloud = true
        });
        Assert.Equal(2.25, overrides.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.1, overrides.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(overrides.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(overrides.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(overrides.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientRoutesAllLadarSettingsThroughFakeWorker()
    {
        var worker = new LadarSettingsWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);

        var point = await client.SetLadarAutoMeasPointAsync(new() { Instrument = Instrument(), SampleTimeMilliseconds = 250 });
        var autoSphere = await client.SetLadarAutoMeasSphereAsync(new()
        {
            Instrument = Instrument(),
            SphereRadius = 2.25,
            ScanLineSpacing = 0.1,
            SendCenterPoint = false,
            SendSphere = true,
            SendMeasuredCloud = true
        });
        var circle = await client.SetLadarFeatureMeasCircleAsync(new() { Instrument = Instrument() });
        var cylinder = await client.SetLadarFeatureMeasCylinderAsync(new() { Instrument = Instrument() });
        var slot = await client.SetLadarFeatureMeasSlotAsync(new() { Instrument = Instrument() });
        var featureSphere = await client.SetLadarFeatureMeasSphereAsync(new() { Instrument = Instrument() });

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.NotNull(point.Execution);
        Assert.NotNull(autoSphere.Execution);
        Assert.NotNull(circle.Execution);
        Assert.NotNull(cylinder.Execution);
        Assert.NotNull(slot.Execution);
        Assert.NotNull(featureSphere.Execution);
        Assert.Equal(250, worker.Commands[0].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(2.25, worker.Commands[1].InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.1, worker.Commands[1].InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(worker.Commands[1].InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(worker.Commands[1].InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
    }

    private static Api.CollectionInstrumentId Instrument() => new()
    {
        CollectionName = "LADAR",
        InstrumentId = 2
    };

    private sealed class LadarSettingsWorker : IWorkerCommandExecutor
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
