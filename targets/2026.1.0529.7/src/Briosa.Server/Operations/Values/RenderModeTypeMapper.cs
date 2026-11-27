using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class RenderModeTypeMapper
{
    public static WorkerChoiceValue<WorkerRenderModeTypeValue> ToWorker(Api.RenderModeType value, string fieldName) => value switch
    {
        Api.RenderModeType.Wireframe => new(WorkerRenderModeTypeValue.Wireframe),
        Api.RenderModeType.HiddenLineRemoved => new(WorkerRenderModeTypeValue.HiddenLineRemoved),
        Api.RenderModeType.SolidAndEdges => new(WorkerRenderModeTypeValue.SolidAndEdges),
        Api.RenderModeType.Solid => new(WorkerRenderModeTypeValue.Solid),
        _ => throw RequestFieldViolation.SupportedChoice(fieldName)
    };

    public static WorkerChoiceValue<WorkerRenderModeTypeValue> Required(Api.RenderModeType? value, string fieldName)
    {
        if (value is null)
        {
            throw RequestFieldViolation.Required(fieldName);
        }

        return ToWorker(value.Value, fieldName);
    }
}
