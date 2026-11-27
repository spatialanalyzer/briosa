using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class DatasetTypeMapper
{
    public static WorkerVectorComponentValue Required(Api.DatasetType? value, string fieldName) => value switch
    {
        Api.DatasetType.X => WorkerVectorComponentValue.X,
        Api.DatasetType.Y => WorkerVectorComponentValue.Y,
        Api.DatasetType.Z => WorkerVectorComponentValue.Z,
        Api.DatasetType.Magnitude => WorkerVectorComponentValue.Magnitude,
        null => throw RequestFieldViolation.Required(fieldName),
        _ => throw RequestFieldViolation.SupportedChoice(fieldName)
    };
}
