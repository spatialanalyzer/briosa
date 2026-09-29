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

public sealed class TypedViewCameraAndClippingOperationTests
{
    private static readonly string[] Ids =
    [
        "view_control.auto_scale", "view_control.center_graphics_about_objects",
        "view_control.center_graphics_about_point", "view_control.define_point_of_view",
        "view_control.get_active_clipping_planes", "view_control.get_point_of_view_parameters",
        "view_control.refresh_views", "view_control.save_point_of_view", "view_control.set_point_of_view",
        "view_control.set_point_of_view_from_frame", "view_control.set_point_of_view_from_instrument_updates",
        "view_control.set_render_mode_type", "view_control.set_view_clipping_plane"
    ];

    [Fact]
    public void CameraAndClippingOperationsAreRegisteredWithCorrectExecutionSafety()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        foreach (var operation in new[]
        {
            GetActiveClippingPlanesOperation.Descriptor,
            GetPointOfViewParametersOperation.Descriptor
        })
        {
            Assert.Equal(Api.OperationExecutionScope.GlobalStateRead, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Safe, operation.ReplaySafety);
        }

        Assert.Equal(SpatialAnalyzerApi.TargetVersion.StartsWith("2024", StringComparison.Ordinal)
                ? "Define point of view" : "Define Point of View",
            DefinePointOfViewOperation.Descriptor.MpStep);
        Assert.Equal(SpatialAnalyzerApi.TargetVersion.StartsWith("2024", StringComparison.Ordinal)
                ? "Set Point of View from Frame" : "Set Point of View From Frame",
            SetPointOfViewFromFrameOperation.Descriptor.MpStep);
        Assert.Contains("fixture_validation_pending",
            SetPointOfViewFromInstrumentUpdatesOperation.Descriptor.RiskFlags);
    }

    [Fact]
    public void CameraAndClippingMappingsPreserveDefaultsChoicesAndOutputs()
    {
        Assert.Empty(AutoScaleOperation.CreateCommand(new()).InputArguments);
        var centerObjects = CenterGraphicsAboutObjectsOperation.CreateCommand(new());
        Assert.Equal(WorkerObjectTypeValue.Any,
            centerObjects.InputArguments[0].RequireValue<WorkerChoiceValue<WorkerObjectTypeValue>>().Value);
        Assert.Equal(["*", "*"], centerObjects.InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerTextValue>().Value));
        Assert.Throws<ArgumentException>(() => CenterGraphicsAboutPointOperation.CreateCommand(new()));

        Assert.Throws<ArgumentException>(() => DefinePointOfViewOperation.CreateCommand(new()));
        var defined = DefinePointOfViewOperation.CreateCommand(new()
        {
            ViewName = new Api.ViewName { Name = "Front" }
        });
        Assert.Equal("Front", defined.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal([0d, 0d, 0d], defined.InputArguments.Skip(1).Take(3)
            .Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));
        Assert.Equal(1d, defined.InputArguments[5].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(WorkerRenderModeTypeValue.Wireframe,
            defined.InputArguments[9].RequireValue<WorkerChoiceValue<WorkerRenderModeTypeValue>>().Value);

        var clippingQuery = GetActiveClippingPlanesOperation.CreateCommand(new());
        Assert.Empty(clippingQuery.InputArguments);
        Assert.Equal("GetCollectionObjectNameRefListArg", clippingQuery.OutputArguments[0].SdkBinding);
        var clippingResult = GetActiveClippingPlanesOperation.CreateResult(Success(
        [
            new WorkerRetrievedOutput("Objects", WorkerMpValueKind.CollectionObjectNameList,
                new WorkerCollectionObjectNameListValue(
                [new WorkerCollectionObjectNameValue("Parts", "ClipPlane", WorkerObjectTypeValue.Plane)]))
        ]));
        Assert.Single(clippingResult.Objects);
        Assert.Equal("ClipPlane", clippingResult.Objects[0].ObjectName);

        Assert.Throws<ArgumentException>(() => GetPointOfViewParametersOperation.CreateCommand(new()));
        var viewParameters = GetPointOfViewParametersOperation.CreateCommand(new()
        {
            ViewName = new Api.ViewName { Name = "Front" }
        });
        Assert.Equal(8, viewParameters.OutputArguments.Count);
        var viewResult = GetPointOfViewParametersOperation.CreateResult(Success(
        [
            new WorkerRetrievedOutput("Rotation (x)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1d)),
            new WorkerRetrievedOutput("Rotation (y)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2d)),
            new WorkerRetrievedOutput("Rotation (z)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(3d)),
            new WorkerRetrievedOutput("Restore Zoom Settings?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
            new WorkerRetrievedOutput("Scale Factor", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(4d)),
            new WorkerRetrievedOutput("Origin (x)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(5d)),
            new WorkerRetrievedOutput("Origin (y)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(6d)),
            new WorkerRetrievedOutput("Restore Render Mode?", WorkerMpValueKind.Logical, new WorkerBooleanValue(false))
        ]));
        Assert.Equal([1d, 2d, 3d, 4d, 5d, 6d], new[]
        {
            viewResult.RotationX, viewResult.RotationY, viewResult.RotationZ,
            viewResult.ScaleFactor, viewResult.OriginX, viewResult.OriginY
        });
        Assert.True(viewResult.RestoreZoomSettings);
        Assert.False(viewResult.RestoreRenderMode);

        Assert.Throws<ArgumentException>(() => SetRenderModeTypeOperation.CreateCommand(new()));
        var mode = SetRenderModeTypeOperation.CreateCommand(new()
        {
            RenderingMode = Api.RenderModeType.SolidAndEdges
        });
        Assert.Equal(WorkerRenderModeTypeValue.SolidAndEdges,
            mode.InputArguments[0].RequireValue<WorkerChoiceValue<WorkerRenderModeTypeValue>>().Value);

        Assert.Throws<ArgumentException>(() => SetViewClippingPlaneOperation.CreateCommand(new()));
        var clippingPlane = SetViewClippingPlaneOperation.CreateCommand(new()
        {
            Object = Object("ClipPlane", Api.ObjectType.Plane)
        });
        Assert.Equal("SetCollectionObjectNameArg2", clippingPlane.InputArguments[0].SdkBinding);
        Assert.False(clippingPlane.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => SavePointOfViewOperation.CreateCommand(new()));
        var saved = SavePointOfViewOperation.CreateCommand(new()
        {
            ViewName = new Api.ViewName { Name = "Front" }
        });
        Assert.True(saved.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Empty(RefreshViewsOperation.CreateCommand(new()).InputArguments);
        Assert.Throws<ArgumentException>(() => SetPointOfViewOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetPointOfViewFromFrameOperation.CreateCommand(new()));

        Assert.Throws<ArgumentException>(() => SetPointOfViewFromInstrumentUpdatesOperation.CreateCommand(new()));
        var instrumentUpdates = SetPointOfViewFromInstrumentUpdatesOperation.CreateCommand(new()
        {
            InstrumentId = new Api.CollectionInstrumentId { CollectionName = "Parts", InstrumentId = 7 },
            ReferenceFrameObject = Object("Frame", Api.ObjectType.Frame)
        });
        Assert.Equal(12, instrumentUpdates.InputArguments.Count);
        Assert.Equal(75d, instrumentUpdates.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(45d, instrumentUpdates.InputArguments[7].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(8, instrumentUpdates.InputArguments[8].RequireValue<WorkerIntegerValue>().Value);
        Assert.True(instrumentUpdates.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(instrumentUpdates.InputArguments[10].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(1d, instrumentUpdates.InputArguments[11].RequireValue<WorkerDoubleValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientRoutesCameraAndClippingOperationsThroughTypedWorkerMappings()
    {
        var worker = new ViewWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ViewControlService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ViewControl.ViewControlClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var clipping = await client.GetActiveClippingPlanesAsync(new(), options);
        var parameters = await client.GetPointOfViewParametersAsync(new()
        {
            ViewName = new Api.ViewName { Name = "Front" }
        }, options);
        var setClipping = await client.SetViewClippingPlaneAsync(new()
        {
            Object = Object("ClipPlane", Api.ObjectType.Plane)
        }, options);

        Assert.Equal("ClipPlane", Assert.Single(clipping.Objects).ObjectName);
        Assert.Equal(10d, parameters.RotationX);
        Assert.Equal(Api.MpExecutionState.Succeeded, setClipping.Execution.State);
        Assert.Equal([
            "view_control.get_active_clipping_planes", "view_control.get_point_of_view_parameters",
            "view_control.set_view_clipping_plane"
        ], worker.Commands.Select(command => command.OperationId));
    }

    private static SuccessfulOperationExecution Success(IReadOnlyList<WorkerMpOutputValue> outputs)
    {
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
        return new(execution, new Api.MpExecutionDetails());
    }

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
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "view_control.get_active_clipping_planes" =>
                [new WorkerRetrievedOutput("Objects", WorkerMpValueKind.CollectionObjectNameList,
                    new WorkerCollectionObjectNameListValue(
                    [new WorkerCollectionObjectNameValue("Parts", "ClipPlane", WorkerObjectTypeValue.Plane)]))],
                "view_control.get_point_of_view_parameters" =>
                [
                    new WorkerRetrievedOutput("Rotation (x)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(10d)),
                    new WorkerRetrievedOutput("Rotation (y)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(20d)),
                    new WorkerRetrievedOutput("Rotation (z)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(30d)),
                    new WorkerRetrievedOutput("Restore Zoom Settings?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("Scale Factor", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2d)),
                    new WorkerRetrievedOutput("Origin (x)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1d)),
                    new WorkerRetrievedOutput("Origin (y)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2d)),
                    new WorkerRetrievedOutput("Restore Render Mode?", WorkerMpValueKind.Logical, new WorkerBooleanValue(false))
                ],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
