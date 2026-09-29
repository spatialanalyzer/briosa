using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedRelationshipSigmoidalGapTests
{
    [Fact]
    public void CreateCommandUsesReviewedBindingsInResultOrder()
    {
        var request = new Api.GetRelationshipSigmoidalGapFitConstraintsRequest
        {
            RelationshipName = new() { CollectionName = "collection", ItemName = "relationship" }
        };

        var command = GetRelationshipSigmoidalGapFitConstraintsOperation.CreateCommand(request);

        Assert.Equal("Get Relationship Sigmoidal Gap Fit Constraints", command.StepName);
        Assert.Equal("SetCollectionObjectNameArg2", command.InputArguments[0].SdkBinding);
        Assert.Equal(new WorkerCollectionItemNameValue("collection", "relationship", WorkerItemTypeValue.Relationship),
            command.InputArguments[0].Value);
        Assert.Equal(
            ["Use Sigmoidal Gap Constraints", "Minimum Gap Boundary", "Minimum Gap Weight", "Maximum Gap Boundary",
                "Maximum Gap Weight", "Nominal Gap", "Nominal Gap Weight", "Gradient Steepness Factor"],
            command.OutputArguments.Select(output => output.Name));
        Assert.Equal(
            ["GetBoolArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg"],
            command.OutputArguments.Select(output => output.SdkBinding));
        Assert.Throws<ArgumentException>(() => GetRelationshipSigmoidalGapFitConstraintsOperation.CreateCommand(new()));
    }

    [Fact]
    public async Task GeneratedClientReceivesSigmoidalGapConstraints()
    {
        var worker = new SigmoidalGapWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        var result = await client.GetRelationshipSigmoidalGapFitConstraintsAsync(new()
        {
            RelationshipName = new() { CollectionName = "collection", ItemName = "relationship" }
        }, deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(2, result.Execution.MpResultCode);
        Assert.True(result.Constraints.UseSigmoidalGapConstraints);
        Assert.Equal(-1.25, result.Constraints.MinimumGapBoundary);
        Assert.Equal(0.5, result.Constraints.MinimumGapWeight);
        Assert.Equal(2.5, result.Constraints.MaximumGapBoundary);
        Assert.Equal(0.75, result.Constraints.MaximumGapWeight);
        Assert.Equal(0.25, result.Constraints.NominalGap);
        Assert.Equal(0.9, result.Constraints.NominalGapWeight);
        Assert.Equal(1.1, result.Constraints.GradientSteepnessFactor);
        Assert.Equal(8, result.Execution.OutputRetrievals.Count);

        var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetRelationshipSigmoidalGapFitConstraintsAsync(new(), deadline: DateTime.UtcNow.AddSeconds(10)));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
        Assert.Equal(1, worker.Calls);

    }

    private sealed class SigmoidalGapWorker : IWorkerCommandExecutor
    {
        public int Calls { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            command = RoundTrip(WorkerControlMessage.Execute(Guid.NewGuid(), command)).Command!;
            Assert.Equal(GetRelationshipSigmoidalGapFitConstraintsOperation.Descriptor.OperationId, command.OperationId);
            Assert.Equal(8, command.OutputArguments.Count);
            var outputs = new WorkerMpOutputValue[]
            {
                new WorkerRetrievedOutput("Use Sigmoidal Gap Constraints", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                new WorkerRetrievedOutput("Minimum Gap Boundary", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(-1.25)),
                new WorkerRetrievedOutput("Minimum Gap Weight", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.5)),
                new WorkerRetrievedOutput("Maximum Gap Boundary", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2.5)),
                new WorkerRetrievedOutput("Maximum Gap Weight", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.75)),
                new WorkerRetrievedOutput("Nominal Gap", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.25)),
                new WorkerRetrievedOutput("Nominal Gap Weight", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.9)),
                new WorkerRetrievedOutput("Gradient Steepness Factor", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1.1))
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
