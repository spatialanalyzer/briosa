using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class PointDeltaReportOptionsMapper
{
    public static WorkerPointDeltaReportOptionsValue ToWorker(Api.PointDeltaReportOptions? value) => new(
        (int)CoordinateSystemTypeMapper.OrDefault(
            value?.HasCoordinateSystem == true ? value.CoordinateSystem : null,
            Api.CoordinateSystemType.Cartesian),
        value?.HasDetailsFormat == true ? value.DetailsFormat : "Single",
        value?.HasShowPointA != true || value.ShowPointA,
        value?.HasShowPointB != true || value.ShowPointB,
        value?.HasShowDelta != true || value.ShowDelta,
        value?.HasShowMagnitude != true || value.ShowMagnitude,
        value?.HasShowComponent1 != true || value.ShowComponent1,
        value?.HasShowComponent2 != true || value.ShowComponent2,
        value?.HasShowComponent3 != true || value.ShowComponent3,
        value?.HasSortPointNames == true && value.SortPointNames,
        value?.HasShowToleranceFields != true || value.ShowToleranceFields,
        value?.HasColorizeInToleranceFields != true || value.ColorizeInToleranceFields);
}
