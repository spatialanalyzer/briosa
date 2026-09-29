using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SetObjectsColorOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.set_objects_color", "Set Object(s) Color", "briosa.ViewControl",
        "SetObjectsColor", "/briosa.ViewControl/SetObjectsColor", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetObjectsColorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Objects to change", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ObjectsToChange, "objects_to_change"), "SetCollectionObjectNameRefListArg"),
            new("New Working Color Name", WorkerMpValueKind.RgbColor,
                ViewControlColorMapper.WithDefault(request.NewWorkingColorName), "SetColorArg"),
            new("Auto Increment", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.AutoIncrement), "SetBoolArg")
        ], []);
    }

    public static Api.SetObjectsColorResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
