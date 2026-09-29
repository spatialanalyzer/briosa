using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedSetGroupToNominalGroupViewZoomingTests
{
    [Fact]
    public void CreateCommandPreservesDefaultsAndExplicitFalseAndZero()
    {
        var defaults = SetGroupToNominalGroupViewZoomingOperation.CreateCommand(Request());
        Assert.Equal(WorkerItemTypeValue.Relationship,
            defaults.InputArguments[0].RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.Equal("SetCollectionObjectNameArg2", defaults.InputArguments[0].SdkBinding);
        Assert.True(defaults.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(defaults.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(defaults.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(defaults.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0.01, defaults.InputArguments[5].RequireValue<WorkerDoubleValue>().Value);

        var explicitValues = Request();
        explicitValues.UseClosestPoint = false;
        explicitValues.ShowClosestPointWatchWindow = true;
        explicitValues.UseViewZooming = false;
        explicitValues.IgnorePointsBeyondThreshold = false;
        explicitValues.ProximityThreshold = 0;
        var command = SetGroupToNominalGroupViewZoomingOperation.CreateCommand(explicitValues);
        Assert.False(command.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(command.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(command.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(command.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0, command.InputArguments[5].RequireValue<WorkerDoubleValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedSettingsRoute()
    {
        var worker = new ZoomingWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        await client.SetGroupToNominalGroupViewZoomingAsync(Request(),
            deadline: DateTime.UtcNow.AddSeconds(10));

        var sent = Assert.Single(worker.Commands);
        Assert.Equal(SetGroupToNominalGroupViewZoomingOperation.Descriptor.OperationId, sent.OperationId);
        Assert.Equal(0.01, sent.InputArguments[5].RequireValue<WorkerDoubleValue>().Value);

    }

    private static Api.SetGroupToNominalGroupViewZoomingRequest Request() => new()
    {
        RelationshipName = new() { CollectionName = "Relationships", ItemName = "R1" }
    };

    private sealed class ZoomingWorker : IWorkerCommandExecutor
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
