using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class GetCalibrationApplianceIntegerValueOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.get_calibration_appliance_integer_value", "Get Calibration Appliance Integer Value",
        "briosa.RobotOperations", "GetCalibrationApplianceIntegerValue", "/briosa.RobotOperations/GetCalibrationApplianceIntegerValue",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("integer_value", "Integer Value", WorkerMpValueKind.WholeNumber)];

    public static WorkerMpCommand CreateCommand(Api.GetCalibrationApplianceIntegerValueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var index = request.HasIndexOffset ? request.IndexOffset : 0;
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Index Offset", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(index), "SetIntegerArg")],
            [new("Integer Value", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }

    public static Api.GetCalibrationApplianceIntegerValueResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        IntegerValue = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
        Execution = completed.Details
    };
}
