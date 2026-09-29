using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class RenamePictureOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.rename_picture", "Rename Picture", "briosa.ReportingOperations",
        "RenamePicture", "/briosa.ReportingOperations/RenamePicture", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RenamePictureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Original Picture Name", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.OriginalPictureName, "original_picture_name"), "SetCollectionObjectNameArg2"),
            new("New Picture Name", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.NewPictureName, "new_picture_name"), "SetCollectionObjectNameArg2"),
            new("Overwrite if exists?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.OverwriteIfExists), "SetBoolArg")
        ], []);
    }

    public static Api.RenamePictureResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
