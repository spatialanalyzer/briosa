using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class SetDoubleVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.set_double_variable", "Set Double Variable",
        "briosa.Variables", "SetDoubleVariable", "/briosa.Variables/SetDoubleVariable",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetDoubleVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new WorkerMpInputArgument("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), sdkBinding: "SetStringArg"),
                new WorkerMpInputArgument("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Value), sdkBinding: "SetDoubleArg")
            ], []);
    }

    public static Api.SetDoubleVariableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
