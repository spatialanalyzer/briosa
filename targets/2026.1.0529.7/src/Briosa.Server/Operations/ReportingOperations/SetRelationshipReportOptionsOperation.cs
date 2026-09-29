using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class SetRelationshipReportOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.set_relationship_report_options", "Set Relationship Report Options", "briosa.ReportingOperations",
        "SetRelationshipReportOptions", "/briosa.ReportingOperations/SetRelationshipReportOptions", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipReportOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
            new("Report Options", WorkerMpValueKind.PointDeltaReportOptions,
                PointDeltaReportOptionsMapper.ToWorker(request.ReportOptions), "SetPointDeltaReportOptionsArg")
        ], []);
    }

    public static Api.SetRelationshipReportOptionsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
