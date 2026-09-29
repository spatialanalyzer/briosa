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

public sealed class TypedRobotCalibrationApplianceNodeExecutionOperationTests
{
    private static readonly string[] OperationIds =
    [
        "robot_calibration_appliance_node_operations.clear_calibration_appliance_node_trap_manager_requests",
        "robot_calibration_appliance_node_operations.connect_disconnect_calibration_appliance_node",
        "robot_calibration_appliance_node_operations.enable_disable_calibration_appliance_node_instrument_auto_point",
        "robot_calibration_appliance_node_operations.enable_disable_calibration_appliance_node_trap_manager",
        "robot_calibration_appliance_node_operations.get_calibration_appliance_node_data",
        "robot_calibration_appliance_node_operations.get_calibration_appliance_node_integer_value",
        "robot_calibration_appliance_node_operations.get_calibration_appliance_node_real_value",
        "robot_calibration_appliance_node_operations.get_calibration_appliance_node_status",
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_data",
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_integer_value",
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_real_value",
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_trapping_node_id",
        "robot_calibration_appliance_node_operations.skip_calibration_appliance_node_measurement",
        "robot_calibration_appliance_node_operations.update_calibration_appliance_node_display_robot_joints"
    ];

    [Fact]
    public void ExecutionAndResultOperationsAreTypedAndRemovedFromGenericCatalog()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }
    }

    [Fact]
    public void ExecutionCommandsPreserveDefaultsAndRetrievalSizing()
    {
        var node = new Api.CollectionObjectName { CollectionName = "Calibration", ObjectName = "Node" };
        var connect = ConnectDisconnectCalibrationApplianceNodeOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node
        });
        Assert.True(connect.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        var disconnect = ConnectDisconnectCalibrationApplianceNodeOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node, Connect = false
        });
        Assert.False(disconnect.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        Assert.True(EnableDisableCalibrationApplianceNodeInstrumentAutoPointOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node
        }).InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(EnableDisableCalibrationApplianceNodeTrapManagerOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node, Enable = false
        }).InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(UpdateCalibrationApplianceNodeDisplayRobotJointsOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node
        }).InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        var data = GetCalibrationApplianceNodeDataOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node, RealValueCount = 3
        });
        Assert.Empty(data.InputArguments.Skip(1));
        Assert.Equal(3, Assert.Single(data.OutputArguments).ArraySize);

        var defaultInteger = SetCalibrationApplianceNodeIntegerValueOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node
        });
        Assert.Equal([0, 0], defaultInteger.InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerIntegerValue>().Value));
        var defaultReal = SetCalibrationApplianceNodeRealValueOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node
        });
        Assert.Equal(0, defaultReal.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(0d, defaultReal.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, SetCalibrationApplianceNodeTrappingNodeIdOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node
        }).InputArguments[1].RequireValue<WorkerIntegerValue>().Value);

        var emptyData = SetCalibrationApplianceNodeDataOperation.CreateCommand(new()
        {
            CalibrationApplianceNode = node
        });
        Assert.Empty(emptyData.InputArguments[1].RequireValue<WorkerDoubleArrayValue>().Values);
        Assert.Throws<ArgumentException>(() => GetCalibrationApplianceNodeStatusOperation.CreateCommand(new()));
    }

    [Fact]
    public async Task GeneratedClientExecutesTypedCommandsAndMapsNodeResults()
    {
        var worker = new NodeWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RobotCalibrationApplianceNodeOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RobotCalibrationApplianceNodeOperations.RobotCalibrationApplianceNodeOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var node = new Api.CollectionObjectName { CollectionName = "Calibration", ObjectName = "Node" };

        await client.ClearCalibrationApplianceNodeTrapManagerRequestsAsync(new() { CalibrationApplianceNode = node }, options);
        await client.ConnectDisconnectCalibrationApplianceNodeAsync(new() { CalibrationApplianceNode = node, Connect = false }, options);
        await client.EnableDisableCalibrationApplianceNodeInstrumentAutoPointAsync(new() { CalibrationApplianceNode = node }, options);
        await client.EnableDisableCalibrationApplianceNodeTrapManagerAsync(new() { CalibrationApplianceNode = node, Enable = false }, options);
        var data = await client.GetCalibrationApplianceNodeDataAsync(new()
        {
            CalibrationApplianceNode = node, RealValueCount = 3
        }, options);
        var integer = await client.GetCalibrationApplianceNodeIntegerValueAsync(new()
        {
            CalibrationApplianceNode = node, IndexOffset = 4
        }, options);
        var real = await client.GetCalibrationApplianceNodeRealValueAsync(new()
        {
            CalibrationApplianceNode = node, IndexOffset = 5
        }, options);
        var status = await client.GetCalibrationApplianceNodeStatusAsync(new() { CalibrationApplianceNode = node }, options);
        await client.SetCalibrationApplianceNodeDataAsync(new()
        {
            CalibrationApplianceNode = node, RealValues = { 1.25, 2.5 }
        }, options);
        await client.SetCalibrationApplianceNodeIntegerValueAsync(new()
        {
            CalibrationApplianceNode = node, IndexOffset = 6, IntegerValue = 17
        }, options);
        await client.SetCalibrationApplianceNodeRealValueAsync(new()
        {
            CalibrationApplianceNode = node, IndexOffset = 7, RealValue = 3.5
        }, options);
        await client.SetCalibrationApplianceNodeTrappingNodeIdAsync(new()
        {
            CalibrationApplianceNode = node, TrappingNodeId = 8
        }, options);
        await client.SkipCalibrationApplianceNodeMeasurementAsync(new() { CalibrationApplianceNode = node }, options);
        await client.UpdateCalibrationApplianceNodeDisplayRobotJointsAsync(new()
        {
            CalibrationApplianceNode = node, EnableDisplayRobotJointUpdates = false
        }, options);

        Assert.Equal([1d, 2d, 3d], data.RealValues);
        Assert.Equal(17, integer.IntegerValue);
        Assert.Equal(2.5, real.RealValue);
        Assert.True(status.InstrumentConnected);
        Assert.False(status.CalibrationApplianceConnected);
        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(4, worker.Commands[5].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(5, worker.Commands[6].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(3, worker.Commands[4].OutputArguments[0].ArraySize);
    }

    private sealed class NodeWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "robot_calibration_appliance_node_operations.get_calibration_appliance_node_data" =>
                    [new WorkerRetrievedOutput("Real Values", WorkerMpValueKind.DoubleArray, new WorkerDoubleArrayValue([1, 2, 3]))],
                "robot_calibration_appliance_node_operations.get_calibration_appliance_node_integer_value" =>
                    [new WorkerRetrievedOutput("Integer Value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(17))],
                "robot_calibration_appliance_node_operations.get_calibration_appliance_node_real_value" =>
                    [new WorkerRetrievedOutput("Real Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2.5))],
                "robot_calibration_appliance_node_operations.get_calibration_appliance_node_status" =>
                [
                    new WorkerRetrievedOutput("Instrument Connected?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("Calibration Appliance Connected?", WorkerMpValueKind.Logical, new WorkerBooleanValue(false))
                ],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
