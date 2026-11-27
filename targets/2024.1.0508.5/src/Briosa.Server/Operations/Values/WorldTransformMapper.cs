using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class WorldTransformMapper
{
    public static WorkerWorldTransformValue Required(Api.WorldTransform? value, string fieldName)
    {
        if (value is null)
        {
            throw RequestFieldViolation.Required(fieldName);
        }

        return new(
            TransformMapper.Required(value.Transform, RequestFieldViolation.Path(fieldName, "transform")),
            value.HasScaleFactor ? value.ScaleFactor : 1d);
    }

    public static Api.WorldTransform ToProtocol(WorkerWorldTransformValue value) => new()
    {
        Transform = TransformMapper.ToProtocol(value.Transform),
        ScaleFactor = value.ScaleFactor
    };
}
