using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedPipeRelationshipCutStatusTests
{
    [Fact]
    public void CreateCommandUsesReviewedInputAndOutputBindings()
    {
        var request = new Api.GetPipeRelationshipCutStatusRequest
        {
            RelationshipName = new() { ObjectName = "pipe relationship" }
        };

        var command = GetPipeRelationshipCutStatusOperation.CreateCommand(request);

        Assert.Equal("Get Pipe Relationship Cut Status", command.StepName);
        Assert.Equal("SetCollectionObjectNameArg2", command.InputArguments[0].SdkBinding);
        Assert.Equal(new WorkerCollectionObjectNameValue("", "pipe relationship", WorkerObjectTypeValue.Any),
            command.InputArguments[0].Value);
        Assert.Equal(
            ["GetBoolArg", "GetBoolArg", "GetBoolArg", "GetBoolArg"],
            command.OutputArguments.Select(output => output.SdkBinding));
    }

    [Fact]
    public async Task GeneratedClientReceivesAllPipeCutOutputs()
    {
        var worker = new PipeRelationshipWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        var result = await client.GetPipeRelationshipCutStatusAsync(new()
        {
            RelationshipName = new() { ObjectName = "pipe relationship" }
        }, deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(2, result.Execution.MpResultCode);
        Assert.True(result.HasPipe1CutAvailable);
        Assert.True(result.Pipe1CutAvailable);
        Assert.True(result.HasPipe1CutActive);
        Assert.False(result.Pipe1CutActive);
        Assert.True(result.Pipe2CutAvailable);
        Assert.True(result.Pipe2CutActive);
        Assert.Equal(4, result.Execution.OutputRetrievals.Count);

        var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetPipeRelationshipCutStatusAsync(new(), deadline: DateTime.UtcNow.AddSeconds(10)));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
        Assert.Equal(1, worker.Calls);

    }

    private sealed class PipeRelationshipWorker : IWorkerCommandExecutor
    {
        public int Calls { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            command = RoundTrip(WorkerControlMessage.Execute(Guid.NewGuid(), command)).Command!;
            Assert.Equal(GetPipeRelationshipCutStatusOperation.Descriptor.OperationId, command.OperationId);
            Assert.Equal(
                ["GetBoolArg", "GetBoolArg", "GetBoolArg", "GetBoolArg"],
                command.OutputArguments.Select(output => output.SdkBinding));
            var outputs = new WorkerMpOutputValue[]
            {
                new WorkerRetrievedOutput("Pipe 1 - Cut Available?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                new WorkerRetrievedOutput("Pipe 1 - Cut Active?", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
                new WorkerRetrievedOutput("Pipe 2 - Cut Available?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                new WorkerRetrievedOutput("Pipe 2 - Cut Active?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true))
            };
            var response = RoundTrip(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), new(
                WorkerExecutionResponseStatus.Completed,
                new WorkerMpResultAvailable(2, 1, outputs, null),
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
