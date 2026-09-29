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

public sealed class TypedViewAppearanceAndHighlightingOperationTests
{
    private static readonly string[] Ids =
    [
        "view_control.highlight_objects", "view_control.highlight_point", "view_control.highlight_relationships",
        "view_control.set_background_color", "view_control.set_objects_color", "view_control.set_objects_translucency",
        "view_control.set_target_labels_use_full_names", "view_control.set_working_color",
        "view_control.set_working_color_auto_increment"
    ];

    [Fact]
    public void AppearanceAndHighlightingOperationsAreRegisteredAndRemovedFromCatalog()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Equal("Set Background Color", SetBackgroundColorOperation.Descriptor.MpStep);
        Assert.Equal("Highlight Relationships", HighlightRelationshipsOperation.Descriptor.MpStep);
    }

    [Fact]
    public void MappingsPreserveColorDefaultsInputOrderAndEmptyHighlightSemantics()
    {
        var background = SetBackgroundColorOperation.CreateCommand(new());
        Assert.Equal(["SetColorArg", "SetColorArg", "SetColorArg", "SetColorArg"],
            background.InputArguments.Select(argument => argument.SdkBinding));
        Assert.All(background.InputArguments, argument =>
            Assert.Equal(new WorkerRgbColorValue(255, 0, 0), argument.RequireValue<WorkerRgbColorValue>()));
        var explicitColor = SetBackgroundColorOperation.CreateCommand(new()
        {
            SolidColorName = new Api.Color { Red = 12, Green = 34, Blue = 56 }
        });
        Assert.Equal(new WorkerRgbColorValue(12, 34, 56),
            explicitColor.InputArguments[0].RequireValue<WorkerRgbColorValue>());
        Assert.Throws<ArgumentOutOfRangeException>(() => SetWorkingColorOperation.CreateCommand(new()
        {
            NewWorkingColorName = new Api.Color { Red = 256 }
        }));

        Assert.Throws<ArgumentException>(() => SetObjectsColorOperation.CreateCommand(new()));
        var objectColor = SetObjectsColorOperation.CreateCommand(new()
        {
            ObjectsToChange = { new Api.CollectionObjectName
                { CollectionName = "Parts", ObjectName = "Bracket", ObjectType = Api.ObjectType.Surface } }
        });
        Assert.Equal("SetCollectionObjectNameRefListArg", objectColor.InputArguments[0].SdkBinding);
        Assert.Equal("Bracket", objectColor.InputArguments[0].RequireValue<WorkerCollectionObjectNameListValue>()
            .Values[0].ObjectName);
        Assert.Equal(new WorkerRgbColorValue(255, 0, 0),
            objectColor.InputArguments[1].RequireValue<WorkerRgbColorValue>());
        Assert.False(objectColor.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => SetObjectsTranslucencyOperation.CreateCommand(new()));
        var translucency = SetObjectsTranslucencyOperation.CreateCommand(new()
        {
            ObjectsToChange = { new Api.CollectionObjectName
                { CollectionName = "Parts", ObjectName = "Bracket", ObjectType = Api.ObjectType.Surface } },
            RenderingType = Api.TranslucencyType.Translucent,
            OpacityValue = 0.35
        });
        Assert.Equal(WorkerTranslucencyTypeValue.Translucent,
            translucency.InputArguments[1].RequireValue<WorkerChoiceValue<WorkerTranslucencyTypeValue>>().Value);
        Assert.Equal(0.35d, translucency.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);

        var objectHighlights = HighlightObjectsOperation.CreateCommand(new());
        Assert.Empty(objectHighlights.InputArguments[0].RequireValue<WorkerCollectionObjectNameListValue>().Values);
        Assert.False(objectHighlights.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        var pointHighlight = HighlightPointOperation.CreateCommand(new());
        Assert.Equal(new WorkerPointNameValue("", "", ""),
            pointHighlight.InputArguments[0].RequireValue<WorkerPointNameValue>());
        Assert.False(pointHighlight.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        var relationshipHighlights = HighlightRelationshipsOperation.CreateCommand(new());
        Assert.Empty(relationshipHighlights.InputArguments[0].RequireValue<WorkerCollectionItemNameListValue>().Values);
        Assert.False(relationshipHighlights.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        Assert.False(SetTargetLabelsUseFullNamesOperation.CreateCommand(new()).InputArguments[0]
            .RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(new WorkerRgbColorValue(255, 0, 0),
            SetWorkingColorOperation.CreateCommand(new()).InputArguments[0].RequireValue<WorkerRgbColorValue>());
        Assert.False(SetWorkingColorAutoIncrementOperation.CreateCommand(new()).InputArguments[0]
            .RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientRoutesAppearanceAndHighlightingThroughTypedWorkerMappings()
    {
        var worker = new ViewWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ViewControlService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ViewControl.ViewControlClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var color = await client.SetBackgroundColorAsync(new(), options);
        var highlight = await client.HighlightRelationshipsAsync(new(), options);

        Assert.Equal(Api.MpExecutionState.Succeeded, color.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, highlight.Execution.State);
        Assert.Equal(["view_control.set_background_color", "view_control.highlight_relationships"],
            worker.Commands.Select(command => command.OperationId));
    }

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
