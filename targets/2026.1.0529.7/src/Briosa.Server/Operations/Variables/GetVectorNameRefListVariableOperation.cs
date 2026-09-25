using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class GetVectorNameRefListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.get_vector_name_ref_list_variable", "Get Vector Name Ref List Variable",
        "briosa.Variables", "GetVectorNameRefListVariable", "/briosa.Variables/GetVectorNameRefListVariable",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.VectorNameList)];

    public static WorkerMpCommand CreateCommand(Api.GetVectorNameRefListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg")],
            [new("Value", WorkerMpValueKind.VectorNameList, "GetVectorNameRefListArg")]);
    }

    public static Api.GetVectorNameRefListVariableResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetVectorNameRefListVariableResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerVectorNameListValue>().Values)
        {
            result.Value.Add(VectorNameMapper.ToProtocol(value));
        }
        return result;
    }
}
