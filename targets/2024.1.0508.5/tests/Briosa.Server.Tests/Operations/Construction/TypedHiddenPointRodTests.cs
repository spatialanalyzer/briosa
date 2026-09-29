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

public sealed class TypedHiddenPointRodTests
{
    [Fact]
    public async Task GeneratedClientUsesTypedHiddenPointRodRoutesAndMapsIndices()
    {
        var worker = new HiddenPointRodWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var createPoint = await client.CreateHiddenPointAsync(new()
        {
            EndAPointName = new() { TargetName = "A" },
            EndBPointName = new() { TargetName = "B" },
            PointNameToCreate = new() { TargetName = "Hidden" }
        }, options);
        var createRod = await client.CreateHiddenPointRodAsync(new(), options);
        var deleteRod = await client.DeleteHiddenPointRodAsync(new(), options);
        var getRodIndex = await client.GetHiddenPointRodIndexByNameAsync(new(), options);

        Assert.All(new[] { createPoint.Execution, createRod.Execution, deleteRod.Execution, getRodIndex.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal(17, createRod.HiddenPointRodIndex);
        Assert.Equal(17, getRodIndex.HiddenPointRodIndex);
        Assert.Equal(4, worker.Commands.Count);
        Assert.Equal(0, worker.Commands[0].InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(worker.Commands[0].InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(string.Empty, worker.Commands[1].InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0d, worker.Commands[1].InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, worker.Commands[1].InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, worker.Commands[1].InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, worker.Commands[2].InputArguments[0].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(string.Empty, worker.Commands[3].InputArguments[0].RequireValue<WorkerTextValue>().Value);

        var missingEndA = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.CreateHiddenPointAsync(new()
            {
                EndBPointName = new() { TargetName = "B" },
                PointNameToCreate = new() { TargetName = "Hidden" }
            }, options));
        Assert.Equal(StatusCode.InvalidArgument, missingEndA.StatusCode);
        Assert.Equal(4, worker.Commands.Count);
    }

    [Fact]
    public void HiddenPointRodOperationsPreserveMetadata()
    {
        var mutationOperations = new[]
        {
            CreateHiddenPointOperation.Descriptor,
            CreateHiddenPointRodOperation.Descriptor,
            DeleteHiddenPointRodOperation.Descriptor
        };
        foreach (var operation in mutationOperations)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
        }
        Assert.Equal(["destructive"], DeleteHiddenPointRodOperation.Descriptor.RiskFlags);
        Assert.Empty(CreateHiddenPointOperation.Descriptor.RiskFlags);
        Assert.Empty(CreateHiddenPointRodOperation.Descriptor.RiskFlags);

        var lookup = GetHiddenPointRodIndexByNameOperation.Descriptor;
        Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == lookup.OperationId);
        Assert.Equal(Api.OperationExecutionScope.GlobalStateRead, lookup.ExecutionScope);
        Assert.Equal(Api.ReplaySafety.Safe, lookup.ReplaySafety);
    }

    private sealed class HiddenPointRodWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId is
                "construction_operations.create_hidden_point_rod" or
                "construction_operations.get_hidden_point_rod_index_by_name"
                ? [new WorkerRetrievedOutput("Hidden Point Rod Index", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(17))]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
