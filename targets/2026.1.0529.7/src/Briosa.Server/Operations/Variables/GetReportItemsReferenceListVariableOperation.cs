using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class GetReportItemsReferenceListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.get_report_items_reference_list_variable", "Get Report Items Reference List Variable",
        "briosa.Variables", "GetReportItemsReferenceListVariable", "/briosa.Variables/GetReportItemsReferenceListVariable",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.CollectionItemNameList)];

    public static WorkerMpCommand CreateCommand(Api.GetReportItemsReferenceListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg")],
            [new("Value", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.GetReportItemsReferenceListVariableResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetReportItemsReferenceListVariableResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionItemNameListValue>().Values)
        {
            result.Value.Add(CollectionItemNameMapper.ToProtocol(value));
        }
        return result;
    }
}
