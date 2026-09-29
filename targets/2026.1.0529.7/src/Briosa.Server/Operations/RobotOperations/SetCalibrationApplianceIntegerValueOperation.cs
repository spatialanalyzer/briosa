using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class SetCalibrationApplianceIntegerValueOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.set_calibration_appliance_integer_value", "Set Calibration Appliance Integer Value",
        "briosa.RobotOperations", "SetCalibrationApplianceIntegerValue", "/briosa.RobotOperations/SetCalibrationApplianceIntegerValue",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetCalibrationApplianceIntegerValueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var index = request.HasIndexOffset ? request.IndexOffset : 0;
        var value = request.HasIntegerValue ? request.IntegerValue : 0;
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Index Offset", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(index), "SetIntegerArg"),
                new("Integer Value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(value), "SetIntegerArg")
            ], []);
    }

    public static Api.SetCalibrationApplianceIntegerValueResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
