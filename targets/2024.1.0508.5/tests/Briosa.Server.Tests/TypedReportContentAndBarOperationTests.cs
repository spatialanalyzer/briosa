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

public sealed class TypedReportContentAndBarOperationTests
{
    private static readonly string[] Ids =
    [
        "reporting_operations.add_datums_to_report_bar", "reporting_operations.add_events_to_report_bar",
        "reporting_operations.add_feature_checks_to_report_bar", "reporting_operations.add_item_to_sa_report_at_location",
        "reporting_operations.add_objects_to_report_bar", "reporting_operations.add_relationships_to_report_bar",
        "reporting_operations.append_items_to_sa_report", "reporting_operations.refresh_callout_views_in_sa_report",
        "reporting_operations.refresh_report_bar", "reporting_operations.set_report_bar_visibility"
    ];

    [Fact]
    public void ReportContentAndBarOperationsAreRegisteredAndRemovedFromTheCatalog()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Equal("Add Datums to Report Bar", AddDatumsToReportBarOperation.Descriptor.MpStep);
        Assert.Equal("Append Items to SA Report", AppendItemsToSaReportOperation.Descriptor.MpStep);
        Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation,
            SetReportBarVisibilityOperation.Descriptor.ExecutionScope);
        Assert.Equal(Api.ReplaySafety.Unsafe, RefreshReportBarOperation.Descriptor.ReplaySafety);
    }

    [Fact]
    public void ReportContentAndBarMappingsPreserveListBindingsAndOptionalDefaults()
    {
        Assert.Throws<ArgumentException>(() => AddDatumsToReportBarOperation.CreateCommand(new()));
        var datums = AddDatumsToReportBarOperation.CreateCommand(new()
        {
            Datums = { Object("Datum", Api.ObjectType.Datum) }
        });
        Assert.Equal("SetCollectionObjectNameRefListArg", datums.InputArguments[0].SdkBinding);
        Assert.False(datums.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        var events = AddEventsToReportBarOperation.CreateCommand(new()
        {
            Events = { Item("Event") }, ClearExisting = true
        });
        Assert.Equal(WorkerMpValueKind.CollectionItemNameList, events.InputArguments[0].Kind);
        Assert.True(events.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        var checks = AddFeatureChecksToReportBarOperation.CreateCommand(new()
        {
            FeatureChecks = { Item("Check") }
        });
        Assert.Equal("Check", checks.InputArguments[0]
            .RequireValue<WorkerCollectionItemNameListValue>().Values[0].ItemName);

        Assert.Throws<ArgumentException>(() => AddObjectsToReportBarOperation.CreateCommand(new()));
        var objects = AddObjectsToReportBarOperation.CreateCommand(new()
        {
            Objects = { Object("Plane", Api.ObjectType.Plane) }
        });
        Assert.True(objects.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(AddObjectsToReportBarOperation.CreateCommand(new()
        {
            Objects = { Object("Plane", Api.ObjectType.Plane) }, ClearExisting = false
        }).InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        var relationships = AddRelationshipsToReportBarOperation.CreateCommand(new()
        {
            Relationships = { Item("Alignment") }
        });
        Assert.Equal("SetCollectionObjectNameRefListArg", relationships.InputArguments[0].SdkBinding);
        Assert.False(relationships.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => AddItemToSaReportAtLocationOperation.CreateCommand(new()));
        var reportItem = AddItemToSaReportAtLocationOperation.CreateCommand(new()
        {
            ReportName = Object("Report", Api.ObjectType.Unspecified),
            ItemName = Object("Item", Api.ObjectType.Unspecified)
        });
        Assert.Equal([0], reportItem.InputArguments.Skip(2).Take(1)
            .Select(argument => argument.RequireValue<WorkerIntegerValue>().Value));
        Assert.Equal([1d, 1d], reportItem.InputArguments.Skip(3).Take(2)
            .Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));
        Assert.False(reportItem.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => AppendItemsToSaReportOperation.CreateCommand(new()));
        var append = AppendItemsToSaReportOperation.CreateCommand(new()
        {
            ReportName = Object("Report", Api.ObjectType.Unspecified),
            ItemsToReport = { Object("Item", Api.ObjectType.Unspecified) },
            BeginOnNewPage = true
        });
        Assert.Equal("Item", append.InputArguments[1]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectName);
        Assert.False(append.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(append.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => RefreshCalloutViewsInSaReportOperation.CreateCommand(new()));
        var refreshCallouts = RefreshCalloutViewsInSaReportOperation.CreateCommand(new()
        {
            ReportName = Item("Report")
        });
        Assert.Equal("SetCollectionObjectNameArg2", refreshCallouts.InputArguments[0].SdkBinding);
        Assert.Empty(RefreshReportBarOperation.CreateCommand(new()).InputArguments);
        Assert.False(SetReportBarVisibilityOperation.CreateCommand(new()).InputArguments[0]
            .RequireValue<WorkerBooleanValue>().Value);
        Assert.True(SetReportBarVisibilityOperation.CreateCommand(new() { ShowReportBar = true })
            .InputArguments[0].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientRoutesReportContentAndBarOperationsThroughTypedMappings()
    {
        var worker = new ReportWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ReportingOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ReportingOperations.ReportingOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        await client.AddDatumsToReportBarAsync(new() { Datums = { Object("Datum", Api.ObjectType.Datum) } }, options);
        await client.AddEventsToReportBarAsync(new() { Events = { Item("Event") } }, options);
        await client.AddFeatureChecksToReportBarAsync(new() { FeatureChecks = { Item("Check") } }, options);
        await client.AddItemToSaReportAtLocationAsync(new()
        {
            ReportName = Object("Report", Api.ObjectType.Unspecified),
            ItemName = Object("Item", Api.ObjectType.Unspecified)
        }, options);
        await client.AddObjectsToReportBarAsync(new() { Objects = { Object("Plane", Api.ObjectType.Plane) } }, options);
        await client.AddRelationshipsToReportBarAsync(new() { Relationships = { Item("Alignment") } }, options);
        await client.AppendItemsToSaReportAsync(new()
        {
            ReportName = Object("Report", Api.ObjectType.Unspecified),
            ItemsToReport = { Object("Item", Api.ObjectType.Unspecified) }
        }, options);
        await client.RefreshCalloutViewsInSaReportAsync(new() { ReportName = Item("Report") }, options);
        await client.RefreshReportBarAsync(new(), options);
        await client.SetReportBarVisibilityAsync(new() { ShowReportBar = true }, options);

        Assert.Equal(Ids, worker.Commands.Select(command => command.OperationId));
        Assert.All(worker.Commands, command => Assert.Empty(command.OutputArguments));
    }

    private static Api.CollectionItemName Item(string name) => new()
    {
        CollectionName = "Parts",
        ItemName = name
    };

    private static Api.CollectionObjectName Object(string name, Api.ObjectType type) => new()
    {
        CollectionName = "Parts",
        ObjectName = name,
        ObjectType = type
    };

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
