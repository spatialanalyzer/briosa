using Briosa.Server.Operations;
using Briosa.Server.Operations.ReportingOperations;
using Briosa.Server.Operations.Values;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedReportLifecycleOperationTests
{
    private static readonly string[] Ids =
    [
        "reporting_operations.close_all_reports", "reporting_operations.close_html_display_board",
        "reporting_operations.combine_sa_reports", "reporting_operations.define_report_template",
        "reporting_operations.delete_sa_doc", "reporting_operations.delete_sa_report",
        "reporting_operations.delete_sa_report_template", "reporting_operations.make_new_sa_report"
    ];

    [Fact]
    public void ReportLifecycleOperationsAreRegisteredWithAtRiskStatus()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Contains("fixture_validation_pending", CloseHtmlDisplayBoardOperation.Descriptor.RiskFlags);
        Assert.All(Ids, id => Assert.Equal(Api.ReplaySafety.Unsafe,
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id).ReplaySafety));
    }

    [Fact]
    public void ReportLifecycleMappingsPreserveInputsOptionalityAndDefaults()
    {
        Assert.Empty(CloseAllReportsOperation.CreateCommand(new()).InputArguments);
        Assert.Empty(CloseHtmlDisplayBoardOperation.CreateCommand(new()).InputArguments);

        Assert.Throws<ArgumentException>(() => CombineSaReportsOperation.CreateCommand(new()));
        var combine = CombineSaReportsOperation.CreateCommand(new()
        {
            SaReportsToCombine = { Item("One"), Item("Two") },
            OutputSaReportName = Object("Combined")
        });
        Assert.Equal(["One", "Two"], combine.InputArguments[0]
            .RequireValue<WorkerCollectionItemNameListValue>().Values.Select(value => value.ItemName));
        Assert.Equal("Combined", combine.InputArguments[1]
            .RequireValue<WorkerCollectionObjectNameValue>().ObjectName);
        Assert.False(combine.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => DefineReportTemplateOperation.CreateCommand(new()));
        var define = DefineReportTemplateOperation.CreateCommand(DefineTemplateRequest());
        Assert.Equal("Template", define.InputArguments[0]
            .RequireValue<WorkerCollectionObjectNameValue>().ObjectName);
        Assert.Equal(["Summary"], define.InputArguments[1].RequireValue<WorkerStringListValue>().Values);
        Assert.Equal(new WorkerReportViewOptionsValue(2, "Views", "Detail"),
            define.InputArguments[2].RequireValue<WorkerReportViewOptionsValue>());
        Assert.Equal("Part", define.InputArguments[3]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectName);
        Assert.Equal("Relationship", define.InputArguments[4]
            .RequireValue<WorkerCollectionItemNameListValue>().Values[0].ItemName);
        Assert.Equal("Event", define.InputArguments[5]
            .RequireValue<WorkerCollectionItemNameListValue>().Values[0].ItemName);
        var output = define.InputArguments[6].RequireValue<WorkerReportOutputOptionsValue>();
        Assert.Equal(1, output.OutputType);
        Assert.Null(output.ExternalPath);
        Assert.Equal(new WorkerEmbeddedReportFileValue(string.Empty, "My Report"), output.EmbeddedFile);
        Assert.Equal(WorkerReportPageOrientationValue.Portrait,
            define.InputArguments[7].RequireValue<WorkerChoiceValue<WorkerReportPageOrientationValue>>().Value);
        Assert.False(define.InputArguments[8].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(define.InputArguments[9].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => DeleteSaDocOperation.CreateCommand(new()));
        Assert.Equal("Document", DeleteSaDocOperation.CreateCommand(new() { DocName = Object("Document") })
            .InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectName);
        Assert.Throws<ArgumentException>(() => DeleteSaReportOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => DeleteSaReportTemplateOperation.CreateCommand(new()));

        Assert.Throws<ArgumentException>(() => MakeNewSaReportOperation.CreateCommand(new()));
        var reportWithoutTemplate = MakeNewSaReportOperation.CreateCommand(new()
        {
            NewSaReportName = Object("New Report")
        });
        Assert.Single(reportWithoutTemplate.InputArguments);
        var reportWithTemplate = MakeNewSaReportOperation.CreateCommand(new()
        {
            NewSaReportName = Object("New Report"), SaReportTemplate = Object("Template")
        });
        Assert.Equal("Template", reportWithTemplate.InputArguments[1]
            .RequireValue<WorkerCollectionObjectNameValue>().ObjectName);

        Assert.Throws<ArgumentException>(() => ReportViewOptionsMapper.Required(
            new Api.ReportViewOptions(), "graphical_view_options"));
        Assert.Throws<ArgumentException>(() => ReportOutputOptionsMapper.WithDefault(new Api.ReportOutputOptions()));
        Assert.Throws<ArgumentException>(() => ReportPageSettingsMapper.WithDefault(Api.ReportPageSettings.Unspecified));
        Assert.Equal(WorkerReportPageOrientationValue.Landscape,
            ReportPageSettingsMapper.WithDefault(Api.ReportPageSettings.Landscape));
    }

    [Fact]
    public async Task GeneratedClientRoutesReportLifecycleOperationsThroughTypedMappings()
    {
        var worker = new LifecycleWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ReportingOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ReportingOperations.ReportingOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(30));

        await client.CloseAllReportsAsync(new(), options);
        await client.CloseHtmlDisplayBoardAsync(new(), options);
        await client.CombineSaReportsAsync(new()
        {
            SaReportsToCombine = { Item("One") }, OutputSaReportName = Object("Combined")
        }, options);
        await client.DefineReportTemplateAsync(DefineTemplateRequest(), options);
        await client.DeleteSaDocAsync(new() { DocName = Object("Document") }, options);
        await client.DeleteSaReportAsync(new() { ReportName = Object("Report") }, options);
        await client.DeleteSaReportTemplateAsync(new() { ReportTemplateName = Object("Template") }, options);
        await client.MakeNewSaReportAsync(new() { NewSaReportName = Object("New Report") }, options);

        Assert.Equal(Ids, worker.Commands.Select(command => command.OperationId));
    }

    private static Api.DefineReportTemplateRequest DefineTemplateRequest() => new()
    {
        ReportTemplateName = Object("Template"),
        Title = { "Summary" },
        GraphicalViewOptions = new Api.ReportViewOptions
        {
            ViewType = Api.ReportViewType.CalloutView,
            CollectionName = "Views",
            CalloutName = "Detail"
        },
        ItemsToReport = { Object("Part") },
        RelationshipsToReport = { Item("Relationship") },
        EventsToReport = { Item("Event") }
    };

    private static Api.CollectionObjectName Object(string name) => new()
    {
        CollectionName = "Reports",
        ObjectName = name,
        ObjectType = Api.ObjectType.Unspecified
    };

    private static Api.CollectionItemName Item(string name) => new()
    {
        CollectionName = "Reports",
        ItemName = name
    };

    private sealed class LifecycleWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
