using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class SetObjectReportingFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.set_object_reporting_frame", "Set Object Reporting Frame",
        "briosa.AnalysisOperations", "SetObjectReportingFrame", "/briosa.AnalysisOperations/SetObjectReportingFrame",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetObjectReportingFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Object Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ObjectName, "object_name"), "SetCollectionObjectNameArg2"),
                new("Reporting Frame", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ReportingFrame, "reporting_frame"), "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.SetObjectReportingFrameResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
