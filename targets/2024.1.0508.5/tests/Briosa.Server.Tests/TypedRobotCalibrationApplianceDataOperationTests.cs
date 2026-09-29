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

public sealed class TypedRobotCalibrationApplianceDataOperationTests
{
    private static readonly string[] OperationIds =
    [
        "robot_operations.get_calibration_appliance_data",
        "robot_operations.get_calibration_appliance_integer_value",
        "robot_operations.get_calibration_appliance_real_value",
        "robot_operations.set_calibration_appliance_data",
        "robot_operations.set_calibration_appliance_integer_value",
        "robot_operations.set_calibration_appliance_real_value"
    ];

    [Fact]
    public void CalibrationApplianceDataCommandsPreserveDefaultsAndArrayMetadata()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var getData = GetCalibrationApplianceDataOperation.CreateCommand(new() { RealValueCount = 3 });
        Assert.Empty(getData.InputArguments);
        Assert.Equal(3, Assert.Single(getData.OutputArguments).ArraySize);

        var getInteger = GetCalibrationApplianceIntegerValueOperation.CreateCommand(new());
        Assert.Equal(0, getInteger.InputArguments[0].RequireValue<WorkerIntegerValue>().Value);
        var getReal = GetCalibrationApplianceRealValueOperation.CreateCommand(new() { IndexOffset = 4 });
        Assert.Equal(4, getReal.InputArguments[0].RequireValue<WorkerIntegerValue>().Value);

        var setData = SetCalibrationApplianceDataOperation.CreateCommand(new());
        Assert.Empty(setData.InputArguments[0].RequireValue<WorkerDoubleArrayValue>().Values);
        var setInteger = SetCalibrationApplianceIntegerValueOperation.CreateCommand(new());
        Assert.Equal([0, 0], setInteger.InputArguments.Select(argument => argument.RequireValue<WorkerIntegerValue>().Value));
        var setReal = SetCalibrationApplianceRealValueOperation.CreateCommand(new());
        Assert.Equal(0, setReal.InputArguments[0].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(0d, setReal.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedCalibrationApplianceDataRoutes()
    {
        var worker = new RobotWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RobotOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RobotOperations.RobotOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var data = await client.GetCalibrationApplianceDataAsync(new() { RealValueCount = 3 }, options);
        var integer = await client.GetCalibrationApplianceIntegerValueAsync(new(), options);
        var real = await client.GetCalibrationApplianceRealValueAsync(new() { IndexOffset = 4 }, options);
        var setData = await client.SetCalibrationApplianceDataAsync(new() { RealValues = { 1, 2, 3 } }, options);
        var setInteger = await client.SetCalibrationApplianceIntegerValueAsync(new() { IndexOffset = 2, IntegerValue = 8 }, options);
        var setReal = await client.SetCalibrationApplianceRealValueAsync(new() { IndexOffset = 3, RealValue = 4.25 }, options);

        Assert.Equal([1d, 2d, 3d], data.RealValues);
        Assert.Equal(17, integer.IntegerValue);
        Assert.Equal(2.5, real.RealValue);
        Assert.NotNull(setData.Execution);
        Assert.NotNull(setInteger.Execution);
        Assert.NotNull(setReal.Execution);
        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(3, worker.Commands[0].OutputArguments[0].ArraySize);
        Assert.Equal([1d, 2d, 3d], worker.Commands[3].InputArguments[0].RequireValue<WorkerDoubleArrayValue>().Values);
        Assert.Equal([2, 8], worker.Commands[4].InputArguments.Select(argument => argument.RequireValue<WorkerIntegerValue>().Value));
        Assert.Equal(3, worker.Commands[5].InputArguments[0].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(4.25, worker.Commands[5].InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
    }

    private sealed class RobotWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "robot_operations.get_calibration_appliance_data" =>
                    [new WorkerRetrievedOutput("Real Values", WorkerMpValueKind.DoubleArray,
                        new WorkerDoubleArrayValue([1d, 2d, 3d]))],
                "robot_operations.get_calibration_appliance_integer_value" =>
                    [new WorkerRetrievedOutput("Integer Value", WorkerMpValueKind.WholeNumber,
                        new WorkerIntegerValue(17))],
                "robot_operations.get_calibration_appliance_real_value" =>
                    [new WorkerRetrievedOutput("Real Value", WorkerMpValueKind.FloatingPoint,
                        new WorkerDoubleValue(2.5))],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}