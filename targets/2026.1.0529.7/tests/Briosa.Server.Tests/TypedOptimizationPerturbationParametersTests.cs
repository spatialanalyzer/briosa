using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedOptimizationPerturbationParametersTests
{
    [Fact]
    public void CreateCommandPreservesReviewedDefaultsAndSdkArgumentNames()
    {
        var command = SetOptimizationPerturbationParametersOperation.CreateCommand(new());

        Assert.Equal("Set Optimization Perturbation Parameters", command.StepName);
        Assert.Equal(["Length Perturbation", "Angular Perturbation", "Damping "],
            command.InputArguments.Select(input => input.Name));
        Assert.Equal(["SetDoubleArg", "SetDoubleArg", "SetDoubleArg"],
            command.InputArguments.Select(input => input.SdkBinding));
        Assert.Equal(0.0001, command.InputArguments[0].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.0001, command.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(1.0, command.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Empty(command.OutputArguments);
    }

    [Fact]
    public void CreateCommandPreservesExplicitZeroAndSuppliedValues()
    {
        var command = SetOptimizationPerturbationParametersOperation.CreateCommand(new()
        {
            LengthPerturbation = 0,
            AngularPerturbation = 0.75,
            Damping = 2.5
        });

        Assert.Equal(0, command.InputArguments[0].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.75, command.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(2.5, command.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientExecutesTypedOptimizationSetter()
    {
        var worker = new OptimizationParametersWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        var result = await client.SetOptimizationPerturbationParametersAsync(new()
        {
            LengthPerturbation = 0.0002,
            AngularPerturbation = 0.5,
            Damping = 1.25
        }, deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(2, result.Execution.MpResultCode);
        Assert.Empty(result.Execution.OutputRetrievals);
        Assert.Equal(1, worker.Calls);

    }

    private sealed class OptimizationParametersWorker : IWorkerCommandExecutor
    {
        public int Calls { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            command = RoundTrip(WorkerControlMessage.Execute(Guid.NewGuid(), command)).Command!;
            Assert.Equal(SetOptimizationPerturbationParametersOperation.Descriptor.OperationId,
                command.OperationId);
            Assert.Equal(["Length Perturbation", "Angular Perturbation", "Damping "],
                command.InputArguments.Select(input => input.Name));
            Assert.Equal(0.0002, command.InputArguments[0].RequireValue<WorkerDoubleValue>().Value);
            Assert.Equal(0.5, command.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
            Assert.Equal(1.25, command.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
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
