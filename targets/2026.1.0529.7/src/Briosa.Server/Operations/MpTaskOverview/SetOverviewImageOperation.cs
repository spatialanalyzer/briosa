using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.MpTaskOverview;

internal static class SetOverviewImageOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "mp_task_overview.set_overview_image", "Set Overview Image",
        "briosa.MpTaskOverview", "SetOverviewImage", "/briosa.MpTaskOverview/SetOverviewImage",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.SetOverviewImageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Image Path", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.ImagePath, "image_path"), "SetFilePathArg")], []);
    }
    public static Api.SetOverviewImageResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
