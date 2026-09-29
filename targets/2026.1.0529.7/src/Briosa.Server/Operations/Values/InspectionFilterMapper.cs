using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class InspectionFilterMapper
{
    public static WorkerTextValue ToWorker(Api.InspectionFilter value) => value switch
    {
        Api.InspectionFilter.Unspecified or Api.InspectionFilter.All => new("ALL"),
        Api.InspectionFilter.Checks => new("CHECKS"),
        Api.InspectionFilter.Datums => new("DATUMS"),
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported inspection filter.")
    };
}
