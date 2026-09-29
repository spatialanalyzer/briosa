using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class SetCalibrationApplianceDataOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.set_calibration_appliance_data", "Set Calibration Appliance Data",
        "briosa.RobotOperations", "SetCalibrationApplianceData", "/briosa.RobotOperations/SetCalibrationApplianceData",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetCalibrationApplianceDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Real Values", WorkerMpValueKind.DoubleArray, new WorkerDoubleArrayValue(request.RealValues), "SetDoubleArrayArg")], []);
    }

    public static Api.SetCalibrationApplianceDataResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
