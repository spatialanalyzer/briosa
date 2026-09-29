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

public sealed class TypedChartOperationTests
{
    private static readonly string[] Ids =
    [
        "reporting_operations.add_charts_to_report_bar", "reporting_operations.create_chart_from_vector_group",
        "reporting_operations.delete_chart", "reporting_operations.make_utility_chart",
        "reporting_operations.save_chart_to_jpeg_file"
    ];

    [Fact]
    public void ChartOperationsAreRegisteredAndKeepAtRiskValidationStatus()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Contains("fixture_validation_pending", MakeUtilityChartOperation.Descriptor.RiskFlags);
        Assert.Equal(SpatialAnalyzerApi.TargetVersion.StartsWith("2024", StringComparison.Ordinal)
                ? "Save Chart to JPeg file" : "Save Chart to JPEG File",
            SaveChartToJPegFileOperation.Descriptor.MpStep);
    }

    [Fact]
    public void ChartMappingsPreserveChoicesDefaultsAndOutputs()
    {
        Assert.Throws<ArgumentException>(() => AddChartsToReportBarOperation.CreateCommand(new()));
        var charts = AddChartsToReportBarOperation.CreateCommand(new()
        {
            Charts = { Item("Control Chart") }
        });
        Assert.Equal("Control Chart", charts.InputArguments[0]
            .RequireValue<WorkerCollectionItemNameListValue>().Values[0].ItemName);
        Assert.False(charts.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => CreateChartFromVectorGroupOperation.CreateCommand(new()));
        var chart = CreateChartFromVectorGroupOperation.CreateCommand(new()
        {
            NewChartName = new Api.ChartName { Name = "Run" },
            VectorGroupName = Object("Vectors"),
            ChartType = Api.ChartType.RunChart,
            DataSetToChart = Api.DatasetType.X,
            AuxDataSetToChart = Api.DatasetType.Magnitude,
            TemplateChartName = new Api.ChartName { Name = "Template" }
        });
        Assert.Equal("Run", chart.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(WorkerChartTypeValue.RunChart,
            chart.InputArguments[2].RequireValue<WorkerChoiceValue<WorkerChartTypeValue>>().Value);
        Assert.Equal(WorkerVectorComponentValue.X,
            chart.InputArguments[3].RequireValue<WorkerChoiceValue<WorkerVectorComponentValue>>().Value);
        Assert.Equal(WorkerVectorComponentValue.Magnitude,
            chart.InputArguments[4].RequireValue<WorkerChoiceValue<WorkerVectorComponentValue>>().Value);
        Assert.Equal("Template", chart.InputArguments[5].RequireValue<WorkerTextValue>().Value);
        Assert.False(chart.InputArguments[6].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => CreateChartFromVectorGroupOperation.CreateCommand(new()
        {
            NewChartName = new Api.ChartName { Name = "Run" },
            VectorGroupName = Object("Vectors"),
            ChartType = Api.ChartType.Unspecified,
            DataSetToChart = Api.DatasetType.X,
            AuxDataSetToChart = Api.DatasetType.Y,
            TemplateChartName = new Api.ChartName { Name = "Template" }
        }));
        Assert.Throws<ArgumentException>(() => CreateChartFromVectorGroupOperation.CreateCommand(new()
        {
            NewChartName = new Api.ChartName { Name = "Run" },
            VectorGroupName = Object("Vectors"),
            ChartType = Api.ChartType.RunChart,
            DataSetToChart = Api.DatasetType.X,
            AuxDataSetToChart = Api.DatasetType.Y
        }));

        Assert.Throws<ArgumentException>(() => DeleteChartOperation.CreateCommand(new()));
        Assert.Equal("Chart Name", DeleteChartOperation.CreateCommand(new()
        {
            ChartName = Object("Run")
        }).InputArguments[0].Name);

        Assert.Throws<ArgumentException>(() => MakeUtilityChartOperation.CreateCommand(new()));
        var utilityChart = MakeUtilityChartOperation.CreateCommand(new()
        {
            AsciiFilePath = new Api.FileReference { Path = "data.txt" },
            OutputPictureName = Item("Plot")
        });
        Assert.Equal([string.Empty, "Plot"], new[]
        {
            utilityChart.InputArguments[1].RequireValue<WorkerTextValue>().Value,
            utilityChart.InputArguments[2].RequireValue<WorkerCollectionItemNameValue>().ItemName
        });
        Assert.Equal([false, false], utilityChart.InputArguments.Skip(3).Take(2)
            .Select(argument => argument.RequireValue<WorkerBooleanValue>().Value));
        Assert.Equal([0d, 0d], utilityChart.InputArguments.Skip(5)
            .Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));
        Assert.Equal("GetBoolArg", utilityChart.OutputArguments[0].SdkBinding);
        var utilityResult = MakeUtilityChartOperation.CreateResult(Success(
        [new WorkerRetrievedOutput("Is Point Inside?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true))]));
        Assert.True(utilityResult.IsPointInside);

        Assert.Throws<ArgumentException>(() => SaveChartToJPegFileOperation.CreateCommand(new()));
        var save = SaveChartToJPegFileOperation.CreateCommand(new()
        {
            ChartToSave = new Api.ChartName { Name = "Run" },
            FileToSaveTo = new Api.FileReference { Path = "run.jpg" }
        });
        Assert.Equal(["SetChartNameArg", "SetFilePathArg"], save.InputArguments.Select(argument => argument.SdkBinding));
    }

    [Fact]
    public async Task GeneratedClientRoutesChartOperationsThroughTypedMappings()
    {
        var worker = new ChartWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ReportingOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ReportingOperations.ReportingOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        await client.AddChartsToReportBarAsync(new() { Charts = { Item("Control Chart") } }, options);
        await client.CreateChartFromVectorGroupAsync(new()
        {
            NewChartName = new Api.ChartName { Name = "Run" },
            VectorGroupName = Object("Vectors"),
            ChartType = Api.ChartType.RunChart,
            DataSetToChart = Api.DatasetType.X,
            AuxDataSetToChart = Api.DatasetType.Y,
            TemplateChartName = new Api.ChartName { Name = "Template" }
        }, options);
        await client.DeleteChartAsync(new() { ChartName = Object("Run") }, options);
        var chart = await client.MakeUtilityChartAsync(new()
        {
            AsciiFilePath = new Api.FileReference { Path = "data.txt" },
            OutputPictureName = Item("Plot")
        }, options);
        await client.SaveChartToJPegFileAsync(new()
        {
            ChartToSave = new Api.ChartName { Name = "Run" },
            FileToSaveTo = new Api.FileReference { Path = "run.jpg" }
        }, options);

        Assert.True(chart.IsPointInside);
        Assert.Equal(Ids, worker.Commands.Select(command => command.OperationId));
    }

    private static Api.CollectionItemName Item(string name) => new()
    {
        CollectionName = "Parts",
        ItemName = name
    };

    private static Api.CollectionObjectName Object(string name) => new()
    {
        CollectionName = "Parts",
        ObjectName = name,
        ObjectType = Api.ObjectType.Unspecified
    };

    private static SuccessfulOperationExecution Success(IReadOnlyList<WorkerMpOutputValue> outputs)
    {
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
        return new(execution, new Api.MpExecutionDetails());
    }

    private sealed class ChartWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId == "reporting_operations.make_utility_chart"
                ? [new WorkerRetrievedOutput("Is Point Inside?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true))]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
