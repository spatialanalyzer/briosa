using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class ChartTypeMapper
{
    public static WorkerChartTypeValue Required(Api.ChartType? value, string fieldName) => value switch
    {
        Api.ChartType.RunChart => WorkerChartTypeValue.RunChart,
        Api.ChartType.IndividualXMovingRange => WorkerChartTypeValue.IndividualXMovingRange,
        Api.ChartType.BullseyeChart => WorkerChartTypeValue.BullseyeChart,
        _ => throw new ArgumentException($"Request field '{fieldName}' is required or unsupported.", nameof(value))
    };
}
