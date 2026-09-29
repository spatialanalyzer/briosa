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

public sealed class TypedLrApdisAndSelfTestOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.lr_apdis_activate_mcm_calibration",
        "instrument_operations.lr_apdis_get_active_mcm_calibration",
        "instrument_operations.lr_apdis_perform_mcm_calibration",
        "instrument_operations.lr_self_test",
        "instrument_operations.lr_self_test_flip_test",
        "instrument_operations.lr_self_test_linearization",
        "instrument_operations.lr_self_test_lo_sep",
        "instrument_operations.lr_set_red_laser_intensity"
    ];

    [Fact]
    public void OperationsAreRegisteredAsTypedOperations()
    {
        foreach (var operationId in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == operationId);
        }
    }

    [Fact]
    public void CommandsPreserveOmittedOptionalInputsAndDefaults()
    {
        var instrument = Instrument();
        var omittedCalibration = LrApdisActivateMcmCalibrationOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Single(omittedCalibration.InputArguments);

        var explicitCalibration = LrApdisActivateMcmCalibrationOperation.CreateCommand(new()
        {
            Instrument = instrument,
            CalibrationName = "Calibration A",
            CalibrationId = 12
        });
        Assert.Equal(3, explicitCalibration.InputArguments.Count);
        Assert.Equal("Calibration A", explicitCalibration.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(12, explicitCalibration.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);

        var calibrationDefaults = LrApdisPerformMcmCalibrationOperation.CreateCommand(new()
        {
            Instrument = instrument,
            NominalGroup = NominalGroup()
        });
        Assert.True(calibrationDefaults.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(string.Empty, calibrationDefaults.InputArguments[3].RequireValue<WorkerTextValue>().Value);

        var loSepDefaults = LrSelfTestLoSepOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(0, loSepDefaults.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(0, loSepDefaults.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        var laserDefaults = LrSetRedLaserIntensityOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(0, laserDefaults.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientRoutesCommandsAndMapsResultsThroughFakeWorker()
    {
        var worker = new LrApdisAndSelfTestWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);

        var active = await client.LrApdisActivateMcmCalibrationAsync(new()
        {
            Instrument = Instrument(),
            CalibrationName = "Calibration A",
            CalibrationId = 12
        });
        var current = await client.LrApdisGetActiveMcmCalibrationAsync(new() { Instrument = Instrument() });
        var performed = await client.LrApdisPerformMcmCalibrationAsync(new()
        {
            Instrument = Instrument(),
            NominalGroup = NominalGroup(),
            UseMatteToolingBall = false,
            NewCalibrationName = "Calibration B"
        });
        var selfTest = await client.LrSelfTestAsync(new() { Instrument = Instrument() });
        var flipTest = await client.LrSelfTestFlipTestAsync(new() { Instrument = Instrument() });
        var linearity = await client.LrSelfTestLinearizationAsync(new() { Instrument = Instrument() });
        var loSeparation = await client.LrSelfTestLoSepAsync(new()
        {
            Instrument = Instrument(),
            Region = 2,
            NumRangeMeasurements = 5
        });
        var laser = await client.LrSetRedLaserIntensityAsync(new() { Instrument = Instrument(), Intensity = 42 });

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal("MCM-1", active.ActiveMcmName);
        Assert.Equal("MCM-1", current.ActiveMcmName);
        Assert.NotNull(performed.Execution);
        Assert.Equal(1.25, selfTest.ReferenceArmLength);
        Assert.Equal(9, selfTest.MirrorMeasurementCount);
        Assert.True(selfTest.PassedOverall);
        Assert.Equal(1, flipTest.Result.FrontRange);
        Assert.Equal(11, flipTest.Result.FrontBackDifferenceElevation);
        Assert.Equal(9600, linearity.Linearity);
        Assert.Equal(1, loSeparation.Result.PrimaryLo);
        Assert.Equal(12, loSeparation.Result.SecondaryLoQualityStandardDeviation);
        Assert.NotNull(laser.Execution);

        var performedInputs = worker.Commands[2].InputArguments;
        Assert.False(performedInputs[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("Calibration B", performedInputs[3].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(2, worker.Commands[6].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(5, worker.Commands[6].InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(42, worker.Commands[7].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
    }

    private static Api.CollectionInstrumentId Instrument() => new()
    {
        CollectionName = "Trackers",
        InstrumentId = 4
    };

    private static Api.CollectionObjectName NominalGroup() => new()
    {
        CollectionName = "Nominals",
        ObjectName = "Calibration Group",
        ObjectType = Api.ObjectType.PointGroup
    };

    private sealed class LrApdisAndSelfTestWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "instrument_operations.lr_apdis_activate_mcm_calibration" or
                "instrument_operations.lr_apdis_get_active_mcm_calibration" =>
                    [Text("Active MCM Name", "MCM-1")],
                "instrument_operations.lr_self_test" =>
                [
                    Double("Ref Arm Length (Inches)", 1.25),
                    Double("Ref Arm Quality", 2.5),
                    Integer("Mirror Measurement Count", 9),
                    Double("Mirror Measurement Range - Mean (Inches)", 4),
                    Double("Mirror Measurement Range - StdDev (Inches)", 5),
                    Double("Mirror Measurement Quality - Mean", 6),
                    Double("Mirror Measurement Quality - StdDev", 7),
                    Boolean("Passed Ref Arm Quality Threshold?", true),
                    Boolean("Passed Mirror Offset Delta Threshold?", true),
                    Boolean("Passed Mirror Offset StdDev Threshold?", true),
                    Boolean("Passed Mirror Mean Quality Threshold?", true),
                    Boolean("Passed Overall?", true)
                ],
                "instrument_operations.lr_self_test_flip_test" =>
                [
                    Double("Front Measurement - Range (Inches)", 1),
                    Double("Front Measurement - Azimuth (Degs)", 2),
                    Double("Front Measurement - Elevation (Degs)", 3),
                    Double("Front Measurement - Quality", 4),
                    Double("Back Measurement - Range (Inches)", 5),
                    Double("Back Measurement - Azimuth (Degs)", 6),
                    Double("Back Measurement - Elevation (Degs)", 7),
                    Double("Back Measurement - Quality", 8),
                    Double("Front/Back Difference - Range (Inches)", 9),
                    Double("Front/Back Difference - Azimuth (Degs)", 10),
                    Double("Front/Back Difference - Elevation (Degs)", 11)
                ],
                "instrument_operations.lr_self_test_linearization" => [Double("Linearity (kHz)", 9600)],
                "instrument_operations.lr_self_test_lo_sep" =>
                [
                    Integer("Primary LO (indexed from 1)", 1),
                    Integer("Secondary LO (indexed from 1)", 2),
                    Integer("Primary LO Measurement Count", 3),
                    Double("Primary LO Measurement Range - Mean (Inches)", 4),
                    Double("Primary LO Measurement Range - StdDev (Inches)", 5),
                    Double("Primary LO Measurement Quality - Mean", 6),
                    Double("Primary LO Measurement Quality - StdDev", 7),
                    Integer("Secondary LO Measurement Count", 8),
                    Double("Secondary LO Measurement Range - Mean (Inches)", 9),
                    Double("Secondary LO Measurement Range - StdDev (Inches)", 10),
                    Double("Secondary LO Measurement Quality - Mean", 11),
                    Double("Secondary LO Measurement Quality - StdDev", 12)
                ],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }

        private static WorkerRetrievedOutput Text(string name, string value) =>
            new(name, WorkerMpValueKind.Text, new WorkerTextValue(value));

        private static WorkerRetrievedOutput Double(string name, double value) =>
            new(name, WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(value));

        private static WorkerRetrievedOutput Integer(string name, int value) =>
            new(name, WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(value));

        private static WorkerRetrievedOutput Boolean(string name, bool value) =>
            new(name, WorkerMpValueKind.Logical, new WorkerBooleanValue(value));
    }
}
