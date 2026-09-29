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

public sealed class TypedFrameQueryOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.get_euler_parameters_for_frame",
        "analysis_operations.get_euler_parameters_for_ith_frame_in_frame_set",
        "analysis_operations.get_number_of_frames_in_frame_set",
        "analysis_operations.get_timestamp_for_ith_frame_in_frame_set",
        "analysis_operations.get_transform_for_ith_frame_in_frame_set"
    ];

    [Fact]
    public void EachFrameQueryHasOneTypedRegistrationAndNoCatalogEntry()
    {
        foreach (var id in MigratedIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    [Fact]
    public void RequiredFramesAndFrameSetIndexDefaultsMatchTheBindings()
    {
        Assert.Throws<ArgumentException>(() => GetEulerParametersForFrameOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetEulerParametersForIthFrameInFrameSetOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetNumberOfFramesInFrameSetOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetTimestampForIthFrameInFrameSetOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetTransformForIthFrameInFrameSetOperation.CreateCommand(new()));

        var frameSet = new Api.CollectionObjectName { ObjectName = "frames" };
        WorkerMpCommand[] indexedQueries =
        [
            GetEulerParametersForIthFrameInFrameSetOperation.CreateCommand(new() { FrameSet = frameSet }),
            GetTimestampForIthFrameInFrameSetOperation.CreateCommand(new() { FrameSet = frameSet }),
            GetTransformForIthFrameInFrameSetOperation.CreateCommand(new() { FrameSet = frameSet })
        ];
        foreach (var command in indexedQueries)
        {
            Assert.Equal("SetCollectionObjectNameArg2", command.InputArguments[0].SdkBinding);
            Assert.Equal("SetIntegerArg", command.InputArguments[1].SdkBinding);
            Assert.Equal(0, command.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        }

        var frameCommand = GetEulerParametersForFrameOperation.CreateCommand(new()
        {
            Frame = new Api.CollectionObjectName { ObjectName = "frame" }
        });
        Assert.Equal("SetCollectionObjectNameArg2", frameCommand.InputArguments[0].SdkBinding);
    }

    [Fact]
    public void EulerParametersPreserveTheSevenOutputOrderAndPresence()
    {
        var result = GetEulerParametersForFrameOperation.CreateResult(Completed(
            Enumerable.Range(1, 7).Select(i =>
                (WorkerMpOutputValue)new WorkerRetrievedOutput($"value-{i}", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(i))).ToArray()));

        Assert.Equal(1d, result.X);
        Assert.Equal(2d, result.Y);
        Assert.Equal(3d, result.Z);
        Assert.Equal(4d, result.E1);
        Assert.Equal(5d, result.E2);
        Assert.Equal(6d, result.E3);
        Assert.Equal(7d, result.E4);
        Assert.True(result.HasE4);
    }

    [Fact]
    public void FrameCountTimestampAndTransformResultsMapToTheirTypedValues()
    {
        var count = GetNumberOfFramesInFrameSetOperation.CreateResult(Completed(
            new WorkerRetrievedOutput("Total Count", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(9))));
        var timestamp = GetTimestampForIthFrameInFrameSetOperation.CreateResult(Completed(
            new WorkerRetrievedOutput("Timestamp", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(12.5))));
        var transformValues = Enumerable.Range(0, 16).Select(i => (double)i).ToArray();
        var transform = GetTransformForIthFrameInFrameSetOperation.CreateResult(Completed(
            new WorkerRetrievedOutput("Transform in Working", WorkerMpValueKind.Transform,
                new WorkerTransformValue(transformValues))));

        Assert.Equal(9, count.TotalCount);
        Assert.Equal(12.5, timestamp.Timestamp);
        Assert.Equal(transformValues, transform.TransformInWorking.Values);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedFrameCountRoute()
    {
        var worker = new CountWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var result = await new Api.AnalysisOperations.AnalysisOperationsClient(channel)
            .GetNumberOfFramesInFrameSetAsync(new()
            {
                FrameSetContainer = new Api.CollectionObjectName { ObjectName = "frames" }
            }, new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));

        Assert.Equal(7, result.TotalCount);
        Assert.Equal(MigratedIds[2], Assert.Single(worker.Commands).OperationId);
    }

    private static SuccessfulOperationExecution Completed(params WorkerMpOutputValue[] outputs) => new(
        WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed"),
        new Api.MpExecutionDetails());

    private sealed class CountWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            Assert.Equal("Get Number of Frames In Frame Set", command.StepName);
            WorkerMpOutputValue[] outputs =
            [
                new WorkerRetrievedOutput("Total Count", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(7))
            ];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
