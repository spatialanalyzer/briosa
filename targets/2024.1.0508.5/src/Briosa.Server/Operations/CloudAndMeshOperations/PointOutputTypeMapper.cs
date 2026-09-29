using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class PointOutputTypeMapper
{
    public static WorkerTextValue ToMpValue(Api.PointOutputType outputType) => new(outputType switch
    {
        Api.PointOutputType.Points => "Points",
        Api.PointOutputType.CloudPoints => "Cloud Points",
        _ => throw new ArgumentException("Unsupported point output type.", nameof(outputType))
    });
}
