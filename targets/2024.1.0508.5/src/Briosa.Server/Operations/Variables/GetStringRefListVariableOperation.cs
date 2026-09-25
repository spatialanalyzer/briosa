using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class GetStringRefListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.get_string_ref_list_variable", "Get String Ref List Variable",
        "briosa.Variables", "GetStringRefListVariable", "/briosa.Variables/GetStringRefListVariable",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.StringList)];

    public static WorkerMpCommand CreateCommand(Api.GetStringRefListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg")],
            [new("Value", WorkerMpValueKind.StringList, "GetStringRefListArg")]);
    }

    public static Api.GetStringRefListVariableResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        Value = { completed.Execution.OutputValues[0].RequireValue<WorkerStringListValue>().Values }
    };
}
