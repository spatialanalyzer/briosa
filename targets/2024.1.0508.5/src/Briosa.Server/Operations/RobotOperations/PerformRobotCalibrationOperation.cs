using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class PerformRobotCalibrationOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.perform_robot_calibration", "Perform Robot Calibration",
        "briosa.RobotOperations", "PerformRobotCalibration", "/briosa.RobotOperations/PerformRobotCalibration",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("metrics", "XYZ Max", WorkerMpValueKind.FloatingPoint),
        new("metrics", "XYZ Average", WorkerMpValueKind.FloatingPoint),
        new("metrics", "XYZ RMS", WorkerMpValueKind.FloatingPoint),
        new("metrics", "Orient Max", WorkerMpValueKind.FloatingPoint),
        new("metrics", "Orient Average", WorkerMpValueKind.FloatingPoint),
        new("metrics", "Orient RMS", WorkerMpValueKind.FloatingPoint),
        new("metrics", "Robustness", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.PerformRobotCalibrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Machine ID", WorkerMpValueKind.CollectionMachineId,
                    new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId), "SetColMachineIdArg"),
                new("Calibration Name", WorkerMpValueKind.Text, new WorkerTextValue(request.CalibrationName), "SetStringArg"),
                new("Set Current Base as Nominal?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasSetCurrentBaseAsNominal && request.SetCurrentBaseAsNominal), "SetBoolArg"),
                new("Show Interface", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasShowInterface && request.ShowInterface), "SetBoolArg"),
                new("Allowed Outlier Rejection Count", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.AllowedOutlierRejectionCount), "SetIntegerArg"),
                new("Allowable Maximum Error", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.AllowableMaximumError), "SetDoubleArg"),
                new("Allowable Average Error", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.AllowableAverageError), "SetDoubleArg")
            ],
            [
                new("XYZ Max", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("XYZ Average", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("XYZ RMS", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Orient Max", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Orient Average", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Orient RMS", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Robustness", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.PerformRobotCalibrationResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Metrics = RobotCalibrationMetricsMapper.ToProtocol(completed.Execution.OutputValues),
        Execution = completed.Details
    };
}