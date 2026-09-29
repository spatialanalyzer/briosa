using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class SetBooleanVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.set_boolean_variable", "Set Boolean Variable",
        "briosa.Variables", "SetBooleanVariable", "/briosa.Variables/SetBooleanVariable",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetBooleanVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg"),
             new("Value", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.Value), "SetBoolArg")], []);
    }

    public static Api.SetBooleanVariableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
