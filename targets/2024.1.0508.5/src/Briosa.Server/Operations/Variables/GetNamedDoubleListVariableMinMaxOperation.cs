using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class GetNamedDoubleListVariableMinMaxOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.get_named_double_list_variable_min_max", "Get Named Double List Variable Min/Max",
        "briosa.Variables", "GetNamedDoubleListVariableMinMax", "/briosa.Variables/GetNamedDoubleListVariableMinMax",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("minimum_value", "Minimum Value", WorkerMpValueKind.FloatingPoint),
         new("maximum_value", "Maximum Value", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.GetNamedDoubleListVariableMinMaxRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg")],
            [new("Minimum Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
             new("Maximum Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.GetNamedDoubleListVariableMinMaxResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        MinimumValue = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        MaximumValue = completed.Execution.OutputValues[1].RequireValue<WorkerDoubleValue>().Value
    };
}
