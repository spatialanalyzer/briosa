using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class AddDoubleToNamedDoubleListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.add_double_to_named_double_list_variable", "Add Double to Named Double List Variable",
        "briosa.Variables", "AddDoubleToNamedDoubleListVariable", "/briosa.Variables/AddDoubleToNamedDoubleListVariable",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AddDoubleToNamedDoubleListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg"),
                new("Double Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.DoubleValue), "SetDoubleArg")
            ], []);
    }

    public static Api.AddDoubleToNamedDoubleListVariableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
