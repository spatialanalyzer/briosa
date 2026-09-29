using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class GetDefinedReportTagsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.get_defined_report_tags", "Get Defined Report Tags", "briosa.ReportingOperations",
        "GetDefinedReportTags", "/briosa.ReportingOperations/GetDefinedReportTags", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [new("defined_tags", "Defined Tags", WorkerMpValueKind.StringList)];

    public static WorkerMpCommand CreateCommand(Api.GetDefinedReportTagsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [],
            [new("Defined Tags", WorkerMpValueKind.StringList, "GetStringRefListArg")]);
    }

    public static Api.GetDefinedReportTagsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetDefinedReportTagsResult { Execution = completed.Details };
        result.DefinedTags.AddRange(completed.Execution.OutputValues[0]
            .RequireValue<WorkerStringListValue>().Values);
        return result;
    }
}
