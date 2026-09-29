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

public sealed class TypedObjectTransformOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.transform_objects_frame_to_frame",
        "analysis_operations.transform_objects_by_delta_about_working_frame",
        "analysis_operations.transform_objects_by_delta_world_transform_operator",
        "analysis_operations.translate_objects_by_delta"
    ];

    [Fact]
    public void EachObjectTransformHasOneTypedRegistrationAndNoCatalogEntry()
    {
        foreach (var id in MigratedIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    [Fact]
    public void FrameToFrameTransformMapsRequiredNamesAndDefaultStepCount()
    {
        var objectList = new Api.CollectionObjectName
        {
            CollectionName = "parts",
            ObjectName = "part",
            ObjectType = Api.ObjectType.PointGroup
        };
        var request = new Api.TransformObjectsFrameToFrameRequest
        {
            InitialFrameName = new Api.CollectionObjectName { ObjectName = "from" },
            DestinationFrameName = new Api.CollectionObjectName { ObjectName = "to" }
        };
        request.ObjectNameList.Add(objectList);

        var command = TransformObjectsFrameToFrameOperation.CreateCommand(request);

        Assert.Equal("SetCollectionObjectNameRefListArg", command.InputArguments[0].SdkBinding);
        Assert.Equal("SetCollectionObjectNameArg2", command.InputArguments[1].SdkBinding);
        Assert.Equal("SetCollectionObjectNameArg2", command.InputArguments[2].SdkBinding);
        Assert.Equal("SetIntegerArg", command.InputArguments[3].SdkBinding);
        Assert.Equal(0, command.InputArguments[3].RequireValue<WorkerIntegerValue>().Value);
        Assert.Single(command.InputArguments[0].RequireValue<WorkerCollectionObjectNameListValue>().Values);
        Assert.Throws<ArgumentException>(() => TransformObjectsFrameToFrameOperation.CreateCommand(new()));
    }

    [Fact]
    public void DeltaTransformsValidateInputsAndPreserveWorldScaleDefault()
    {
        var name = new Api.CollectionObjectName { ObjectName = "part" };
        var transform = new Api.Transform();
        transform.Values.AddRange(Enumerable.Range(0, 16).Select(i => (double)i));
        var workingFrameCommand = TransformObjectsByDeltaAboutWorkingFrameOperation.CreateCommand(new()
        {
            DeltaTransform = transform,
            ObjectsToTransform = { name }
        });
        var worldCommand = TransformObjectsByDeltaWorldTransformOperatorOperation.CreateCommand(new()
        {
            DeltaTransform = new Api.WorldTransform { Transform = transform },
            ObjectsToTransform = { name }
        });

        Assert.Equal("SetCollectionObjectNameRefListArg", workingFrameCommand.InputArguments[0].SdkBinding);
        Assert.Equal("SetTransformArg", workingFrameCommand.InputArguments[1].SdkBinding);
        Assert.Equal("SetWorldTransformArg", worldCommand.InputArguments[1].SdkBinding);
        var world = worldCommand.InputArguments[1].RequireValue<WorkerWorldTransformValue>();
        Assert.Equal(16, world.Transform.Values.Count);
        Assert.Equal(1d, world.ScaleFactor);
        Assert.Throws<ArgumentException>(() => TransformObjectsByDeltaAboutWorkingFrameOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => TransformObjectsByDeltaWorldTransformOperatorOperation.CreateCommand(new()));
    }

    [Fact]
    public void TranslationMapsVectorAndUsesRequiredObjectList()
    {
        var command = TranslateObjectsByDeltaOperation.CreateCommand(new()
        {
            DeltaTranslation = new Api.Vector { X = 1, Y = 2, Z = 3 },
            ObjectsToTranslate = { new Api.CollectionObjectName { ObjectName = "part" } }
        });

        Assert.Equal("SetCollectionObjectNameRefListArg", command.InputArguments[0].SdkBinding);
        Assert.Equal("SetVectorArg", command.InputArguments[1].SdkBinding);
        Assert.Equal(3, command.InputArguments[1].RequireValue<WorkerVectorValue>().Z);
        Assert.Throws<ArgumentException>(() => TranslateObjectsByDeltaOperation.CreateCommand(new()));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedFrameToFrameRoute()
    {
        var worker = new TransformWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var request = new Api.TransformObjectsFrameToFrameRequest
        {
            InitialFrameName = new Api.CollectionObjectName { ObjectName = "from" },
            DestinationFrameName = new Api.CollectionObjectName { ObjectName = "to" }
        };
        request.ObjectNameList.Add(new Api.CollectionObjectName { ObjectName = "part" });
        var result = await new Api.AnalysisOperations.AnalysisOperationsClient(channel)
            .TransformObjectsFrameToFrameAsync(request,
                new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(MigratedIds[0], Assert.Single(worker.Commands).OperationId);
    }

    private sealed class TransformWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            Assert.Equal("Transform Objects - Frame To Frame", command.StepName);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
