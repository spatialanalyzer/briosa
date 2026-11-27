using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class TransformMapper
{
    public static WorkerTransformValue Required(Api.Transform? value, string fieldName)
    {
        if (value is null)
        {
            throw RequestFieldViolation.Required(fieldName);
        }

        if (value.Values.Count != 16)
        {
            throw RequestFieldViolation.ElementCount(RequestFieldViolation.Path(fieldName, "values"));
        }

        return new(value.Values);
    }

    public static Api.Transform ToProtocol(WorkerTransformValue value) => new() { Values = { value.Values } };
}
