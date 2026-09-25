using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class GetBooleanVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.get_boolean_variable", "Get Boolean Variable",
        "briosa.Variables", "GetBooleanVariable", "/briosa.Variables/GetBooleanVariable",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.Logical)];

    public static WorkerMpCommand CreateCommand(Api.GetBooleanVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg")],
            [new("Value", WorkerMpValueKind.Logical, "GetBoolArg")]);
    }

    public static Api.GetBooleanVariableResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        Value = completed.Execution.OutputValues[0].RequireValue<WorkerBooleanValue>().Value
    };
}
