using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class CaptureCurrentViewOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.capture_current_view", "Capture Current View", "briosa.ReportingOperations",
        "CaptureCurrentView", "/briosa.ReportingOperations/CaptureCurrentView", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CaptureCurrentViewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Picture Name", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.PictureName, "picture_name"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.CaptureCurrentViewResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
