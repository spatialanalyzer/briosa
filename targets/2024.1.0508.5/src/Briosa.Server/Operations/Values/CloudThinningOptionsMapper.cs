using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class CloudThinningOptionsMapper
{
    public static WorkerCloudThinningOptionsValue ToWorker(Api.CloudThinningOptions? value)
    {
        if (value is null)
        {
            return new(0, 1, 0, 0);
        }

        var mode = value.HasMode ? value.Mode : Api.CloudThinningMode.None;
        if (mode == Api.CloudThinningMode.Unspecified || !Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Cloud thinning mode is not supported.");
        }

        return new(
            (int)mode - 1,
            value.HasPointIncrement ? value.PointIncrement : 1,
            value.HasMinimumNumberOfPoints ? value.MinimumNumberOfPoints : 0,
            value.HasMaximumNumberOfPoints ? value.MaximumNumberOfPoints : 0);
    }
}
