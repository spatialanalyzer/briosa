using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class GetReportTagValueOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.get_report_tag_value", "Get Report Tag Value", "briosa.ReportingOperations",
        "GetReportTagValue", "/briosa.ReportingOperations/GetReportTagValue", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("tag_value_as_string", "Tag Value As String", WorkerMpValueKind.Text),
        new("tag_value_as_integer", "Tag Value As Integer", WorkerMpValueKind.WholeNumber),
        new("tag_value_as_double", "Tag Value As Double", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetReportTagValueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Tag Name", WorkerMpValueKind.Text, new WorkerTextValue(request.TagName), "SetStringArg")],
        [
            new("Tag Value As String", WorkerMpValueKind.Text, "GetStringArg"),
            new("Tag Value As Integer", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("Tag Value As Double", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
        ]);
    }

    public static Api.GetReportTagValueResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Execution = completed.Details,
            TagValueAsString = values[0].RequireValue<WorkerTextValue>().Value,
            TagValueAsInteger = values[1].RequireValue<WorkerIntegerValue>().Value,
            TagValueAsDouble = values[2].RequireValue<WorkerDoubleValue>().Value
        };
    }
}
