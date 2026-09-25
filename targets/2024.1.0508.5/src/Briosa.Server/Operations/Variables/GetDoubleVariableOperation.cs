using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class GetDoubleVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.get_double_variable", "Get Double Variable",
        "briosa.Variables", "GetDoubleVariable", "/briosa.Variables/GetDoubleVariable",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.GetDoubleVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new WorkerMpInputArgument("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), sdkBinding: "SetStringArg")],
            [new("Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    // OperationExecutor validates ordered output shape and retrieval before mapping.
    public static Api.GetDoubleVariableResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        Value = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value
    };
}
