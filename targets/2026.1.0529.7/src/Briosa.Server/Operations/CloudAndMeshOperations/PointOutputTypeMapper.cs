using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class PointOutputTypeMapper
{
    public static WorkerTextValue ToMpValue(Api.PointOutputType outputType, string fieldName) => new(outputType switch
    {
        Api.PointOutputType.Points => "Points",
        Api.PointOutputType.CloudPoints => "Cloud Points",
        _ => throw RequestFieldViolation.SupportedChoice(fieldName)
    });
}
