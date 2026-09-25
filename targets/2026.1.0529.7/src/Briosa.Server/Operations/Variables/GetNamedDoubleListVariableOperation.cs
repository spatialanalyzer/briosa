using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class GetNamedDoubleListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.get_named_double_list_variable", "Get Named Double List Variable",
        "briosa.Variables", "GetNamedDoubleListVariable", "/briosa.Variables/GetNamedDoubleListVariable",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("double_list_variable", "Double List Variable", WorkerMpValueKind.DoubleArray)];

    public static WorkerMpCommand CreateCommand(Api.GetNamedDoubleListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, StringValue: request.Name, SdkBinding: "SetStringArg")],
            [new("Double List Variable", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg")]);
    }

    public static Api.GetNamedDoubleListVariableResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues[0].DoubleArrayValue ??
            throw new InvalidOperationException("The double-list output is missing.");
        var result = new Api.GetNamedDoubleListVariableResult { Execution = completed.Details };
        result.DoubleListVariable.AddRange(values.Values);
        return result;
    }
}
