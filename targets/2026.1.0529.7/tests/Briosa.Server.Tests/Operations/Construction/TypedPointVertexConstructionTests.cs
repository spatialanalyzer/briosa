using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedPointVertexConstructionTests
{
    [Fact]
    public async Task GeneratedClientConstructsPointsFromObjectVertices()
    {
        var worker = new PointVertexWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var result = await client.ConstructPointsOnObjectVerticesAsync(new()
        {
            ObjectNameList =
            {
                new Api.CollectionObjectName { ObjectName = "Object A" },
                new Api.CollectionObjectName { ObjectName = "Object B" }
            },
            ResultantGroupName = new() { ObjectName = "Vertices" }
        }, options);

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        var command = Assert.Single(worker.Commands);
        var objects = command.InputArguments[0].RequireValue<WorkerCollectionObjectNameListValue>().Values;
        Assert.Equal(2, objects.Count);
        Assert.All(objects, value => Assert.Equal(WorkerObjectTypeValue.Any, value.ObjectType));
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            command.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal("SetCollectionObjectNameRefListArg", command.InputArguments[0].SdkBinding);

        var missingObjects = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointsOnObjectVerticesAsync(new()
            {
                ResultantGroupName = new() { ObjectName = "Vertices" }
            }, options));
        Assert.Equal(StatusCode.InvalidArgument, missingObjects.StatusCode);
        Assert.Single(worker.Commands);
    }

    [Fact]
    public void VertexPointOperationIsRegisteredAsUnsafeGlobalMutation()
    {
        var operation = ConstructPointsOnObjectVerticesOperation.Descriptor;
        Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
        Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
        Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
        Assert.Empty(operation.RiskFlags);
    }

    private sealed class PointVertexWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
