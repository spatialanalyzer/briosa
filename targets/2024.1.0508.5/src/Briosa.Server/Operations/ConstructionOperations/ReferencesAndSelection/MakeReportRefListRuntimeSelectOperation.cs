using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeReportRefListRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_report_ref_list_runtime_select", "Make a Report Ref List - Runtime Select",
        "briosa.ConstructionOperations", "MakeReportRefListRuntimeSelect",
        "/briosa.ConstructionOperations/MakeReportRefListRuntimeSelect", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("report_list", "Report List", WorkerMpValueKind.CollectionItemNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakeReportRefListRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("User Prompt", WorkerMpValueKind.Text, new WorkerTextValue(request.UserPrompt), "SetStringArg")],
            [new("Report List", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.MakeReportRefListRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeReportRefListRuntimeSelectResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionItemNameListValue>().Values)
            result.ReportList.Add(CollectionItemNameMapper.ToProtocol(value));
        return result;
    }
}
