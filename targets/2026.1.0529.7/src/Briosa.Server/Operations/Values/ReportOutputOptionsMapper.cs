using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class ReportOutputOptionsMapper
{
    public static WorkerReportOutputOptionsValue WithDefault(Api.ReportOutputOptions? value)
    {
        if (value is null)
        {
            value = new Api.ReportOutputOptions
            {
                OutputType = Api.ReportOutputType.SaReport,
                EmbeddedFile = new Api.EmbeddedReportFile { FileName = "My Report" }
            };
        }

        if (!value.HasOutputType || value.OutputType == Api.ReportOutputType.Unspecified ||
            !Enum.IsDefined(value.OutputType))
        {
            throw new ArgumentException("Report output type must specify a supported value.", nameof(value));
        }

        var embeddedFile = value.DestinationCase == Api.ReportOutputOptions.DestinationOneofCase.EmbeddedFile
            ? new WorkerEmbeddedReportFileValue(value.EmbeddedFile.CollectionName, value.EmbeddedFile.FileName)
            : null;
        var externalPath = value.DestinationCase == Api.ReportOutputOptions.DestinationOneofCase.ExternalPath
            ? value.ExternalPath
            : null;
        if ((embeddedFile is null) == (externalPath is null))
        {
            throw new ArgumentException("Report output must specify exactly one destination.", nameof(value));
        }

        return new((int)value.OutputType - 1, externalPath, embeddedFile);
    }
}
