using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class CloudThinningOptionsMapper
{
    public static WorkerCloudThinningOptionsValue ToWorker(Api.CloudThinningOptions? value, string fieldName)
    {
        if (value is null)
        {
            return new(0, 1, 0, 0);
        }

        var mode = value.HasMode ? value.Mode : Api.CloudThinningMode.None;
        if (mode == Api.CloudThinningMode.Unspecified || !Enum.IsDefined(mode))
        {
            throw RequestFieldViolation.SupportedChoice(RequestFieldViolation.Path(fieldName, "mode"));
        }

        return new(
            (int)mode - 1,
            value.HasPointIncrement ? value.PointIncrement : 1,
            value.HasMinimumNumberOfPoints ? value.MinimumNumberOfPoints : 0,
            value.HasMaximumNumberOfPoints ? value.MaximumNumberOfPoints : 0);
    }
}
