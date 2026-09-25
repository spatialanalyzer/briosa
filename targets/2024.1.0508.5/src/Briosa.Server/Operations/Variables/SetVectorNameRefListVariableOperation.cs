using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class SetVectorNameRefListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.set_vector_name_ref_list_variable", "Set Vector Name Ref List Variable",
        "briosa.Variables", "SetVectorNameRefListVariable", "/briosa.Variables/SetVectorNameRefListVariable",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetVectorNameRefListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg"),
             new("Value", WorkerMpValueKind.VectorNameList, VectorNameMapper.RequiredList(request.Value, "value"), "SetVectorNameRefListArg")], []);
    }

    public static Api.SetVectorNameRefListVariableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
