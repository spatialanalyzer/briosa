using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class SetScaleForPictureOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.set_scale_for_picture", "Set Scale for Picture", "briosa.ReportingOperations",
        "SetScaleForPicture", "/briosa.ReportingOperations/SetScaleForPicture", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetScaleForPictureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Picture Name", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.PictureName, "picture_name"), "SetCollectionObjectNameArg2"),
            new("Scale", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasScale ? request.Scale : 100d), "SetDoubleArg")
        ], []);
    }

    public static Api.SetScaleForPictureResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
