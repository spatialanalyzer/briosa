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

public sealed class TypedInstrumentHardwareSettingOperationTests
{
    private static readonly double[] DefaultCteInputs = [0, 0, 0];
    private static readonly double[] ExplicitCteInputs = [0.000012, 68, 70];
    private static readonly double[] DefaultPcmmInputs = [0.001, 0.001, 0.001];
    private static readonly double[] ExplicitPcmmInputs = [0, 0.002, 0.003];
    private static readonly double[] DefaultTrackerInputs = [1, 0.001, 1, 0.001, 2.5, 0.0003];
    private static readonly double[] ExplicitTrackerInputs = [2, 0.002, 3, 0.003, 4, 0.004];

    private static readonly string[] OperationIds =
    [
        "instrument_operations.compute_cte_scale_factor",
        "instrument_operations.get_instrument_scale_factor",
        "instrument_operations.get_pcmm_instrument_xyz_uncertainties",
        "instrument_operations.get_tracker_edm_theodolite_uncertainties",
        "instrument_operations.get_wrtl_channel_and_status",
        "instrument_operations.set_absolute_instrument_scale_factor",
        "instrument_operations.set_multiply_instrument_scale_factor",
        "instrument_operations.set_pcmm_instrument_xyz_uncertainties",
        "instrument_operations.set_tracker_edm_theodolite_uncertainties",
        "instrument_operations.set_wrtl_channel"
    ];

