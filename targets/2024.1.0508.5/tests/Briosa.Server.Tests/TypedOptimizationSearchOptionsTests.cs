using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedOptimizationSearchOptionsTests
{
    [Fact]
    public void CreateCommandUsesReviewedDefaultAndIntegerBinding()
    {
        var command = SetOptimizationSearchOptionsOperation.CreateCommand(new());

        var input = Assert.Single(command.InputArguments);
        Assert.Equal("Max Number of Step Size Reduction", input.Name);
        Assert.Equal("SetIntegerArg", input.SdkBinding);
        Assert.Equal(5, input.RequireValue<WorkerIntegerValue>().Value);
        Assert.Empty(command.OutputArguments);
    }

    [Fact]
    public void CreateCommandPreservesExplicitZero()
    {
        var command = SetOptimizationSearchOptionsOperation.CreateCommand(new()
        {
            MaxNumberOfStepSizeReduction = 0
        });

        Assert.Equal(0, command.InputArguments[0].RequireValue<WorkerIntegerValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientExecutesTypedOptimizationSearchSetting()
    {
        var worker = new OptimizationSearchWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        var result = await client.SetOptimizationSearchOptionsAsync(new()
        {
            MaxNumberOfStepSizeReduction = 7
        }, deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(2, result.Execution.MpResultCode);
        Assert.Equal(1, worker.Calls);

    }

    private sealed class OptimizationSearchWorker : IWorkerCommandExecutor
    {
        public int Calls { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            command = RoundTrip(WorkerControlMessage.Execute(Guid.NewGuid(), command)).Command!;
            Assert.Equal(SetOptimizationSearchOptionsOperation.Descriptor.OperationId, command.OperationId);
            Assert.Equal(7, command.InputArguments[0].RequireValue<WorkerIntegerValue>().Value);
            Assert.Empty(command.OutputArguments);
            var response = RoundTrip(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), new(
                WorkerExecutionResponseStatus.Completed,
                new WorkerMpResultAvailable(2, 1, [], null),
                new(WorkerConnectionState.Disconnected, WorkerExecutionReadinessState.Unverified,
                    null, 0, 1, "test", DateTimeOffset.UnixEpoch), null)));
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, response.ExecutionResponse!.Execution, null, "completed", 1));
        }

        private static WorkerControlMessage RoundTrip(WorkerControlMessage message)
        {
            using var stream = new MemoryStream();
            using var channel = new WorkerControlChannel(stream);
            channel.Send(message);
            stream.Position = 0;
            return channel.Receive();
        }
    }
}
