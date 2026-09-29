using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class RenderModeTypeMapper
{
    public static WorkerChoiceValue<WorkerRenderModeTypeValue> ToWorker(Api.RenderModeType value) => value switch
    {
        Api.RenderModeType.Wireframe => new(WorkerRenderModeTypeValue.Wireframe),
        Api.RenderModeType.HiddenLineRemoved => new(WorkerRenderModeTypeValue.HiddenLineRemoved),
        Api.RenderModeType.SolidAndEdges => new(WorkerRenderModeTypeValue.SolidAndEdges),
        Api.RenderModeType.Solid => new(WorkerRenderModeTypeValue.Solid),
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Render mode is not supported.")
    };

    public static WorkerChoiceValue<WorkerRenderModeTypeValue> Required(Api.RenderModeType? value, string fieldName)
    {
        if (value is null || value == Api.RenderModeType.Unspecified)
        {
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(value));
        }

        return ToWorker(value.Value);
    }
}
