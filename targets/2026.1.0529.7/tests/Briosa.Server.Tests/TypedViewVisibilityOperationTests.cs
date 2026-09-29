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

public sealed class TypedViewVisibilityOperationTests
{
    private static readonly string[] Ids =
    [
        "view_control.hide_objects", "view_control.show_by_object_type", "view_control.show_hide_annotations_for_datums",
        "view_control.show_hide_annotations_for_feature_checks", "view_control.show_hide_by_object_type",
        "view_control.show_hide_dimension", "view_control.show_hide_instruments", "view_control.show_hide_points",
        "view_control.show_labels", "view_control.show_objects"
    ];

    [Fact]
    public void VisibilityOperationsAreRegisteredAndRemovedFromCatalog()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var expectedTypeStep = SpatialAnalyzerApi.TargetVersion.StartsWith("2024", StringComparison.Ordinal)
            ? "Show / Hide by Object Type" : "Show/Hide by Object Type";
        var expectedPointsStep = SpatialAnalyzerApi.TargetVersion.StartsWith("2024", StringComparison.Ordinal)
            ? "Show / Hide Points" : "Show/Hide Points";
        Assert.Equal(expectedTypeStep, ShowHideByObjectTypeOperation.Descriptor.MpStep);
        Assert.Equal(expectedPointsStep, ShowHidePointsOperation.Descriptor.MpStep);
    }

    [Fact]
    public void VisibilityMappingsPreserveBindingsDefaultsAndRequiredReferences()
    {
        Assert.Throws<ArgumentException>(() => HideObjectsOperation.CreateCommand(new()));
        var hidden = HideObjectsOperation.CreateCommand(new()
        {
            ObjectsToHide = { Object("Part") }
        });
        Assert.Equal("SetCollectionObjectNameRefListArg", hidden.InputArguments[0].SdkBinding);
        Assert.Equal("Part", hidden.InputArguments[0].RequireValue<WorkerCollectionObjectNameListValue>()
            .Values[0].ObjectName);

        Assert.Throws<ArgumentException>(() => ShowHideByObjectTypeOperation.CreateCommand(new()));
        var byType = ShowHideByObjectTypeOperation.CreateCommand(new()
        {
            SpecificCollection = new Api.CollectionName { Name = "Parts" }
        });
        Assert.Equal("Parts", byType.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Any,
            byType.InputArguments[2].RequireValue<WorkerChoiceValue<WorkerObjectTypeValue>>().Value);
        Assert.True(byType.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => ShowHideByObjectTypeOperation.CreateCommand(new()
        {
            SpecificCollection = new Api.CollectionName { Name = "Parts" },
            ObjectTypeToShowHide = Api.ObjectType.Unspecified
        }));

        Assert.Throws<ArgumentException>(() => ShowHideDimensionOperation.CreateCommand(new()));
        var dimension = ShowHideDimensionOperation.CreateCommand(new()
        {
            DimensionName = new Api.CollectionItemName { CollectionName = "Parts", ItemName = "Diameter" }
        });
        Assert.Equal("SetCollectionObjectNameArg2", dimension.InputArguments[0].SdkBinding);
        Assert.True(dimension.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => ShowHidePointsOperation.CreateCommand(new()));
        var points = ShowHidePointsOperation.CreateCommand(new()
        {
            PointNames = { new Api.PointName { CollectionName = "Parts", GroupName = "Targets", TargetName = "P1" } }
        });
        Assert.Equal("SetPointNameRefListArg", points.InputArguments[0].SdkBinding);
        Assert.Equal("P1", points.InputArguments[0].RequireValue<WorkerPointNameListValue>().Values[0].TargetName);
        Assert.False(points.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => ShowByObjectTypeOperation.CreateCommand(new()));
        var shownByType = ShowByObjectTypeOperation.CreateCommand(new()
        {
            ObjectTypeToShow = Object("Surface", Api.ObjectType.Surface)
        });
        Assert.Equal(WorkerObjectTypeValue.Surface,
            shownByType.InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.False(shownByType.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        var labels = ShowLabelsOperation.CreateCommand(new());
        Assert.All(labels.InputArguments, argument => Assert.False(argument.RequireValue<WorkerBooleanValue>().Value));
        Assert.Throws<ArgumentException>(() => ShowObjectsOperation.CreateCommand(new()));
        Assert.Equal("SetCollectionObjectNameRefListArg", ShowObjectsOperation.CreateCommand(new()
        {
            ObjectsToShow = { Object("Part") }
        }).InputArguments[0].SdkBinding);

        Assert.Throws<ArgumentException>(() => ShowHideAnnotationsForDatumsOperation.CreateCommand(new()));
        var datumAnnotations = ShowHideAnnotationsForDatumsOperation.CreateCommand(new()
        {
            DatumNameList = { Object("Datum", Api.ObjectType.Datum) }
        });
        Assert.All(datumAnnotations.InputArguments.Skip(1), argument =>
            Assert.False(argument.RequireValue<WorkerBooleanValue>().Value));

        Assert.Throws<ArgumentException>(() => ShowHideAnnotationsForFeatureChecksOperation.CreateCommand(new()));
        var featureAnnotations = ShowHideAnnotationsForFeatureChecksOperation.CreateCommand(new()
        {
            FeatureCheckNameList = { new Api.CollectionItemName { CollectionName = "Parts", ItemName = "FC1" } }
        });
        Assert.Equal("SetCollectionObjectNameRefListArg", featureAnnotations.InputArguments[0].SdkBinding);
        Assert.All(featureAnnotations.InputArguments.Skip(1), argument =>
            Assert.False(argument.RequireValue<WorkerBooleanValue>().Value));

        Assert.Throws<ArgumentException>(() => ShowHideInstrumentsOperation.CreateCommand(new()));
        var instruments = ShowHideInstrumentsOperation.CreateCommand(new()
        {
            InstrumentIDs = { new Api.CollectionInstrumentId { CollectionName = "Parts", InstrumentId = 7 } }
        });
        Assert.Equal("SetColInstIdRefListArg", instruments.InputArguments[0].SdkBinding);
        Assert.Equal(7, instruments.InputArguments[0].RequireValue<WorkerCollectionInstrumentIdListValue>()
            .Values[0].InstrumentId);
        Assert.False(instruments.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => ShowHideInstrumentsOperation.CreateCommand(new()
        {
            InstrumentIDs = { new Api.CollectionInstrumentId { InstrumentId = 7 } }
        }));
    }

    [Fact]
    public async Task GeneratedClientRoutesVisibilityThroughTypedWorkerMappings()
    {
        var worker = new ViewWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ViewControlService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ViewControl.ViewControlClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var hidden = await client.HideObjectsAsync(new()
        {
            ObjectsToHide = { Object("Part") }
        }, options);
        var labels = await client.ShowLabelsAsync(new(), options);

        Assert.Equal(Api.MpExecutionState.Succeeded, hidden.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, labels.Execution.State);
        Assert.Equal(["view_control.hide_objects", "view_control.show_labels"],
            worker.Commands.Select(command => command.OperationId));
    }

    private static Api.CollectionObjectName Object(string name, Api.ObjectType type = Api.ObjectType.Any) => new()
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
