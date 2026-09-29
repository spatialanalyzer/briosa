using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Security;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedCalloutViewMigrationTests
{
    private static readonly string[] PositionNames = ["X Position", "Y Position", "X Anchor Position", "Y Anchor Position", "Callout Width", "Callout Height"];
    [Fact]
    public async Task GeneratedClientRoutesEightCalloutViewOperations()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var lifetime = host.ConfigureAwait(true);
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(host.Channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        await client.AutoArrangeCalloutViewAsync(new() { CalloutView = View("a") }, options);
        await client.DeleteCalloutViewAsync(new() { CalloutView = View("a") }, options);
        var position = await client.GetIthCalloutPositionInCalloutViewAsync(new() { CalloutView = View("a"), CalloutViewIndex = 2 }, options);
        var count = await client.GetNumberOfCalloutsInCalloutViewAsync(new() { CalloutView = View("a") }, options);
        await client.RenameCalloutViewAsync(new() { OriginalCalloutViewName = View("a"), NewCalloutViewName = View("b") }, options);
        await client.SetCalloutViewPropertiesAsync(new() { CalloutViews = { View("a") } }, options);
        await client.SetDefaultCalloutViewPropertiesAsync(new(), options);
        await client.SetIthCalloutPositionInCalloutViewAsync(new() { CalloutView = View("a"), CalloutViewIndex = 2, XPosition = 3, YPosition = 4 }, options);

        Assert.Equal(8, worker.Commands.Count);
        Assert.Equal(WorkerItemTypeValue.CalloutView, worker.Commands[0].InputArguments[0].RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.Contains("destructive", DeleteCalloutViewOperation.Descriptor.RiskFlags);
        Assert.Equal(2, worker.Commands[2].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.All(worker.Commands[2].OutputArguments, a => Assert.Equal("GetIntegerArg", a.SdkBinding));
        Assert.Equal([1, 2, 3, 4, 5, 6], new[] { position.XPosition, position.YPosition, position.XAnchorPosition, position.YAnchorPosition, position.CalloutWidth, position.CalloutHeight });
        Assert.Equal(7, count.CalloutsCount);
        Assert.False(worker.Commands[4].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("SetCollectionObjectNameRefListArg", worker.Commands[5].InputArguments[0].SdkBinding);
        Assert.Equal(WorkerItemTypeValue.CalloutView, worker.Commands[5].InputArguments[0].RequireValue<WorkerCollectionItemNameListValue>().Values[0].ItemType);
        Assert.Equal(2, worker.Commands[5].InputArguments[4].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(new WorkerRgbColorValue(128, 128, 128), worker.Commands[5].InputArguments[5].RequireValue<WorkerRgbColorValue>());
        Assert.Equal(new WorkerRgbColorValue(0, 0, 255), worker.Commands[5].InputArguments[7].RequireValue<WorkerRgbColorValue>());
        Assert.Equal("SetFontTypeArg", worker.Commands[5].InputArguments[9].SdkBinding);
        Assert.Equal("Callout 1", worker.Commands[6].InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(["Callout View", "Callout View Index", "X Position", "Y Position"], worker.Commands[7].InputArguments.Select(a => a.Name));
        foreach (var id in new[] { "auto_arrange_callout_view", "delete_callout_view", "get_ith_callout_position_in_callout_view", "get_number_of_callouts_in_callout_view", "rename_callout_view", "set_callout_view_properties", "set_default_callout_view_properties", "set_ith_callout_position_in_callout_view" })
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == $"construction_operations.{id}");
        }
    }

    [Fact]
    public void InvalidCalloutViewInputsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => AutoArrangeCalloutViewOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => DeleteCalloutViewOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetIthCalloutPositionInCalloutViewOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetNumberOfCalloutsInCalloutViewOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => RenameCalloutViewOperation.CreateCommand(new() { OriginalCalloutViewName = View("a") }));
        Assert.Throws<ArgumentException>(() => SetCalloutViewPropertiesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetIthCalloutPositionInCalloutViewOperation.CreateCommand(new()));
        Assert.Throws<ArgumentOutOfRangeException>(() => SetDefaultCalloutViewPropertiesOperation.CreateCommand(new()
        {
            Properties = new() { CalloutLeaderColor = new() { Red = 256 } }
        }));
    }

    private static Api.CollectionItemName View(string name) => new() { CollectionName = "part", ItemName = name };

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];
        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "construction_operations.get_ith_callout_position_in_callout_view" =>
                    Enumerable.Range(1, 6).Select((value, index) => new WorkerRetrievedOutput(
                        PositionNames[index],
                        WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(value))).ToArray(),
                "construction_operations.get_number_of_callouts_in_callout_view" =>
                    [new WorkerRetrievedOutput("Callouts Count", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(7))],
                _ => []
            };
            var result = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, result, null, "completed", 1));
        }
    }
}
