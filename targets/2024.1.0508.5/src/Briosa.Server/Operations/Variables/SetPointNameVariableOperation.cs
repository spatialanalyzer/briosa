using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class SetPointNameVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.set_point_name_variable", "Set Point Name Variable",
        "briosa.Variables", "SetPointNameVariable", "/briosa.Variables/SetPointNameVariable",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetPointNameVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg"),
             new("Value", WorkerMpValueKind.PointName, PointNameMapper.Required(request.Value, "value"), "SetPointNameArg")], []);
    }

    public static Api.SetPointNameVariableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
