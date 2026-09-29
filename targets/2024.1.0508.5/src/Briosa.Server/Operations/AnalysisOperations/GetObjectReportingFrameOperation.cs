using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetObjectReportingFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_object_reporting_frame", "Get Object Reporting Frame",
        "briosa.AnalysisOperations", "GetObjectReportingFrame",
        "/briosa.AnalysisOperations/GetObjectReportingFrame", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe,
        ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("reporting_frame", "Reporting Frame", WorkerMpValueKind.CollectionObjectName)];

    public static WorkerMpCommand CreateCommand(Api.GetObjectReportingFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Object Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ObjectName, "object_name"), "SetCollectionObjectNameArg2")],
            [new("Reporting Frame", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg")]);
    }

    public static Api.GetObjectReportingFrameResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        ReportingFrame = CollectionObjectNameMapper.ToProtocol(
            completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameValue>()),
        Execution = completed.Details
    };
}
