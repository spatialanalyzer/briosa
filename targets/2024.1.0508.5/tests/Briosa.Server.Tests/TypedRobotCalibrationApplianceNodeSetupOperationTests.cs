using Briosa.Server.Operations;
using Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedRobotCalibrationApplianceNodeSetupOperationTests
{
    private static readonly string[] OperationIds =
    [
        "robot_calibration_appliance_node_operations.add_calibration_appliance_node",
        "robot_calibration_appliance_node_operations.delete_calibration_appliance_node",
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_calibration_appliance_ip_address",
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_display_robot",
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_instrument",
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_instrument_dwell_time",
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_frame",
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_offset_transform",
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_point_group",
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_profile",
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_target"
    ];

    [Fact]
    public void SetupOperationsAreTypedRegisteredAndRemovedFromGenericCatalog()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Contains("destructive", DeleteCalibrationApplianceNodeOperation.Descriptor.RiskFlags);
    }

    [Fact]
    public void SetupCommandsPreserveReferencesDefaultsAndExactBindings()
    {
        var node = new Api.CollectionObjectName { CollectionName = "Calibration", ObjectName = "Node" };
        var add = AddCalibrationApplianceNodeOperation.CreateCommand(new() { CalibrationApplianceNodeToAdd = node });
        var delete = DeleteCalibrationApplianceNodeOperation.CreateCommand(new() { CalibrationApplianceNodeToDelete = node });
        Assert.Equal(new WorkerCollectionObjectNameValue("Calibration", "Node", WorkerObjectTypeValue.Any),
            Assert.Single(add.InputArguments).RequireValue<WorkerCollectionObjectNameValue>());
        Assert.Equal("SetCollectionObjectNameArg2", Assert.Single(delete.InputArguments).SdkBinding);
        Assert.Throws<ArgumentException>(() => AddCalibrationApplianceNodeOperation.CreateCommand(new()));

        var defaultAddress = SetCalibrationApplianceNodeCalibrationApplianceIpAddressOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node
        });
        Assert.Equal("0.0.0.0", defaultAddress.InputArguments[1].RequireValue<WorkerTextValue>().Value);

        var displayRobot = SetCalibrationApplianceNodeDisplayRobotOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node,
            MachineId = new() { CollectionName = "Robots", MachineId = 3 }
        });
        Assert.Equal("SetColMachineIdArg", displayRobot.InputArguments[1].SdkBinding);
        Assert.Equal(new WorkerCollectionMachineIdValue("Robots", 3),
            displayRobot.InputArguments[1].RequireValue<WorkerCollectionMachineIdValue>());

        var instrument = SetCalibrationApplianceNodeInstrumentOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node,
            Instrument = new() { CollectionName = "Instruments", InstrumentId = 9 }
        });
        Assert.Equal("SetColInstIdArg", instrument.InputArguments[1].SdkBinding);
        Assert.Equal(new WorkerCollectionInstrumentIdValue("Instruments", 9),
            instrument.InputArguments[1].RequireValue<WorkerCollectionInstrumentIdValue>());

        var dwell = SetCalibrationApplianceNodeInstrumentDwellTimeOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node
        });
        Assert.Equal(0d, dwell.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);

        var frame = SetCalibrationApplianceNodeMeasurementFrameOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node,
            MeasurementReferenceFrame = new() { CollectionName = "Frames", ObjectName = "World" }
        });
        Assert.Equal(WorkerObjectTypeValue.Frame,
            frame.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);

        var pointGroup = SetCalibrationApplianceNodeMeasurementPointGroupOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node,
            PointGroupName = new() { CollectionName = "Points", ObjectName = "Targets" }
        });
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            pointGroup.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);

        var explicitFrameType = SetCalibrationApplianceNodeMeasurementFrameOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node,
            MeasurementReferenceFrame = new()
            {
                CollectionName = "Frames", ObjectName = "Fixture", ObjectType = Api.ObjectType.Cylinder
            }
        });
        Assert.Equal(WorkerObjectTypeValue.Cylinder,
            explicitFrameType.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);

        var transform = new Api.Transform { Values = { Enumerable.Range(0, 16).Select(index => (double)index) } };
        var offset = SetCalibrationApplianceNodeMeasurementOffsetTransformOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node,
            MeasurementOffsetTransform = transform
        });
        Assert.Equal("SetTransformArg", offset.InputArguments[1].SdkBinding);
        Assert.Equal(16, offset.InputArguments[1].RequireValue<WorkerTransformValue>().Values.Count);
        Assert.Throws<ArgumentException>(() => SetCalibrationApplianceNodeMeasurementOffsetTransformOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node,
            MeasurementOffsetTransform = new()
        }));

        var profile = SetCalibrationApplianceNodeMeasurementProfileOperation.CreateCommand(new() { CalibrationApplianceNode = node });
        var target = SetCalibrationApplianceNodeMeasurementTargetOperation.CreateCommand(new() { CalibrationApplianceNode = node });
        Assert.Equal(string.Empty, profile.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(string.Empty, target.InputArguments[1].RequireValue<WorkerTextValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientReachesEveryTypedSetupRoute()
    {
        var worker = new NodeWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RobotCalibrationApplianceNodeOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RobotCalibrationApplianceNodeOperations.RobotCalibrationApplianceNodeOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var node = new Api.CollectionObjectName { CollectionName = "Calibration", ObjectName = "Node" };

        await client.AddCalibrationApplianceNodeAsync(new() { CalibrationApplianceNodeToAdd = node }, options);
        await client.DeleteCalibrationApplianceNodeAsync(new() { CalibrationApplianceNodeToDelete = node }, options);
        await client.SetCalibrationApplianceNodeCalibrationApplianceIpAddressAsync(new()
        {
            CalibrationApplianceNode = node, CalibrationApplianceIpAddress = "192.0.2.12"
        }, options);
        await client.SetCalibrationApplianceNodeDisplayRobotAsync(new()
        {
            CalibrationApplianceNode = node, MachineId = new() { CollectionName = "Robots", MachineId = 3 }
        }, options);
        await client.SetCalibrationApplianceNodeInstrumentAsync(new()
        {
            CalibrationApplianceNode = node, Instrument = new() { CollectionName = "Instruments", InstrumentId = 9 }
        }, options);
        await client.SetCalibrationApplianceNodeInstrumentDwellTimeAsync(new()
        {
            CalibrationApplianceNode = node, MeasurementDwellTime = 0.25
        }, options);
        await client.SetCalibrationApplianceNodeMeasurementFrameAsync(new()
        {
            CalibrationApplianceNode = node,
            MeasurementReferenceFrame = new() { CollectionName = "Frames", ObjectName = "World" }
        }, options);
        await client.SetCalibrationApplianceNodeMeasurementOffsetTransformAsync(new()
        {
            CalibrationApplianceNode = node,
            MeasurementOffsetTransform = new() { Values = { Enumerable.Range(0, 16).Select(index => (double)index) } }
        }, options);
        await client.SetCalibrationApplianceNodeMeasurementPointGroupAsync(new()
        {
            CalibrationApplianceNode = node, PointGroupName = new() { CollectionName = "Points", ObjectName = "Targets" }
        }, options);
        await client.SetCalibrationApplianceNodeMeasurementProfileAsync(new()
        {
            CalibrationApplianceNode = node, MeasurementProfile = "Profile A"
        }, options);
        await client.SetCalibrationApplianceNodeMeasurementTargetAsync(new()
        {
            CalibrationApplianceNode = node, MeasurementTarget = "Target A"
        }, options);

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
    }

    private sealed class NodeWorker : IWorkerCommandExecutor
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
