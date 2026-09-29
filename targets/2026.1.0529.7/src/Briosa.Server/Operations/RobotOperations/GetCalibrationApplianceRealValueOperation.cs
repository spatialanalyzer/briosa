using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class GetCalibrationApplianceRealValueOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.get_calibration_appliance_real_value", "Get Calibration Appliance Real Value",
        "briosa.RobotOperations", "GetCalibrationApplianceRealValue", "/briosa.RobotOperations/GetCalibrationApplianceRealValue",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("real_value", "Real Value", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.GetCalibrationApplianceRealValueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var index = request.HasIndexOffset ? request.IndexOffset : 0;
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Index Offset", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(index), "SetIntegerArg")],
            [new("Real Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.GetCalibrationApplianceRealValueResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        RealValue = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
