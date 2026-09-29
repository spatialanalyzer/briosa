using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakePointNameRefListRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_point_name_ref_list_runtime_select", "Make a Point Name Ref List - Runtime Select",
        "briosa.ConstructionOperations", "MakePointNameRefListRuntimeSelect", "/briosa.ConstructionOperations/MakePointNameRefListRuntimeSelect",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_point_name_list", "Resultant Point Name List", WorkerMpValueKind.PointNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakePointNameRefListRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("User Prompt", WorkerMpValueKind.Text, new WorkerTextValue(request.UserPrompt), "SetStringArg")],
            [new("Resultant Point Name List", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg")]);
    }

    public static Api.MakePointNameRefListRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakePointNameRefListRuntimeSelectResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerPointNameListValue>().Values)
            result.ResultantPointNameList.Add(PointNameMapper.ToProtocol(value));
        return result;
    }
}
