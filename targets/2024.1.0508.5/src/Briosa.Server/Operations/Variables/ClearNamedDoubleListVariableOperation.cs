using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class ClearNamedDoubleListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.clear_named_double_list_variable", "Clear Named Double List Variable",
        "briosa.Variables", "ClearNamedDoubleListVariable", "/briosa.Variables/ClearNamedDoubleListVariable",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ClearNamedDoubleListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg")], []);
    }

    public static Api.ClearNamedDoubleListVariableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
