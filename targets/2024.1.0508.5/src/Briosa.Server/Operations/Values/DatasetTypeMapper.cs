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
        _ => throw new ArgumentException($"Request field '{fieldName}' is required or unsupported.", nameof(value))
    };
}
