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

public sealed class TypedCustomTableOperationTests
{
    private static readonly string[] Ids =
    [
        "reporting_operations.add_custom_table_to_sa_report", "reporting_operations.add_custom_tables_to_report_bar",
        "reporting_operations.clear_custom_table", "reporting_operations.delete_custom_table",
        "reporting_operations.get_custom_table_cell_double", "reporting_operations.get_custom_table_cell_string",
        "reporting_operations.make_custom_table", "reporting_operations.set_custom_table_cell_color",
        "reporting_operations.set_custom_table_cell_double", "reporting_operations.set_custom_table_cell_font",
        "reporting_operations.set_custom_table_cell_string", "reporting_operations.set_custom_table_header_cell",
        "reporting_operations.set_custom_table_header_row", "reporting_operations.set_custom_table_title"
    ];

    [Fact]
    public void CustomTableOperationsAreRegisteredWithReadOnlyGets()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Equal(Api.ReplaySafety.Safe, GetCustomTableCellDoubleOperation.Descriptor.ReplaySafety);
        Assert.Equal(Api.ReplaySafety.Safe, GetCustomTableCellStringOperation.Descriptor.ReplaySafety);
        Assert.All(Ids.Except(["reporting_operations.get_custom_table_cell_double", "reporting_operations.get_custom_table_cell_string"]),
            id => Assert.Equal(Api.ReplaySafety.Unsafe,
                Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id).ReplaySafety));
    }

    [Fact]
    public void CustomTableMappingsPreserveBindingsDefaultsAndRetrievedValues()
    {
        Assert.Throws<ArgumentException>(() => AddCustomTableToSaReportOperation.CreateCommand(new()));
        var add = AddCustomTableToSaReportOperation.CreateCommand(new()
        {
            TableName = Object("Table"), ReportName = Object("Report")
        });
        Assert.Equal(["Table", "Report"], add.InputArguments.Take(2)
            .Select(argument => argument.RequireValue<WorkerCollectionObjectNameValue>().ObjectName));
        Assert.False(add.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => AddCustomTablesToReportBarOperation.CreateCommand(new()));
        var addBar = AddCustomTablesToReportBarOperation.CreateCommand(new()
        {
            CustomTablesToReport = { Item("Table") }
        });
        Assert.Equal("Table", addBar.InputArguments[0]
            .RequireValue<WorkerCollectionItemNameListValue>().Values[0].ItemName);
        Assert.False(addBar.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => ClearCustomTableOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => DeleteCustomTableOperation.CreateCommand(new()));

        Assert.Throws<ArgumentException>(() => GetCustomTableCellDoubleOperation.CreateCommand(new()));
        var getDouble = GetCustomTableCellDoubleOperation.CreateCommand(new() { TableName = Object("Table") });
        Assert.Equal([0, 0], getDouble.InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerIntegerValue>().Value));
        Assert.Equal("GetDoubleArg", getDouble.OutputArguments[0].SdkBinding);
        Assert.True(GetCustomTableCellDoubleOperation.CreateResult(Success(
            [new WorkerRetrievedOutput("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(12.5))])).HasValue);
        Assert.Equal(12.5, GetCustomTableCellDoubleOperation.CreateResult(Success(
            [new WorkerRetrievedOutput("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(12.5))])).Value);

        Assert.Throws<ArgumentException>(() => GetCustomTableCellStringOperation.CreateCommand(new()));
        var getString = GetCustomTableCellStringOperation.CreateCommand(new() { TableName = Object("Table") });
        Assert.Equal([0, 0], getString.InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerIntegerValue>().Value));
        Assert.Equal("GetStringArg", getString.OutputArguments[0].SdkBinding);
        Assert.Equal("Cell", GetCustomTableCellStringOperation.CreateResult(Success(
            [new WorkerRetrievedOutput("Value", WorkerMpValueKind.Text, new WorkerTextValue("Cell"))])).Value);

        Assert.Throws<ArgumentException>(() => MakeCustomTableOperation.CreateCommand(new()));
        var table = MakeCustomTableOperation.CreateCommand(new() { TableName = Object("Table") });
        Assert.Equal(6, table.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        table = MakeCustomTableOperation.CreateCommand(new() { TableName = Object("Table"), DecimalPrecision = 2 });
        Assert.Equal(2, table.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);

        Assert.Throws<ArgumentException>(() => SetCustomTableCellColorOperation.CreateCommand(new()));
        var color = SetCustomTableCellColorOperation.CreateCommand(new() { TableName = Object("Table") });
        Assert.Equal(new WorkerRgbColorValue(255, 0, 0), color.InputArguments[3].RequireValue<WorkerRgbColorValue>());
        var customColor = SetCustomTableCellColorOperation.CreateCommand(new()
        {
            TableName = Object("Table"), ForegroundColorName = new Api.Color { Red = 1, Green = 2, Blue = 3 }
        });
        Assert.Equal(new WorkerRgbColorValue(1, 2, 3), customColor.InputArguments[3].RequireValue<WorkerRgbColorValue>());
        Assert.Equal(new WorkerRgbColorValue(255, 0, 0), customColor.InputArguments[4].RequireValue<WorkerRgbColorValue>());

        Assert.Throws<ArgumentException>(() => SetCustomTableCellDoubleOperation.CreateCommand(new()));
        var cellDouble = SetCustomTableCellDoubleOperation.CreateCommand(new() { TableName = Object("Table") });
        Assert.Equal(0d, cellDouble.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal([1, -1], cellDouble.InputArguments.Skip(4)
            .Select(argument => argument.RequireValue<WorkerIntegerValue>().Value));
        cellDouble = SetCustomTableCellDoubleOperation.CreateCommand(new()
        {
            TableName = Object("Table"), Value = 2.5, Span = 3, DecimalPrecision = 4
        });
        Assert.Equal(2.5, cellDouble.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal([3, 4], cellDouble.InputArguments.Skip(4)
            .Select(argument => argument.RequireValue<WorkerIntegerValue>().Value));

        Assert.Throws<ArgumentException>(() => SetCustomTableCellFontOperation.CreateCommand(new()));
        var font = SetCustomTableCellFontOperation.CreateCommand(new() { TableName = Object("Table") })
            .InputArguments[3].RequireValue<WorkerFontValue>();
        Assert.Equal("MS Shell Dlg", font.FontName);
        Assert.Equal((byte)8, font.Size);
        Assert.Equal(new WorkerRgbColorValue(0, 0, 0), font.Color);

        Assert.Throws<ArgumentException>(() => SetCustomTableCellStringOperation.CreateCommand(new()));
        var cellString = SetCustomTableCellStringOperation.CreateCommand(new() { TableName = Object("Table") });
        Assert.Equal(string.Empty, cellString.InputArguments[3].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(1, cellString.InputArguments[4].RequireValue<WorkerIntegerValue>().Value);

        Assert.Throws<ArgumentException>(() => SetCustomTableHeaderCellOperation.CreateCommand(new()));
        var headerCell = SetCustomTableHeaderCellOperation.CreateCommand(new() { TableName = Object("Table") });
        Assert.Equal(string.Empty, headerCell.InputArguments[3].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(1, headerCell.InputArguments[4].RequireValue<WorkerIntegerValue>().Value);

        Assert.Throws<ArgumentException>(() => SetCustomTableHeaderRowOperation.CreateCommand(new()));
        var headerRow = SetCustomTableHeaderRowOperation.CreateCommand(new() { TableName = Object("Table") });
        Assert.Equal(0, headerRow.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(string.Empty, headerRow.InputArguments[2].RequireValue<WorkerTextValue>().Value);

        Assert.Throws<ArgumentException>(() => SetCustomTableTitleOperation.CreateCommand(new()));
        var title = SetCustomTableTitleOperation.CreateCommand(new() { TableName = Object("Table") });
        Assert.Equal([string.Empty, string.Empty], title.InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerTextValue>().Value));
    }

    [Fact]
    public async Task GeneratedClientRoutesCustomTableOperationsThroughTypedMappings()
    {
        var worker = new CustomTableWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ReportingOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ReportingOperations.ReportingOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(30));

        await client.AddCustomTableToSaReportAsync(new() { TableName = Object("Table"), ReportName = Object("Report") }, options);
        await client.AddCustomTablesToReportBarAsync(new() { CustomTablesToReport = { Item("Table") } }, options);
        await client.ClearCustomTableAsync(new() { TableName = Object("Table") }, options);
        await client.DeleteCustomTableAsync(new() { TableName = Object("Table") }, options);
        var doubleResult = await client.GetCustomTableCellDoubleAsync(new() { TableName = Object("Table") }, options);
        var stringResult = await client.GetCustomTableCellStringAsync(new() { TableName = Object("Table") }, options);
        await client.MakeCustomTableAsync(new() { TableName = Object("Table") }, options);
        await client.SetCustomTableCellColorAsync(new() { TableName = Object("Table") }, options);
        await client.SetCustomTableCellDoubleAsync(new() { TableName = Object("Table") }, options);
        await client.SetCustomTableCellFontAsync(new() { TableName = Object("Table") }, options);
        await client.SetCustomTableCellStringAsync(new() { TableName = Object("Table") }, options);
        await client.SetCustomTableHeaderCellAsync(new() { TableName = Object("Table") }, options);
        await client.SetCustomTableHeaderRowAsync(new() { TableName = Object("Table") }, options);
        await client.SetCustomTableTitleAsync(new() { TableName = Object("Table") }, options);

        Assert.Equal(12.5, doubleResult.Value);
        Assert.Equal("Cell", stringResult.Value);
        Assert.Equal(Ids, worker.Commands.Select(command => command.OperationId));
        string[][] expectedBindings =
        [
            ["SetCollectionObjectNameArg2", "SetCollectionObjectNameArg2", "SetBoolArg"],
            ["SetCollectionObjectNameRefListArg", "SetBoolArg"],
            ["SetCollectionObjectNameArg2"],
            ["SetCollectionObjectNameArg2"],
            ["SetCollectionObjectNameArg2", "SetIntegerArg", "SetIntegerArg"],
            ["SetCollectionObjectNameArg2", "SetIntegerArg", "SetIntegerArg"],
            ["SetCollectionObjectNameArg2", "SetIntegerArg"],
            ["SetCollectionObjectNameArg2", "SetIntegerArg", "SetIntegerArg", "SetColorArg", "SetColorArg"],
            ["SetCollectionObjectNameArg2", "SetIntegerArg", "SetIntegerArg", "SetDoubleArg", "SetIntegerArg", "SetIntegerArg"],
            ["SetCollectionObjectNameArg2", "SetIntegerArg", "SetIntegerArg", "SetFontTypeArg"],
            ["SetCollectionObjectNameArg2", "SetIntegerArg", "SetIntegerArg", "SetStringArg", "SetIntegerArg"],
            ["SetCollectionObjectNameArg2", "SetIntegerArg", "SetIntegerArg", "SetStringArg", "SetIntegerArg"],
            ["SetCollectionObjectNameArg2", "SetIntegerArg", "SetStringArg"],
            ["SetCollectionObjectNameArg2", "SetStringArg", "SetStringArg"]
        ];
        Assert.Equal(expectedBindings,
            worker.Commands.Select(command => command.InputArguments.Select(argument => argument.SdkBinding).ToArray()).ToArray());
        Assert.Equal("GetDoubleArg", worker.Commands[4].OutputArguments[0].SdkBinding);
        Assert.Equal("GetStringArg", worker.Commands[5].OutputArguments[0].SdkBinding);
    }

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

    private static SuccessfulOperationExecution Success(IReadOnlyList<WorkerMpOutputValue> outputs)
    {
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
        return new(execution, new Api.MpExecutionDetails());
    }

    private sealed class CustomTableWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "reporting_operations.get_custom_table_cell_double" =>
                    [new WorkerRetrievedOutput("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(12.5))],
                "reporting_operations.get_custom_table_cell_string" =>
                    [new WorkerRetrievedOutput("Value", WorkerMpValueKind.Text, new WorkerTextValue("Cell"))],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
