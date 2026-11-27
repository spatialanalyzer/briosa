using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class ReportOutputOptionsMapper
{
    public static WorkerReportOutputOptionsValue WithDefault(Api.ReportOutputOptions? value, string fieldName)
    {
        if (value is null)
        {
            value = new Api.ReportOutputOptions
            {
                OutputType = Api.ReportOutputType.SaReport,
                EmbeddedFile = new Api.EmbeddedReportFile { FileName = "My Report" }
            };
        }

        if (!value.HasOutputType)
        {
            throw RequestFieldViolation.Required(RequestFieldViolation.Path(fieldName, "output_type"));
        }

        if (value.OutputType == Api.ReportOutputType.Unspecified || !Enum.IsDefined(value.OutputType))
        {
            throw RequestFieldViolation.SupportedChoice(RequestFieldViolation.Path(fieldName, "output_type"));
        }

        var embeddedFile = value.DestinationCase == Api.ReportOutputOptions.DestinationOneofCase.EmbeddedFile
            ? new WorkerEmbeddedReportFileValue(value.EmbeddedFile.CollectionName, value.EmbeddedFile.FileName)
            : null;
        var externalPath = value.DestinationCase == Api.ReportOutputOptions.DestinationOneofCase.ExternalPath
            ? value.ExternalPath
            : null;
        if ((embeddedFile is null) == (externalPath is null))
        {
            throw RequestFieldViolation.OneofRequired(fieldName);
        }

        return new((int)value.OutputType - 1, externalPath, embeddedFile);
    }
}
