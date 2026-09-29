using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class VectorMapper
{
    public static WorkerVectorValue Required(Api.Vector? value, string fieldName)
    {
        if (value is null)
        {
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(value));
        }

        return new(value.X, value.Y, value.Z);
    }

    public static Api.Vector ToProtocol(WorkerVectorValue value) => new() { X = value.X, Y = value.Y, Z = value.Z };
}