    [Fact]
    public void HardwareAndScaleFactorOperationsAreRegisteredAsTypedOperations()
    {
        foreach (var operationId in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == operationId);
        }
    }

    [Fact]
    public void CommandsPreserveHardwareDefaultsAndExplicitOverrides()
    {
        var cte = ComputeCteScaleFactorOperation.CreateCommand(new());
        Assert.Equal(DefaultCteInputs, cte.InputArguments
            .Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));
        var cteOverride = ComputeCteScaleFactorOperation.CreateCommand(new()
        {
            MaterialCte = 0.000012,
            InitialTemperature = 68,
            FinalTemperature = 70
        });
        Assert.Equal(ExplicitCteInputs, cteOverride.InputArguments
            .Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));

        var pcmmDefaults = SetPcmmInstrumentXyzUncertaintiesOperation.CreateCommand(new()
        {
            Instrument = Instrument()
        });
        Assert.Equal(DefaultPcmmInputs, pcmmDefaults.InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));
        var pcmmOverrides = SetPcmmInstrumentXyzUncertaintiesOperation.CreateCommand(new()
        {
            Instrument = Instrument(),
            XUncertainty = 0,
            YUncertainty = 0.002,
            ZUncertainty = 0.003
        });
        Assert.Equal(ExplicitPcmmInputs, pcmmOverrides.InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));

        var trackerDefaults = SetTrackerEdmTheodoliteUncertaintiesOperation.CreateCommand(new()
        {
            Instrument = Instrument()
        });
        Assert.Equal(DefaultTrackerInputs, trackerDefaults.InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));
        var trackerOverrides = SetTrackerEdmTheodoliteUncertaintiesOperation.CreateCommand(new()
        {
            Instrument = Instrument(),
            ThetaDispersion = 2,
            ThetaThreshold = 0.002,
            PhiDispersion = 3,
            PhiThreshold = 0.003,
            Distance = 4,
            DistanceThreshold = 0.004
        });
        Assert.Equal(ExplicitTrackerInputs, trackerOverrides.InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));

        var absoluteScale = SetAbsoluteInstrumentScaleFactorOperation.CreateCommand(new()
        {
            Instrument = Instrument(),
            ScaleFactor = 1.25
        });
        var multiplyScale = SetMultiplyInstrumentScaleFactorOperation.CreateCommand(new()
        {
            Instrument = Instrument(),
            ScaleFactor = 0.5
        });
        Assert.Equal(1.25, absoluteScale.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.5, multiplyScale.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);

        var wrtl = SetWrtlChannelOperation.CreateCommand(new()
        {
            Instrument = Instrument(),
            Channel = 3
        });
        Assert.Equal(3, wrtl.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Throws<ArgumentException>(() => GetInstrumentScaleFactorOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetWrtlChannelOperation.CreateCommand(new()));
    }

    [Fact]
    public async Task GeneratedClientExercisesEveryTypedRouteAndMapsHardwareOutputs()
    {
        var worker = new HardwareSettingWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);

        var cte = await client.ComputeCteScaleFactorAsync(new()
        {
            MaterialCte = 0.00001,
            InitialTemperature = 68,
            FinalTemperature = 70
        });
        var scale = await client.GetInstrumentScaleFactorAsync(new() { Instrument = Instrument() });
        var pcmm = await client.GetPcmmInstrumentXyzUncertaintiesAsync(new() { Instrument = Instrument() });
        var tracker = await client.GetTrackerEdmTheodoliteUncertaintiesAsync(new() { Instrument = Instrument() });
        var wrtl = await client.GetWrtlChannelAndStatusAsync(new() { Instrument = Instrument() });
        await client.SetAbsoluteInstrumentScaleFactorAsync(new() { Instrument = Instrument(), ScaleFactor = 1.25 });
        await client.SetMultiplyInstrumentScaleFactorAsync(new() { Instrument = Instrument(), ScaleFactor = 0.5 });
        await client.SetPcmmInstrumentXyzUncertaintiesAsync(new() { Instrument = Instrument() });
        await client.SetTrackerEdmTheodoliteUncertaintiesAsync(new() { Instrument = Instrument() });
        await client.SetWrtlChannelAsync(new() { Instrument = Instrument(), Channel = 2 });

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(1.00002, cte.ScaleFactor);
        Assert.Equal(1.5, scale.ScaleFactor);
        Assert.Equal(0.01, pcmm.XUncertainty);
        Assert.Equal(0.02, pcmm.YUncertainty);
        Assert.Equal(0.03, pcmm.ZUncertainty);
        Assert.Equal(1, tracker.ThetaDispersion);
        Assert.Equal(2, tracker.ThetaThreshold);
        Assert.Equal(3, tracker.PhiDispersion);
        Assert.Equal(4, tracker.PhiThreshold);
        Assert.Equal(5, tracker.Distance);
        Assert.Equal(6, tracker.DistanceThreshold);
        Assert.True(wrtl.Status.ConnectionStatus);
        Assert.Equal(5, wrtl.Status.ActiveChannel);
        Assert.Equal(0.001, worker.Commands[7].InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(2, worker.Commands[^1].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
    }

    private static Api.CollectionInstrumentId Instrument() => new()
    {
        CollectionName = "Trackers",
        InstrumentId = 4
    };

    private sealed class HardwareSettingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "instrument_operations.compute_cte_scale_factor" =>
                    [new WorkerRetrievedOutput("Scale Factor", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1.00002))],
                "instrument_operations.get_instrument_scale_factor" =>
                    [new WorkerRetrievedOutput("Scale Factor", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1.5))],
                "instrument_operations.get_pcmm_instrument_xyz_uncertainties" =>
                [
                    new WorkerRetrievedOutput("X Uncertainty", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.01)),
                    new WorkerRetrievedOutput("Y Uncertainty", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.02)),
                    new WorkerRetrievedOutput("Z Uncertainty", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.03))
                ],
                "instrument_operations.get_tracker_edm_theodolite_uncertainties" =>
                [
                    new WorkerRetrievedOutput("Theta Dispersion (arcseconds)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1)),
                    new WorkerRetrievedOutput("Theta Threshold (linear units)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2)),
                    new WorkerRetrievedOutput("Phi Dispersion (arcseconds)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(3)),
                    new WorkerRetrievedOutput("Phi Threshold (linear units)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(4)),
                    new WorkerRetrievedOutput("Distance (PPM)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(5)),
                    new WorkerRetrievedOutput("Distance Threshold (linear units)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(6))
                ],
                "instrument_operations.get_wrtl_channel_and_status" =>
                [
                    new WorkerRetrievedOutput("Connection Status", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("Active Channel", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(5))
                ],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
