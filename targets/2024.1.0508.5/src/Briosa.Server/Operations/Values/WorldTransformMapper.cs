using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class WorldTransformMapper
{
    public static WorkerWorldTransformValue Required(Api.WorldTransform? value, string fieldName)
    {
        if (value?.Transform is null)
        {
            throw new ArgumentException($"Request field '{fieldName}' must include a transform.", nameof(value));
        }

        return new(
            TransformMapper.Required(value.Transform, $"{fieldName}.transform"),
            value.HasScaleFactor ? value.ScaleFactor : 1d);
    }

    public static Api.WorldTransform ToProtocol(WorkerWorldTransformValue value) => new()
    {
        Transform = TransformMapper.ToProtocol(value.Transform),
        ScaleFactor = value.ScaleFactor
    };
}
