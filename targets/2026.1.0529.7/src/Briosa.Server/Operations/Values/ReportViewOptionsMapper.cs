using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class ReportViewOptionsMapper
{
    public static WorkerReportViewOptionsValue Required(Api.ReportViewOptions? value, string fieldName)
    {
        if (value is null)
        {
            throw RequestFieldViolation.Required(fieldName);
        }

        if (!value.HasViewType)
        {
            throw RequestFieldViolation.Required(RequestFieldViolation.Path(fieldName, "view_type"));
        }

        if (value.ViewType == Api.ReportViewType.Unspecified || !Enum.IsDefined(value.ViewType))
        {
            throw RequestFieldViolation.SupportedChoice(RequestFieldViolation.Path(fieldName, "view_type"));
        }

        return new((int)value.ViewType - 1, value.CollectionName, value.CalloutName);
    }
}
