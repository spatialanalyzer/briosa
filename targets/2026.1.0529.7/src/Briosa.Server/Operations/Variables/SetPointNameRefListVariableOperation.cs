using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class SetPointNameRefListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.set_point_name_ref_list_variable", "Set Point Name Ref List Variable",
        "briosa.Variables", "SetPointNameRefListVariable", "/briosa.Variables/SetPointNameRefListVariable",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetPointNameRefListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg"),
             new("Value", WorkerMpValueKind.PointNameList, PointNameMapper.RequiredList(request.Value, "value"), "SetPointNameRefListArg")], []);
    }

    public static Api.SetPointNameRefListVariableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
