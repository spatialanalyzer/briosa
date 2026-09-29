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

public sealed class TypedPointGroupCorrespondenceTests
{
    [Fact]
    public async Task GeneratedClientMapsPointGroupCorrespondenceAndTargetDefaults()
    {
        var worker = new CorrespondenceWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var distance = await client.ConstructPointsAutoCorrespondTwoGroupsInterPointDistanceAsync(new()
        {
            ReferenceGroup = new() { ObjectName = "Known" },
            GroupToBeCopied = new() { ObjectName = "Unknown" },
            GroupToContainMatchedPoints = new() { ObjectName = "Matched" }
        }, options);
        var proximity = await client.ConstructPointsAutoCorrespondTwoGroupsProximityAsync(new()
        {
            ReferenceGroup = new() { ObjectName = "Known" },
            GroupToBeCopied = new() { ObjectName = "Unknown" },
            SamePointTolerance = 0.5,
            GroupToContainMatchedPoints = new() { ObjectName = "Matched" }
        }, options);

        Assert.All(new[] { distance.Execution, proximity.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal(2, worker.Commands.Count);
        Assert.Equal(0.1, worker.Commands[0].InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.5, worker.Commands[1].InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.All(worker.Commands, command =>
        {
            Assert.All(command.InputArguments.Where(argument => argument.Kind == WorkerMpValueKind.CollectionObjectName),
                argument => Assert.Equal(WorkerObjectTypeValue.PointGroup,
                    argument.RequireValue<WorkerCollectionObjectNameValue>().ObjectType));
            Assert.All(command.InputArguments, argument =>
                Assert.StartsWith("Set", argument.SdkBinding, StringComparison.Ordinal));
        });

        var missingOutputGroup = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointsAutoCorrespondTwoGroupsInterPointDistanceAsync(new()
            {
                ReferenceGroup = new() { ObjectName = "Known" },
                GroupToBeCopied = new() { ObjectName = "Unknown" }
            }, options));
        Assert.Equal(StatusCode.InvalidArgument, missingOutputGroup.StatusCode);
        Assert.Equal(2, worker.Commands.Count);
    }

    [Fact]
    public void CorrespondenceOperationsAreUnsafeGlobalMutations()
    {
        var operations = new[]
        {
            ConstructPointsAutoCorrespondTwoGroupsInterPointDistanceOperation.Descriptor,
            ConstructPointsAutoCorrespondTwoGroupsProximityOperation.Descriptor
        };
        foreach (var operation in operations)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
            Assert.Empty(operation.RiskFlags);
        }
    }

    private sealed class CorrespondenceWorker : IWorkerCommandExecutor
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
