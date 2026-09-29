using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedHiddenPointBarDatabaseTests
{
    [Fact]
    public async Task GeneratedClientClearsHiddenPointBarDatabaseThroughTypedRoute()
    {
        var worker = new HiddenPointBarWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);

        var result = await client.ClearHiddenPointBarDatabaseAsync(new());

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        var command = Assert.Single(worker.Commands);
        Assert.Empty(command.InputArguments);
        Assert.Empty(command.OutputArguments);
    }

    [Fact]
    public void ClearDatabaseIsAnUnsafeGlobalMutation()
    {
        var operation = ClearHiddenPointBarDatabaseOperation.Descriptor;
        Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
        Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
        Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
        Assert.Empty(operation.RiskFlags);
    }

    private sealed class HiddenPointBarWorker : IWorkerCommandExecutor
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
