using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class DeletePictureOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.delete_picture", "Delete Picture", "briosa.ReportingOperations",
        "DeletePicture", "/briosa.ReportingOperations/DeletePicture", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeletePictureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Picture Name", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.PictureName, "picture_name"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.DeletePictureResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
