using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class ReportViewOptionsMapper
{
    public static WorkerReportViewOptionsValue Required(Api.ReportViewOptions? value, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(value, fieldName);
        if (!value.HasViewType || value.ViewType == Api.ReportViewType.Unspecified || !Enum.IsDefined(value.ViewType))
        {
            throw new ArgumentException($"Request field '{fieldName}.view_type' must specify a supported value.", nameof(value));
        }

        return new((int)value.ViewType - 1, value.CollectionName, value.CalloutName);
    }
}
