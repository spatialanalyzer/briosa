using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Security;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedObjectEditAndScaleBarTests
{
    [Fact]
    public async Task GeneratedClientRoutesEachEditWithExactMpInputs()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var lifetime = host.ConfigureAwait(true);
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(host.Channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        await client.CopyObjectAsync(new() { SourceObject = Object("old"), NewObjectName = Object("new") }, options);
        await client.RenameObjectAsync(new() { OriginalObjectName = Object("old"), NewObjectName = Object("new"), OverwriteIfExists = true }, options);
        await client.RenameItemAsync(new() { OriginalItemName = Item("old"), NewItemName = Item("new") }, options);
        await client.MirrorObjectsAsync(new() { Objects = { Object("part") }, FrameName = Object("frame"), FramePlaneToMirrorAround = Api.MirrorFramePlane.Xz }, options);
        await client.ConstructScaleBarAsync(new() { ScaleBarName = Item("bar"), BeginTarget = Point("start"), EndTarget = Point("end") }, options);

        Assert.Equal(5, worker.Commands.Count);
        Assert.Equal(["Source Object", "New Object Name", "Overwrite if exists?"], worker.Commands[0].InputArguments.Select(a => a.Name));
        Assert.Equal("SetCollectionObjectNameArg2", worker.Commands[0].InputArguments[0].SdkBinding);
        Assert.False(worker.Commands[0].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(worker.Commands[1].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerMpValueKind.CollectionItemName, worker.Commands[2].InputArguments[0].Kind);
        Assert.Equal("SetCollectionObjectNameArg2", worker.Commands[2].InputArguments[1].SdkBinding);
        Assert.Equal("XZ", worker.Commands[3].InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.True(worker.Commands[3].InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Frame, worker.Commands[3].InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerItemTypeValue.ScaleBar, worker.Commands[4].InputArguments[0].RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.Equal(["Scale Bar Name", "Begin Target", "End Target", "Length", "Uncertainty", "Use Relative Tolerances?", "Use High Tolerances?", "Use Low Tolerances?", "High Tolerance", "Low Tolerance"], worker.Commands[4].InputArguments.Select(a => a.Name));
        Assert.True(worker.Commands[4].InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(worker.Commands[4].InputArguments[6].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0, worker.Commands[4].InputArguments[9].RequireValue<WorkerDoubleValue>().Value);
        foreach (var id in new[] { "copy_object", "rename_object", "rename_item", "mirror_objects", "construct_scale_bar" })
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == $"construction_operations.{id}");
        }
    }

    [Fact]
    public async Task InvalidRequiredInputsFailBeforeWorkerExecution()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var lifetime = host.ConfigureAwait(true);
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(host.Channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        async Task Invalid(Func<Task> call) => Assert.Equal(StatusCode.InvalidArgument, (await Assert.ThrowsAsync<RpcException>(call).ConfigureAwait(true)).StatusCode);
        await Invalid(async () => await client.CopyObjectAsync(new(), options));
        await Invalid(async () => await client.RenameObjectAsync(new(), options));
        await Invalid(async () => await client.RenameItemAsync(new(), options));
        await Invalid(async () => await client.MirrorObjectsAsync(new() { Objects = { Object("part") }, FrameName = Object("frame") }, options));
        await Invalid(async () => await client.ConstructScaleBarAsync(new() { ScaleBarName = Item("bar") }, options));
        Assert.Empty(worker.Commands);
    }

    private static Api.CollectionObjectName Object(string name) => new() { CollectionName = "part", ObjectName = name };
    private static Api.CollectionItemName Item(string name) => new() { CollectionName = "part", ItemName = name };
    private static Api.PointName Point(string name) => new() { CollectionName = "part", GroupName = "points", TargetName = name };

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];
        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var result = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, result, null, "completed", 1));
        }
    }
}
