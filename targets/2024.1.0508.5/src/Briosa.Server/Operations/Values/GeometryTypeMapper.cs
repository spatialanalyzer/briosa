using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class GeometryTypeMapper
{
    public static WorkerMpValue Required(Api.GeometryType value, string fieldName)
    {
        if (value is < Api.GeometryType.Line or > Api.GeometryType.Torus)
        {
            throw new ArgumentOutOfRangeException(fieldName, "The geometry type is not supported by this SA target.");
        }

        return WorkerChoiceFactory.FromOrdinal(WorkerMpValueKind.GeometryType, (int)value - 1);
    }
}
