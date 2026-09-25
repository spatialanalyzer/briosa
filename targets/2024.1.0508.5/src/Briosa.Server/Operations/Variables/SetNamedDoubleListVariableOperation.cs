using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class SetNamedDoubleListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.set_named_double_list_variable", "Set Named Double List Variable",
        "briosa.Variables", "SetNamedDoubleListVariable", "/briosa.Variables/SetNamedDoubleListVariable",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetNamedDoubleListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Preserve the reviewed required-list behavior. The current public repeated
        // field cannot distinguish absence from an explicitly empty list.
        if (request.DoubleListVariable.Count == 0)
        {
            throw new ArgumentException("Double List Variable is required.", nameof(request));
        }

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Name", WorkerMpValueKind.Text, StringValue: request.Name, SdkBinding: "SetStringArg"),
                new("Double List Variable", WorkerMpValueKind.DoubleArray,
                    DoubleArrayValue: new(request.DoubleListVariable), SdkBinding: "SetDoubleArrayArg")
            ], []);
    }

    public static Api.SetNamedDoubleListVariableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
