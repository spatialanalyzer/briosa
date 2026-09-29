using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class ReportPageSettingsMapper
{
    public static WorkerReportPageOrientationValue WithDefault(Api.ReportPageSettings? value) => value switch
    {
        null or Api.ReportPageSettings.Portrait => WorkerReportPageOrientationValue.Portrait,
        Api.ReportPageSettings.Landscape => WorkerReportPageOrientationValue.Landscape,
        _ => throw new ArgumentException("Report page settings must specify a supported orientation.", nameof(value))
    };
}
