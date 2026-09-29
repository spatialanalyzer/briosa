using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Operations.Values;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class CalibrationApplianceNodeOperation
{
    private const string Service = "briosa.RobotCalibrationApplianceNodeOperations";
    public static IReadOnlyList<OperationOutputContract> NoOutputs { get; } = [];

    public static OperationDescriptor MutatingDescriptor(
        string operationId,
        string step,
        string rpc,
        IReadOnlyList<string>? riskFlags = null) =>
        new(operationId, step, Service, rpc, $"/{Service}/{rpc}", "state_mutation",
            Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, riskFlags ?? []);

    public static OperationDescriptor ReadOnlyDescriptor(string operationId, string step, string rpc) =>
        new(operationId, step, Service, rpc, $"/{Service}/{rpc}", "read_only",
            Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static WorkerMpInputArgument NodeArgument(
        Api.CollectionObjectName? node,
        string mpArgumentName = "Calibration Appliance Node",
        string requestFieldName = "calibration_appliance_node") =>
        new(mpArgumentName, WorkerMpValueKind.CollectionObjectName,
            CollectionObjectNameMapper.Required(node, requestFieldName), "SetCollectionObjectNameArg2");

    public static WorkerMpInputArgument CollectionObjectArgument(
        string mpArgumentName,
        Api.CollectionObjectName? value,
        string requestFieldName,
        WorkerObjectTypeValue objectTypeWhenOmitted = WorkerObjectTypeValue.Any) =>
        new(mpArgumentName, WorkerMpValueKind.CollectionObjectName,
            CollectionObjectNameMapper.Required(value, requestFieldName, objectTypeWhenOmitted),
            "SetCollectionObjectNameArg2");

    public static WorkerMpCommand MutatingCommand(
        OperationDescriptor descriptor,
        WorkerMpInputArgument nodeArgument,
        params WorkerMpInputArgument[] remainingArguments) =>
        new(descriptor.OperationId, descriptor.MpStep, [nodeArgument, .. remainingArguments], []);

    public static WorkerMpCommand Command(
        OperationDescriptor descriptor,
        IReadOnlyList<WorkerMpInputArgument> inputArguments,
        IReadOnlyList<WorkerMpOutputArgument>? outputArguments = null) =>
        new(descriptor.OperationId, descriptor.MpStep, inputArguments, outputArguments ?? []);

    public static Api.MpExecutionDetails ExecutionDetails(SuccessfulOperationExecution completed) => completed.Details;
}
