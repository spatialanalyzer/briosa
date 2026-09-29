using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class SetCalibrationApplianceRealValueOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.set_calibration_appliance_real_value", "Set Calibration Appliance Real Value",
        "briosa.RobotOperations", "SetCalibrationApplianceRealValue", "/briosa.RobotOperations/SetCalibrationApplianceRealValue",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetCalibrationApplianceRealValueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var index = request.HasIndexOffset ? request.IndexOffset : 0;
        var value = request.HasRealValue ? request.RealValue : 0d;
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Index Offset", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(index), "SetIntegerArg"),
                new("Real Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(value), "SetDoubleArg")
            ], []);
    }

    public static Api.SetCalibrationApplianceRealValueResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
