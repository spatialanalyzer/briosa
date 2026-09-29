using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class SetReportOptionsForObjectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.set_report_options_for_object", "Set Report Options for Object", "briosa.ReportingOperations",
        "SetReportOptionsForObject", "/briosa.ReportingOperations/SetReportOptionsForObject", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetReportOptionsForObjectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Object", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Object, "object"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.SetReportOptionsForObjectResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
