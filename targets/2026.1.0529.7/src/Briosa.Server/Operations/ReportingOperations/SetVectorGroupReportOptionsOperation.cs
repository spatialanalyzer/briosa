using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class SetVectorGroupReportOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.set_vector_group_report_options", "Set Vector Group Report Options", "briosa.ReportingOperations",
        "SetVectorGroupReportOptions", "/briosa.ReportingOperations/SetVectorGroupReportOptions", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetVectorGroupReportOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Vector Group", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroup, "vector_group"), "SetCollectionObjectNameArg2"),
            new("Report Options", WorkerMpValueKind.PointDeltaReportOptions,
                PointDeltaReportOptionsMapper.ToWorker(request.ReportOptions), "SetPointDeltaReportOptionsArg")
        ], []);
    }

    public static Api.SetVectorGroupReportOptionsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
