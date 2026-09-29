using Briosa.Server.Operations;
using Briosa.Server.Operations.RobotOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedRobotMotionAndCalibrationOperationTests
{
    private static readonly string[] MetricOutputNames =
    ["XYZ Max", "XYZ Average", "XYZ RMS", "Orient Max", "Orient Average", "Orient RMS", "Robustness"];

    private static readonly string[] OperationIds =
    [
        "robot_operations.import_poses_match_to_frames",
        "robot_operations.import_poses_match_to_measurements",
        "robot_operations.move_robot_machine_through_path",
        "robot_operations.move_robot_machine_to_frame",
        "robot_operations.move_robot_machine_to_joint_pose_six_dof",
        "robot_operations.move_robot_machine_to_named_destination",
        "robot_operations.perform_robot_calibration_alternate",
        "robot_operations.perform_robot_calibration",
        "robot_operations.simulate_robot_machine_path_output_csv_file",
        "robot_operations.start_stop_robot_calibration_trapping"
    ];

    [Fact]
    public void RobotMotionAndCalibrationCommandsPreserveRequiredValuesAndDefaults()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var machine = new Api.CollectionMachineId { CollectionName = "Robots", MachineId = 12 };
        var frame = new Api.CollectionObjectName { CollectionName = "Frames", ObjectName = "Approach" };
        var point = new Api.PointName { CollectionName = "Points", GroupName = "Nominal", TargetName = "P1" };

        var importedFrames = ImportPosesMatchToFramesOperation.CreateCommand(new()
        {
            MachineId = machine,
            FrameNames = { frame }
        });
        Assert.Equal(3, importedFrames.InputArguments.Count);
        Assert.Equal("", importedFrames.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Single(importedFrames.InputArguments[2].RequireValue<WorkerCollectionObjectNameListValue>().Values);
        Assert.Throws<ArgumentException>(() => ImportPosesMatchToFramesOperation.CreateCommand(new()));

        var importedPoints = ImportPosesMatchToMeasurementsOperation.CreateCommand(new()
        {
            MachineId = machine,
            PointNames = { point }
        });
        Assert.Single(importedPoints.InputArguments[2].RequireValue<WorkerPointNameListValue>().Values);
        Assert.Equal("SetPointNameRefListArg", importedPoints.InputArguments[2].SdkBinding);

        var throughPath = MoveRobotMachineThroughPathOperation.CreateCommand(new()
        {
            MachineId = machine,
            PathFrames = { frame }
        });
        Assert.Equal([true, false, true], throughPath.InputArguments.Skip(2)
            .Select(argument => argument.RequireValue<WorkerBooleanValue>().Value));

        var toFrame = MoveRobotMachineToFrameOperation.CreateCommand(new() { MachineId = machine, DestinationFrame = frame });
        Assert.Equal(WorkerObjectTypeValue.Frame,
            toFrame.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal([false, false], toFrame.InputArguments.Skip(2)
            .Select(argument => argument.RequireValue<WorkerBooleanValue>().Value));
        Assert.Single(toFrame.OutputArguments);

        var jointPose = MoveRobotMachineToJointPoseSixDofOperation.CreateCommand(new() { MachineId = machine });
        Assert.Equal(7, jointPose.InputArguments.Count);
        Assert.All(jointPose.InputArguments.Skip(1), argument =>
            Assert.Equal(0d, argument.RequireValue<WorkerDoubleValue>().Value));

        var namedDestination = MoveRobotMachineToNamedDestinationOperation.CreateCommand(new() { MachineId = machine });
        Assert.Equal("", namedDestination.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.False(namedDestination.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);

        var calibration = PerformRobotCalibrationOperation.CreateCommand(new() { MachineId = machine });
        Assert.Equal(7, calibration.OutputArguments.Count);
        Assert.Equal(0, calibration.InputArguments[4].RequireValue<WorkerIntegerValue>().Value);
        var alternate = PerformRobotCalibrationAlternateOperation.CreateCommand(new() { MachineId = machine });
        Assert.Equal(10, alternate.InputArguments.Count);
        Assert.Equal(7, alternate.OutputArguments.Count);

        var simulatedPath = SimulateRobotMachinePathOutputCsvFileOperation.CreateCommand(new()
        {
            MachineId = machine,
            PathFrames = { frame }
        });
        Assert.Equal(2, simulatedPath.InputArguments.Count);

        var trapping = StartStopRobotCalibrationTrappingOperation.CreateCommand(new()
        {
            MachineId = machine,
            InstrumentId = new() { CollectionName = "Instruments", InstrumentId = 9 }
        });
        Assert.Equal("SetColInstIdArg", trapping.InputArguments[2].SdkBinding);
        Assert.False(trapping.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedRobotMotionAndCalibrationRoutes()
    {
        var worker = new RobotWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RobotOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RobotOperations.RobotOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var machine = new Api.CollectionMachineId { CollectionName = "Robots", MachineId = 12 };
        var frame = new Api.CollectionObjectName { CollectionName = "Frames", ObjectName = "Approach" };
        var destination = new Api.CollectionObjectName { CollectionName = "Frames", ObjectName = "Goal" };
        var point = new Api.PointName { CollectionName = "Points", GroupName = "Nominal", TargetName = "P1" };

        await client.ImportPosesMatchToFramesAsync(new() { MachineId = machine, CalibrationName = "Calibration A", FrameNames = { frame } }, options);
        await client.ImportPosesMatchToMeasurementsAsync(new() { MachineId = machine, CalibrationName = "Calibration A", PointNames = { point } }, options);
        await client.MoveRobotMachineThroughPathAsync(new()
        {
            MachineId = machine,
            PathFrames = { frame, destination },
            UseSaKinematics = true,
            LinearSegments = true,
            AcknowledgeArrival = true
        }, options);
        var toFrame = await client.MoveRobotMachineToFrameAsync(new() { MachineId = machine, DestinationFrame = destination }, options);
        await client.MoveRobotMachineToJointPoseSixDofAsync(new()
        {
            MachineId = machine, Joint1 = 1, Joint2 = 2, Joint3 = 3, Joint4 = 4, Joint5 = 5, Joint6 = 6
        }, options);
        var named = await client.MoveRobotMachineToNamedDestinationAsync(new() { MachineId = machine, DestinationName = "Home" }, options);
        var alternate = await client.PerformRobotCalibrationAlternateAsync(new()
        {
            MachineId = machine,
            CalibrationName = "Alternate",
            BaseDegreesOfFreedom = "XYZ",
            RobotDegreesOfFreedom = "XYZ",
            ToolDegreesOfFreedom = "XYZ",
            AllowedOutlierRejectionCount = 2,
            AllowableMaximumError = 0.5,
            AllowableAverageError = 0.2
        }, options);
        var calibration = await client.PerformRobotCalibrationAsync(new()
        {
            MachineId = machine,
            CalibrationName = "Standard",
            AllowedOutlierRejectionCount = 1,
            AllowableMaximumError = 0.4,
            AllowableAverageError = 0.1
        }, options);
        await client.SimulateRobotMachinePathOutputCsvFileAsync(new() { MachineId = machine, PathFrames = { frame } }, options);
        await client.StartStopRobotCalibrationTrappingAsync(new()
        {
            MachineId = machine,
            CalibrationName = "Standard",
            InstrumentId = new() { CollectionName = "Instruments", InstrumentId = 9 },
            StartTrapping = true
        }, options);

        Assert.Equal(Enumerable.Range(1, 16).Select(value => (double)value), toFrame.ActualTransformInWorking.Values);
        Assert.Equal(Enumerable.Range(1, 16).Select(value => (double)value), named.ActualTransformInWorking.Values);
        Assert.Equal(1, calibration.Metrics.XyzMax);
        Assert.Equal(7, calibration.Metrics.Robustness);
        Assert.Equal(1, alternate.Metrics.XyzMax);
        Assert.Equal(7, alternate.Metrics.Robustness);
        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(WorkerObjectTypeValue.Frame,
            worker.Commands[3].InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal([1d, 2d, 3d, 4d, 5d, 6d], worker.Commands[4].InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));
        Assert.Equal(2, worker.Commands[8].InputArguments.Count);
    }

    private sealed class RobotWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "robot_operations.move_robot_machine_to_frame" or
                "robot_operations.move_robot_machine_to_named_destination" =>
                    [new WorkerRetrievedOutput("Actual Transform In Working (result)", WorkerMpValueKind.Transform,
                        new WorkerTransformValue(Enumerable.Range(1, 16).Select(value => (double)value).ToArray()))],
                "robot_operations.perform_robot_calibration" or
                "robot_operations.perform_robot_calibration_alternate" =>
                    [.. MetricOutputNames
                        .Select((name, index) => new WorkerRetrievedOutput(name, WorkerMpValueKind.FloatingPoint,
                            new WorkerDoubleValue(index + 1)))],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}