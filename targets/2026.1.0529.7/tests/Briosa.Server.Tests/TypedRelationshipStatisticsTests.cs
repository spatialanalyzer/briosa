using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedRelationshipStatisticsTests
{
    [Fact]
    public async Task GeneratedClientReceivesTypedStatisticsForEachRelationshipFamily()
    {
        var worker = new StatisticsWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);
        var relationship = new Api.CollectionItemName { CollectionName = "collection", ItemName = "relationship" };

        var general = await client.GetGeneralRelationshipStatisticsAsync(
            new() { RelationshipName = relationship }, deadline: DateTime.UtcNow.AddSeconds(10));
        var pointToPoint = await client.GetPointToPointRelationshipStatisticsAsync(
            new() { RelationshipName = relationship }, deadline: DateTime.UtcNow.AddSeconds(10));
        var pointsToObjects = await client.GetPointsToObjectsRelationshipStatisticsAsync(
            new() { RelationshipName = relationship }, deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Equal(0.5, general.AbsoluteMaxDeviation);
        Assert.Equal(1.5, general.Rms);
        Assert.True(general.HasSignedDeviation);
        Assert.Equal(3.5, general.SignedMaxDeviation);
        Assert.Equal(4.5, general.SignedMinDeviation);
        Assert.Equal(0.5, pointToPoint.DeltaX);
        Assert.Equal(3.5, pointToPoint.DeltaMagnitude);
        Assert.Equal("reference-frame", pointToPoint.ReferenceFrame.ObjectName);
        Assert.Equal(Api.ObjectType.Frame, pointToPoint.ReferenceFrame.ObjectType);
        Assert.Equal(0.5, pointsToObjects.AbsoluteMaxDeviation);
        Assert.Equal(1.5, pointsToObjects.MaxDeviation);
        Assert.Equal(2.5, pointsToObjects.MinDeviation);
        Assert.Equal(3.5, pointsToObjects.AvgDeviation);
        Assert.Equal(4.5, pointsToObjects.Rms);
        Assert.Equal(15, pointsToObjects.CandidatePointCount);
        Assert.Equal(3, worker.Commands.Count);
        Assert.Equal(
            ["Absolute Max Deviation", "RMS", "Has Signed Deviation?", "Signed Max Deviation", "Signed Min Deviation"],
            worker.Commands[0].OutputArguments.Select(argument => argument.Name));
        Assert.Equal(
            ["Delta X", "Delta Y", "Delta Z", "Delta Magnitude", "Reference Frame"],
            worker.Commands[1].OutputArguments.Select(argument => argument.Name));
        Assert.Equal(
            [
                "Absolute Max Deviation", "Max Deviation", "Min Deviation", "Avg Deviation", "RMS",
                "# of Candidate Points", "# of Points Sampled", "# of Points Rejected",
                "# of Points Used", "# of Points Out of Tolerance"
            ],
            worker.Commands[2].OutputArguments.Select(argument => argument.Name));
        Assert.All(worker.Commands, command =>
        {
            Assert.Equal(new WorkerCollectionItemNameValue("collection", "relationship", WorkerItemTypeValue.Relationship),
                command.InputArguments[0].Value);
            Assert.Equal("SetCollectionObjectNameArg2", command.InputArguments[0].SdkBinding);
        });

    }

    private sealed class StatisticsWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var outputs = command.OutputArguments.Select((argument, index) =>
                new WorkerRetrievedOutput(argument.Name, argument.Kind, argument.Kind switch
                {
                    WorkerMpValueKind.Logical => new WorkerBooleanValue(true),
                    WorkerMpValueKind.FloatingPoint => new WorkerDoubleValue(index + 0.5),
                    WorkerMpValueKind.WholeNumber => new WorkerIntegerValue(index + 10),
                    WorkerMpValueKind.CollectionObjectName => new WorkerCollectionObjectNameValue(
                        "collection", "reference-frame", WorkerObjectTypeValue.Frame),
                    _ => throw new InvalidOperationException($"Unexpected statistics output kind {argument.Kind}.")
                })).ToArray();
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
