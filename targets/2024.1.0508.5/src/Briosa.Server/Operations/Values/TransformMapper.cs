using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class TransformMapper
{
    public static WorkerTransformValue Required(Api.Transform? value, string fieldName)
    {
        if (value is null || value.Values.Count != 16)
        {
            throw new ArgumentException($"Request field '{fieldName}' must contain exactly 16 transform values.", nameof(value));
        }

        return new(value.Values);
    }

    public static Api.Transform ToProtocol(WorkerTransformValue value) => new() { Values = { value.Values } };
}
