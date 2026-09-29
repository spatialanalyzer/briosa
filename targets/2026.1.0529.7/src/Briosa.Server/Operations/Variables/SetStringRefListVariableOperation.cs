using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class SetStringRefListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.set_string_ref_list_variable", "Set String Ref List Variable",
        "briosa.Variables", "SetStringRefListVariable", "/briosa.Variables/SetStringRefListVariable",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetStringRefListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Value.Count == 0)
        {
            throw new ArgumentException("Request field 'value' is required.", nameof(request));
        }

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg"),
             new("Value", WorkerMpValueKind.StringList, new WorkerStringListValue(request.Value), "SetStringRefListArg")], []);
    }

    public static Api.SetStringRefListVariableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
