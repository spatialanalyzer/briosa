using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class GetPointNameRefListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.get_point_name_ref_list_variable", "Get Point Name Ref List Variable",
        "briosa.Variables", "GetPointNameRefListVariable", "/briosa.Variables/GetPointNameRefListVariable",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.PointNameList)];

    public static WorkerMpCommand CreateCommand(Api.GetPointNameRefListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg")],
            [new("Value", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg")]);
    }

    public static Api.GetPointNameRefListVariableResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetPointNameRefListVariableResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerPointNameListValue>().Values)
        {
            result.Value.Add(PointNameMapper.ToProtocol(value));
        }
        return result;
    }
}
