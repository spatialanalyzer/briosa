using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGdtSimultaneousEvaluationTests
{
    [Fact]
    public async Task GeneratedClientUsesTypedGlobalMutationAndPreservesFalseDefault()
    {
        var worker = new SimultaneousEvaluationWorker();
        var grpcHost = await GrpcTestHost.StartAsync<GdtOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.GdtOperations.GdtOperationsClient(channel);

        var defaultResult = await client.SetGlobalForceSimultaneousEvaluationAsync(new(),
            deadline: DateTime.UtcNow.AddSeconds(20));
        var enabledResult = await client.SetGlobalForceSimultaneousEvaluationAsync(
            new() { GlobalSimultaneousEvaluation = true }, deadline: DateTime.UtcNow.AddSeconds(20));

        Assert.Equal(Api.MpExecutionState.Succeeded, defaultResult.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, enabledResult.Execution.State);
        Assert.Equal([false, true], worker.Commands.Select(command =>
            command.InputArguments.Single().RequireValue<WorkerBooleanValue>().Value));
        Assert.All(worker.Commands, command =>
            Assert.Equal("SetBoolArg", command.InputArguments.Single().SdkBinding));
        Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation,
            SetGlobalForceSimultaneousEvaluationOperation.Descriptor.ExecutionScope);
    }

    [Fact]
    public void TypedRouteIsRegisteredAndRemovedFromTheDynamicCatalog()
    {
        var id = SetGlobalForceSimultaneousEvaluationOperation.Descriptor.OperationId;
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
    }

    private sealed class SimultaneousEvaluationWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1,
                Array.Empty<WorkerRetrievedOutput>(), "completed");
            return Task.FromResult(new WorkerExecutionOutcome(
                WorkerExecutionStatus.Completed, WorkerExecutionDisposition.Completed,
                execution, null, "completed", 1));
        }
    }
}
