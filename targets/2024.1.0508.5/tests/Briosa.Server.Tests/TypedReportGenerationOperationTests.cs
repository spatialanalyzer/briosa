using Briosa.Server.Operations;
using Briosa.Server.Operations.ReportingOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedReportGenerationOperationTests
{
    private static readonly string[] Ids =
    [
        "reporting_operations.generate_quick_report_from_tab_order",
        "reporting_operations.generate_standard_html_report",
        "reporting_operations.generate_update_templated_report", "reporting_operations.html_display_board",
        "reporting_operations.output_sa_report_to_excel", "reporting_operations.output_sa_report_to_pdf",
        "reporting_operations.quick_report"
    ];

    [Fact]
    public void ReportGenerationOperationsAreRegisteredAndKeepRiskFlags()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Contains("fixture_validation_pending", HtmlDisplayBoardOperation.Descriptor.RiskFlags);
        Assert.All(Ids, id => Assert.Equal(Api.ReplaySafety.Unsafe,
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id).ReplaySafety));
    }

    [Fact]
    public void ReportGenerationMappingsPreserveDefaultsAndRequiredFields()
    {
        var quickFromTabs = GenerateQuickReportFromTabOrderOperation.CreateCommand(new());
        var output = quickFromTabs.InputArguments[0].RequireValue<WorkerReportOutputOptionsValue>();
        Assert.Equal(1, output.OutputType);
        Assert.Equal(new WorkerEmbeddedReportFileValue(string.Empty, "My Report"), output.EmbeddedFile);
        Assert.False(quickFromTabs.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => GenerateStandardHtmlReportOperation.CreateCommand(new()));
        var standardHtml = GenerateStandardHtmlReportOperation.CreateCommand(new()
        {
            HtmlOutputFile = File("report.html")
        });
        Assert.Equal("report.html", standardHtml.InputArguments[0].RequireValue<WorkerFileReferenceValue>().Path);
        Assert.Equal(0, standardHtml.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);

        Assert.Throws<ArgumentException>(() => GenerateUpdateTemplatedReportOperation.CreateCommand(new()));
        Assert.Equal("Template", GenerateUpdateTemplatedReportOperation.CreateCommand(new()
        {
            ReportTemplate = Object("Template")
        }).InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectName);

        Assert.Throws<ArgumentException>(() => HtmlDisplayBoardOperation.CreateCommand(new()));
        var board = HtmlDisplayBoardOperation.CreateCommand(new() { InputHtmlFile = File("board.html") });
        Assert.True(board.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        board = HtmlDisplayBoardOperation.CreateCommand(new()
        {
            InputHtmlFile = File("board.html"), ShowBoard = false
        });
        Assert.False(board.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => OutputSaReportToExcelOperation.CreateCommand(new()));
        var excel = OutputSaReportToExcelOperation.CreateCommand(new()
        {
            ReportName = Object("Report"), FileName = File("report.xlsx")
        });
        Assert.False(excel.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => OutputSaReportToPdfOperation.CreateCommand(new()));
        var pdf = OutputSaReportToPdfOperation.CreateCommand(new()
        {
            ReportName = Object("Report"), FileName = File("report.pdf")
        });
        Assert.False(pdf.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => QuickReportOperation.CreateCommand(new()));
        var quick = QuickReportOperation.CreateCommand(new() { ItemName = Object("Part") });
        Assert.Equal(string.Empty, quick.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.False(quick.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        quick = QuickReportOperation.CreateCommand(new()
        {
            ItemName = Object("Part"), ReportName = "Inspection", OpenReport = true
        });
        Assert.Equal("Inspection", quick.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.True(quick.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientRoutesReportGenerationOperationsThroughTypedMappings()
    {
        var worker = new ReportWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ReportingOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ReportingOperations.ReportingOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(30));

        await client.GenerateQuickReportFromTabOrderAsync(new(), options);
        await client.GenerateStandardHtmlReportAsync(new() { HtmlOutputFile = File("report.html") }, options);
        await client.GenerateUpdateTemplatedReportAsync(new() { ReportTemplate = Object("Template") }, options);
        await client.HtmlDisplayBoardAsync(new() { InputHtmlFile = File("board.html") }, options);
        await client.OutputSaReportToExcelAsync(new()
        {
            ReportName = Object("Report"), FileName = File("report.xlsx")
        }, options);
        await client.OutputSaReportToPdfAsync(new()
        {
            ReportName = Object("Report"), FileName = File("report.pdf")
        }, options);
        await client.QuickReportAsync(new() { ItemName = Object("Part") }, options);

        Assert.Equal(Ids, worker.Commands.Select(command => command.OperationId));
    }

    private static Api.CollectionObjectName Object(string name) => new()
    {
        CollectionName = "Reports",
        ObjectName = name,
        ObjectType = Api.ObjectType.Unspecified
    };

    private static Api.FileReference File(string path) => new() { Path = path };

    private sealed class ReportWorker : IWorkerCommandExecutor
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
