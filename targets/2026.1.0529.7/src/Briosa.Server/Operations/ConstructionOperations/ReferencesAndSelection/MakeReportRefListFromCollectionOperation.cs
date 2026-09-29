using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeReportRefListFromCollectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_report_ref_list_from_collection", "Make a Report Ref List from a Collection",
        "briosa.ConstructionOperations", "MakeReportRefListFromCollection",
        "/briosa.ConstructionOperations/MakeReportRefListFromCollection", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("report_list", "Report List", WorkerMpValueKind.CollectionItemNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakeReportRefListFromCollectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Collection Name", WorkerMpValueKind.CollectionName,
                CollectionNameMapper.Required(request.CollectionName, "collection_name"), "SetCollectionNameArg")],
            [new("Report List", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.MakeReportRefListFromCollectionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeReportRefListFromCollectionResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionItemNameListValue>().Values)
            result.ReportList.Add(CollectionItemNameMapper.ToProtocol(value));
        return result;
    }
}
