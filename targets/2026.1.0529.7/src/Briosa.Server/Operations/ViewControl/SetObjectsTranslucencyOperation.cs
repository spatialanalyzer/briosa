using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SetObjectsTranslucencyOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.set_objects_translucency", "Set Object(s) Translucency", "briosa.ViewControl",
        "SetObjectsTranslucency", "/briosa.ViewControl/SetObjectsTranslucency", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetObjectsTranslucencyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.HasRenderingType)
        {
            throw RequestFieldViolation.Required("rendering_type");
        }

        var renderingType = request.RenderingType;
        if (!Enum.IsDefined(renderingType) || renderingType == Api.TranslucencyType.Unspecified)
        {
            throw RequestFieldViolation.SupportedChoice("rendering_type");
        }

        var workerRenderingType = renderingType switch
        {
            Api.TranslucencyType.Solid => WorkerTranslucencyTypeValue.Solid,
            Api.TranslucencyType.Translucent => WorkerTranslucencyTypeValue.Translucent,
            Api.TranslucencyType.Wireframe => WorkerTranslucencyTypeValue.Wireframe,
            _ => throw RequestFieldViolation.SupportedChoice("rendering_type")
        };
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Objects to change", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ObjectsToChange, "objects_to_change"), "SetCollectionObjectNameRefListArg"),
            new("Rendering Type", WorkerMpValueKind.TranslucencyType,
                new WorkerChoiceValue<WorkerTranslucencyTypeValue>(workerRenderingType), "SetTranslucencyTypeArg"),
            new("Opacity Value", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasOpacityValue ? request.OpacityValue : 0d), "SetDoubleArg")
        ], []);
    }

    public static Api.SetObjectsTranslucencyResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
