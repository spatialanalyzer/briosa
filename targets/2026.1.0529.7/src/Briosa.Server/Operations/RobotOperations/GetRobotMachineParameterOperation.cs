using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class GetRobotMachineParameterOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.get_robot_machine_parameter", "Get Robot/Machine Parameter",
        "briosa.RobotOperations", "GetRobotMachineParameter", "/briosa.RobotOperations/GetRobotMachineParameter",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("parameter_value", "Parameter Value", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.GetRobotMachineParameterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Machine ID", WorkerMpValueKind.CollectionInstrumentId,
                    new WorkerCollectionInstrumentIdValue(request.MachineId.CollectionName, request.MachineId.InstrumentId),
                    "SetColInstIdArg"),
                new("Parameter Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.ParameterName), "SetStringArg")
            ],
            [new("Parameter Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.GetRobotMachineParameterResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        ParameterValue = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
