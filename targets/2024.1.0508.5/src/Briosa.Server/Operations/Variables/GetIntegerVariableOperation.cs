using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class GetIntegerVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.get_integer_variable", "Get Integer Variable",
        "briosa.Variables", "GetIntegerVariable", "/briosa.Variables/GetIntegerVariable",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.WholeNumber)];

    public static WorkerMpCommand CreateCommand(Api.GetIntegerVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg")],
            [new("Value", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }

    public static Api.GetIntegerVariableResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        Value = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value
    };
}
