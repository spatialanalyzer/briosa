using Briosa.Server.Operations;
using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedPointAndFrameSetterOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.set_point_properties",
        "analysis_operations.set_transform_for_ith_frame_in_frame_set"
    ];

    [Fact]
    public void TypedSettersPreserveTheRequiredValuesAndDefaults()
    {
        foreach (var id in MigratedIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }

        var point = SetPointPropertiesOperation.CreateCommand(new()
        {
            PointNameList = { Point("point") },
            PositionTolerance = new()
            {
                HighX = new() { Enabled = true, Value = 0.25 },
                LowMagnitude = new() { Enabled = true, Value = -0.5 }
            },
            ComponentWeights = Vector(1, 2, 3)
        });
        var transformValues = Enumerable.Range(0, 16).Select(index => (double)index).ToArray();
        var frame = SetTransformForIthFrameInFrameSetOperation.CreateCommand(new()
        {
            FrameSet = Object("frames"),
            TransformInWorking = new() { Values = { transformValues } }
        });

        AssertInputs(point, "Set Point Properties",
            ("Point Name List", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg"),
            ("Planar Offset", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Radial Offset", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Position Tolerance", WorkerMpValueKind.ToleranceVectorOptions, "SetToleranceVectorOptionsArg"),
            ("Component Weights", WorkerMpValueKind.Vector, "SetVectorArg"));
        AssertInputs(frame, "Set Transform for i-th Frame in Frame Set",
            ("Frame Set", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Frame Set Index", WorkerMpValueKind.WholeNumber, "SetIntegerArg"),
            ("Transform in Working", WorkerMpValueKind.Transform, "SetTransformArg"));

        Assert.Equal(0d, point.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, point.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        var tolerance = point.InputArguments[3].RequireValue<WorkerToleranceVectorOptionsValue>();
        Assert.Equal(new WorkerToleranceLimit(true, 0.25), tolerance.HighX);
        Assert.Equal(new WorkerToleranceLimit(false, 0), tolerance.HighY);
        Assert.Equal(new WorkerToleranceLimit(true, -0.5), tolerance.LowMagnitude);
        Assert.Equal(new WorkerVectorValue(1, 2, 3), point.InputArguments[4].RequireValue<WorkerVectorValue>());
        Assert.Equal(0, frame.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(transformValues, frame.InputArguments[2].RequireValue<WorkerTransformValue>().Values);

        Assert.Throws<ArgumentException>(() => SetPointPropertiesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetTransformForIthFrameInFrameSetOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetTransformForIthFrameInFrameSetOperation.CreateCommand(new()
        {
            FrameSet = Object("frames"), TransformInWorking = new() { Values = { 1d, 2d } }
        }));
        Assert.Empty(point.OutputArguments);
        Assert.Empty(frame.OutputArguments);
    }

    [Fact]
    public async Task GeneratedClientRoutesBothSetters()
    {
        var worker = new RecordingWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.AnalysisOperations.AnalysisOperationsClient(channel);
        var deadline = DateTime.UtcNow.AddSeconds(20);
        var point = await client.SetPointPropertiesAsync(new()
        {
            PointNameList = { Point("point") },
            PositionTolerance = new(),
            ComponentWeights = Vector(1, 2, 3)
        }, new CallOptions(deadline: deadline));
        var frame = await client.SetTransformForIthFrameInFrameSetAsync(new()
        {
            FrameSet = Object("frames"), TransformInWorking = new() { Values = { Enumerable.Range(0, 16).Select(i => (double)i) } }
        }, new CallOptions(deadline: deadline));

        Assert.Equal(MigratedIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(Api.MpExecutionState.Succeeded, point.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, frame.Execution.State);
    }

    private static Api.CollectionObjectName Object(string name) => new() { ObjectName = name };

    private static Api.PointName Point(string name) => new() { TargetName = name };

    private static Api.Vector Vector(double x, double y, double z) => new() { X = x, Y = y, Z = z };

    private static void AssertInputs(WorkerMpCommand command, string step,
        params (string Name, WorkerMpValueKind Kind, string? Binding)[] expected)
    {
        Assert.Equal(step, command.StepName);
        Assert.Equal(expected,
            command.InputArguments.Select(argument => (argument.Name, argument.Kind, argument.SdkBinding)).ToArray());
    }

    private sealed class RecordingWorker : IWorkerCommandExecutor
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
