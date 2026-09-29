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

public sealed class TypedInstrumentScanModeAndAxesOperationTests
{
    private static readonly double[] AxisValues = [1.0, 2.0, 3.0];

    private static readonly string[] OperationIds =
    [
        "instrument_operations.enable_disable_frame_set_scan_mode_all_instruments",
        "instrument_operations.enable_disable_frame_set_scan_mode_by_instrument",
        "instrument_operations.enable_disable_point_set_scan_mode",
        "instrument_operations.set_instrument_axes",
        "instrument_operations.set_target_computation_options",
        "instrument_operations.set_xyz_reference_frame_instrument_base_anchor_frame"
    ];

    [Fact]
    public void ScanModeAndAxesCommandsPreserveEnumsDefaultsAndFrameTypes()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        Assert.True(EnableDisableFrameSetScanModeAllInstrumentsOperation.CreateCommand(new())
            .InputArguments[0].RequireValue<WorkerBooleanValue>().Value);
        var byInstrument = EnableDisableFrameSetScanModeByInstrumentOperation.CreateCommand(new() { Instrument = instrument });
        Assert.True(byInstrument.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(EnableDisableFrameSetScanModeByInstrumentOperation.CreateCommand(new()
        {
            Instrument = instrument,
            EnableFrameSetScanMode = false
        }).InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(EnableDisablePointSetScanModeOperation.CreateCommand(new() { Instrument = instrument })
            .InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        var axes = SetInstrumentAxesOperation.CreateCommand(new()
        {
            InstrumentToAdjust = instrument,
            AxisValues = { 1.0, 2.0, 3.0 }
        });
        Assert.Equal(AxisValues, axes.InputArguments[1].RequireValue<WorkerDoubleArrayValue>().Values);
        Assert.Equal(0, axes.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);

        var targetOptions = SetTargetComputationOptionsOperation.CreateCommand(new()
        {
            ComputationMethod = Api.TargetComputationMethod.UseOnlyMostRecentShot
        });
        Assert.Equal(WorkerTargetComputationMethodValue.UseOnlyMostRecentShot,
            targetOptions.InputArguments[0].RequireValue<WorkerChoiceValue<WorkerTargetComputationMethodValue>>().Value);
        Assert.False(targetOptions.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => SetTargetComputationOptionsOperation.CreateCommand(new()));

        var anchor = SetXyzReferenceFrameInstrumentBaseAnchorFrameOperation.CreateCommand(new()
        {
            Instrument = instrument,
            AnchorFrame = new Api.CollectionObjectName { CollectionName = "Frames", ObjectName = "Base" }
        });
        Assert.Equal(WorkerObjectTypeValue.Frame,
            anchor.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedScanModeAndAxesRoutes()
    {
        var worker = new InstrumentWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };

        await client.EnableDisableFrameSetScanModeAllInstrumentsAsync(new() { EnableFrameSetScanMode = false }, options);
        await client.EnableDisableFrameSetScanModeByInstrumentAsync(new() { Instrument = instrument }, options);
        await client.EnableDisablePointSetScanModeAsync(new() { Instrument = instrument, EnablePointSetScanMode = false }, options);
        await client.SetInstrumentAxesAsync(new()
        {
            InstrumentToAdjust = instrument,
            AxisValues = { 0.1, 0.2, 0.3 },
            NumberOfSteps = 5
        }, options);
        await client.SetTargetComputationOptionsAsync(new()
        {
            ComputationMethod = Api.TargetComputationMethod.RemoveAllPriorShots,
            IgnoreDistanceMeasurements = true
        }, options);
        await client.SetXyzReferenceFrameInstrumentBaseAnchorFrameAsync(new()
        {
            Instrument = instrument,
            AnchorFrame = new Api.CollectionObjectName { CollectionName = "Frames", ObjectName = "Base" }
        }, options);

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.False(worker.Commands[0].InputArguments[0].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(worker.Commands[1].InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(worker.Commands[2].InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(5, worker.Commands[3].InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(WorkerTargetComputationMethodValue.RemoveAllPriorShots,
            worker.Commands[4].InputArguments[0].RequireValue<WorkerChoiceValue<WorkerTargetComputationMethodValue>>().Value);
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
