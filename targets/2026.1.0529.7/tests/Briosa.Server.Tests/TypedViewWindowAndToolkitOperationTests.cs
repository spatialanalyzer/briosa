using Briosa.Server.Operations;
using Briosa.Server.Operations.ViewControl;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedViewWindowAndToolkitOperationTests
{
    private static readonly string[] Ids =
    [
        "view_control.hide_all_callout_views", "view_control.load_ribbon_bar_from_xml_file",
        "view_control.reset_ribbon_bar_to_default", "view_control.set_mp_window_state",
        "view_control.set_sa_window_pos", "view_control.set_sa_window_size",
        "view_control.set_sa_window_state", "view_control.set_toolkit_visibility",
        "view_control.show_hide_callout_view", "view_control.show_hide_inspection_bar",
        "view_control.show_hide_instrument_interface", "view_control.show_hide_instrument_probe_tip",
        "view_control.show_hide_relationship_report", "view_control.show_hide_relationship_watch",
        "view_control.show_items_in_tree"
    ];

    [Fact]
    public void WindowAndToolkitOperationsAreRegisteredAndRetainTargetStepNames()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Equal("Set MP's Window State", SetMpWindowStateOperation.Descriptor.MpStep);
        Assert.Equal("Set SA's Window Pos", SetSaWindowPosOperation.Descriptor.MpStep);
        Assert.Equal("Set SA's Window Size", SetSaWindowSizeOperation.Descriptor.MpStep);
        Assert.Equal("Set SA's Window State", SetSaWindowStateOperation.Descriptor.MpStep);
        Assert.Equal("Show / Hide Callout View", ShowHideCalloutViewOperation.Descriptor.MpStep);
        Assert.Contains("fixture_validation_pending", LoadRibbonBarFromXmlFileOperation.Descriptor.RiskFlags);
        Assert.Contains("fixture_validation_pending", SetMpWindowStateOperation.Descriptor.RiskFlags);
    }

    [Fact]
    public void WindowAndToolkitMappingsPreserveRequiredFieldsAndDefaults()
    {
        Assert.Empty(HideAllCalloutViewsOperation.CreateCommand(new()).InputArguments);
        Assert.Empty(ResetRibbonBarToDefaultOperation.CreateCommand(new()).InputArguments);

        Assert.Throws<ArgumentException>(() => LoadRibbonBarFromXmlFileOperation.CreateCommand(new()));
        var ribbon = LoadRibbonBarFromXmlFileOperation.CreateCommand(new()
        {
            FilePath = new Api.FileReference { Path = "C:\\tools\\ribbon.xml", EmbeddedFile = true }
        });
        var file = ribbon.InputArguments[0].RequireValue<WorkerFileReferenceValue>();
        Assert.Equal("C:\\tools\\ribbon.xml", file.Path);
        Assert.True(file.EmbeddedFile);

        Assert.Throws<ArgumentException>(() => SetMpWindowStateOperation.CreateCommand(new()));
        Assert.Equal(WorkerWindowStateValue.Maximize,
            SetMpWindowStateOperation.CreateCommand(new() { MpWindowState = Api.WindowState.Maximize })
                .InputArguments[0].RequireValue<WorkerChoiceValue<WorkerWindowStateValue>>().Value);
        var position = SetSaWindowPosOperation.CreateCommand(new());
        Assert.Equal([0, 0], position.InputArguments
            .Select(argument => argument.RequireValue<WorkerIntegerValue>().Value));
        Assert.Equal([120, 240], SetSaWindowPosOperation.CreateCommand(new Api.SetSaWindowPosRequest { PosX = 120, PosY = 240 })
            .InputArguments.Select(argument => argument.RequireValue<WorkerIntegerValue>().Value));
        Assert.Equal([800, 600], SetSaWindowSizeOperation.CreateCommand(new Api.SetSaWindowSizeRequest { Width = 800, Height = 600 })
            .InputArguments.Select(argument => argument.RequireValue<WorkerIntegerValue>().Value));
        Assert.Throws<ArgumentException>(() => SetSaWindowStateOperation.CreateCommand(new()));
        Assert.Equal(WorkerWindowStateValue.Restore,
            SetSaWindowStateOperation.CreateCommand(new() { SaWindowState = Api.WindowState.Restore })
                .InputArguments[0].RequireValue<WorkerChoiceValue<WorkerWindowStateValue>>().Value);

        Assert.False(SetToolkitVisibilityOperation.CreateCommand(new()).InputArguments[0]
            .RequireValue<WorkerBooleanValue>().Value);
        Assert.True(SetToolkitVisibilityOperation.CreateCommand(new Api.SetToolkitVisibilityRequest { ShowToolkit = true }).InputArguments[0]
            .RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => ShowHideCalloutViewOperation.CreateCommand(new()));
        var callout = ShowHideCalloutViewOperation.CreateCommand(new()
        {
            CalloutViewToShow = new Api.CollectionItemName { CollectionName = "Parts", ItemName = "View" }
        });
        Assert.Equal("SetCollectionObjectNameArg2", callout.InputArguments[0].SdkBinding);
        Assert.True(callout.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(ShowHideInspectionBarOperation.CreateCommand(new()).InputArguments[0]
            .RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => ShowHideInstrumentInterfaceOperation.CreateCommand(new()));
        var interfaceCommand = ShowHideInstrumentInterfaceOperation.CreateCommand(new()
        {
            InstrumentId = Instrument(3)
        });
        Assert.Equal(new WorkerCollectionInstrumentIdValue("Parts", 3),
            interfaceCommand.InputArguments[0].RequireValue<WorkerCollectionInstrumentIdValue>());
        Assert.Equal([false, false], interfaceCommand.InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerBooleanValue>().Value));
        Assert.False(ShowHideInstrumentProbeTipOperation.CreateCommand(new()).InputArguments[0]
            .RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => ShowHideRelationshipReportOperation.CreateCommand(new()));
        var report = ShowHideRelationshipReportOperation.CreateCommand(new()
        {
            CollectionName = new Api.CollectionName { Name = "Parts" },
            ShowRelationshipReport = true
        });
        Assert.Equal("Parts", report.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.True(report.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => ShowHideRelationshipWatchOperation.CreateCommand(new()));
        var watch = ShowHideRelationshipWatchOperation.CreateCommand(new()
        {
            RelationshipName = Object("Alignment", Api.ObjectType.Unspecified),
            RelationshipWatchWindowProperties = Object("Watch", Api.ObjectType.Unspecified)
        });
        Assert.False(watch.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal([0, 0, 0, 0], watch.InputArguments.Skip(3)
            .Select(argument => argument.RequireValue<WorkerIntegerValue>().Value));

        Assert.Throws<ArgumentException>(() => ShowItemsInTreeOperation.CreateCommand(new()));
        var tree = ShowItemsInTreeOperation.CreateCommand(new()
        {
            Points = { new Api.PointName { CollectionName = "Parts", GroupName = "Targets", TargetName = "P1" } },
            Objects = { Object("Plane", Api.ObjectType.Plane) },
            Instruments = { Instrument(3) },
            FeatureChecks = { new Api.CollectionItemName { CollectionName = "Parts", ItemName = "Check" } },
            Datums = { Object("Datum", Api.ObjectType.Datum) },
            Collections = { "Parts" }
        });
        Assert.True(tree.InputArguments[0].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("P1", tree.InputArguments[1].RequireValue<WorkerPointNameListValue>().Values[0].TargetName);
        Assert.Equal(3, tree.InputArguments[3].RequireValue<WorkerCollectionInstrumentIdListValue>().Values[0].InstrumentId);
        Assert.Equal("Parts", tree.InputArguments[6].RequireValue<WorkerStringListValue>().Values[0]);
    }

    [Fact]
    public async Task GeneratedClientRoutesAllWindowAndToolkitOperationsThroughTypedMappings()
    {
        var worker = new ViewWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ViewControlService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ViewControl.ViewControlClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        await client.HideAllCalloutViewsAsync(new(), options);
        await client.LoadRibbonBarFromXmlFileAsync(new() { FilePath = new Api.FileReference { Path = "ribbon.xml" } }, options);
        await client.ResetRibbonBarToDefaultAsync(new(), options);
        await client.SetMpWindowStateAsync(new() { MpWindowState = Api.WindowState.Show }, options);
        await client.SetSaWindowPosAsync(new() { PosX = 10, PosY = 20 }, options);
        await client.SetSaWindowSizeAsync(new() { Width = 640, Height = 480 }, options);
        await client.SetSaWindowStateAsync(new() { SaWindowState = Api.WindowState.Hide }, options);
        await client.SetToolkitVisibilityAsync(new() { ShowToolkit = true }, options);
        await client.ShowHideCalloutViewAsync(new()
        {
            CalloutViewToShow = new Api.CollectionItemName { CollectionName = "Parts", ItemName = "Callout" }
        }, options);
        await client.ShowHideInspectionBarAsync(new(), options);
        await client.ShowHideInstrumentInterfaceAsync(new() { InstrumentId = Instrument(3) }, options);
        await client.ShowHideInstrumentProbeTipAsync(new(), options);
        await client.ShowHideRelationshipReportAsync(new() { CollectionName = new Api.CollectionName { Name = "Parts" } }, options);
        await client.ShowHideRelationshipWatchAsync(new()
        {
            RelationshipName = Object("Alignment", Api.ObjectType.Unspecified),
            RelationshipWatchWindowProperties = Object("Watch", Api.ObjectType.Unspecified)
        }, options);
        await client.ShowItemsInTreeAsync(new()
        {
            Points = { new Api.PointName { CollectionName = "Parts", GroupName = "Targets", TargetName = "P1" } },
            Objects = { Object("Plane", Api.ObjectType.Plane) },
            Instruments = { Instrument(3) },
            FeatureChecks = { new Api.CollectionItemName { CollectionName = "Parts", ItemName = "Check" } },
            Datums = { Object("Datum", Api.ObjectType.Datum) },
            Collections = { "Parts" }
        }, options);

        Assert.Equal(Ids, worker.Commands.Select(command => command.OperationId));
        Assert.All(worker.Commands, command => Assert.Empty(command.OutputArguments));
    }

    private static Api.CollectionInstrumentId Instrument(int id) => new()
    {
        CollectionName = "Parts",
        InstrumentId = id
    };

    private static Api.CollectionObjectName Object(string name, Api.ObjectType type) => new()
    {
        CollectionName = "Parts",
        ObjectName = name,
        ObjectType = type
    };

    private sealed class ViewWorker : IWorkerCommandExecutor
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
