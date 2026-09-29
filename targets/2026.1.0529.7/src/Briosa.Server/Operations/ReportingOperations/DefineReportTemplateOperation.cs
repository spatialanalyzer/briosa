using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class DefineReportTemplateOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.define_report_template", "Define Report Template", "briosa.ReportingOperations",
        "DefineReportTemplate", "/briosa.ReportingOperations/DefineReportTemplate", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DefineReportTemplateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Report Template Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReportTemplateName, "report_template_name"), "SetCollectionObjectNameArg2"),
            new("Title", WorkerMpValueKind.StringList, StringListMapper.RequiredList(request.Title, "title"), "SetStringRefListArg"),
            new("Graphical View Options", WorkerMpValueKind.ReportViewOptions,
                ReportViewOptionsMapper.Required(request.GraphicalViewOptions, "graphical_view_options"), "SetReportViewOptionsArg"),
            new("Items To Report", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ItemsToReport, "items_to_report"), "SetCollectionObjectNameRefListArg"),
            new("Relationships To Report", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.RelationshipsToReport, "relationships_to_report"),
                "SetCollectionObjectNameRefListArg"),
            new("Events To Report", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.EventsToReport, "events_to_report"),
                "SetCollectionObjectNameRefListArg"),
            new("Report Output Options", WorkerMpValueKind.ReportOutputOptions,
                ReportOutputOptionsMapper.WithDefault(request.ReportOutputOptions), "SetReportOutputOptionsArg"),
            new("Report Page Settings ( SA Report only )", WorkerMpValueKind.ReportPageOrientation,
                new WorkerChoiceValue<WorkerReportPageOrientationValue>(ReportPageSettingsMapper.WithDefault(
                    request.HasReportPageSettings ? request.ReportPageSettings : null)), "SetReportPageSettingsArg"),
            new("Generate Now?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.GenerateNow), "SetBoolArg"),
            new("Show Generated Report?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ShowGeneratedReport), "SetBoolArg")
        ], []);
    }

    public static Api.DefineReportTemplateResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
