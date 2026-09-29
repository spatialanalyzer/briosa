using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedObjectToObjectDirectionFitConstraintsTests
{
    [Fact]
    public void CreateCommandPreservesInputOrderAndScalarDefaults()
    {
        var command = SetObjectToObjectDirectionRelationshipFitConstraintsOperation.CreateCommand(new()
        {
            RelationshipName = new() { CollectionName = "collection", ObjectName = "relationship" }
        });

        Assert.Equal(
            ["SetCollectionObjectNameArg2", "SetFitConstraintScalarOptionsArg", "SetFitConstraintScalarOptionsArg"],
            command.InputArguments.Select(input => input.SdkBinding));
        Assert.Equal(
            ["Relationship Name", "Angle Between Vectors Fit Constraints", "Mutual Perpendicular Length Fit Constraints"],
            command.InputArguments.Select(input => input.Name));
        Assert.Equal(new WorkerFitConstraintScalarOptionsValue(
                new WorkerToleranceLimit(false, 0), new WorkerToleranceLimit(false, 0)),
            command.InputArguments[1].RequireValue<WorkerFitConstraintScalarOptionsValue>());
        Assert.Equal(new WorkerFitConstraintScalarOptionsValue(
                new WorkerToleranceLimit(false, 0), new WorkerToleranceLimit(false, 0)),
            command.InputArguments[2].RequireValue<WorkerFitConstraintScalarOptionsValue>());
        Assert.Empty(command.OutputArguments);
        Assert.Throws<ArgumentException>(() => SetObjectToObjectDirectionRelationshipFitConstraintsOperation.CreateCommand(new()));
    }

    [Fact]
    public void CreateCommandMapsEachConstraintSetIndependently()
    {
        var request = new Api.SetObjectToObjectDirectionRelationshipFitConstraintsRequest
        {
            RelationshipName = new() { CollectionName = "collection", ObjectName = "relationship" },
            AngleBetweenVectorsFitConstraints = new()
            {
                High = new() { Enabled = true, Value = 1.5 },
                Low = new() { Enabled = false, Value = 0.25 }
            },
            MutualPerpendicularLengthFitConstraints = new()
            {
                High = new() { Enabled = false, Value = 2.5 },
                Low = new() { Enabled = true, Value = 0.75 }
            }
        };

        var command = SetObjectToObjectDirectionRelationshipFitConstraintsOperation.CreateCommand(request);

        Assert.Equal(new WorkerFitConstraintScalarOptionsValue(
                new WorkerToleranceLimit(true, 1.5), new WorkerToleranceLimit(false, 0.25)),
            command.InputArguments[1].RequireValue<WorkerFitConstraintScalarOptionsValue>());
        Assert.Equal(new WorkerFitConstraintScalarOptionsValue(
                new WorkerToleranceLimit(false, 2.5), new WorkerToleranceLimit(true, 0.75)),
            command.InputArguments[2].RequireValue<WorkerFitConstraintScalarOptionsValue>());
    }

    [Fact]
    public async Task GeneratedClientExecutesTypedFitConstraintSetter()
    {
        var worker = new FitConstraintsWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        var result = await client.SetObjectToObjectDirectionRelationshipFitConstraintsAsync(new()
        {
            RelationshipName = new() { CollectionName = "collection", ObjectName = "relationship" },
            AngleBetweenVectorsFitConstraints = new() { High = new() { Enabled = true, Value = 4.5 } },
            MutualPerpendicularLengthFitConstraints = new() { Low = new() { Enabled = true, Value = 3.25 } }
        }, deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(2, result.Execution.MpResultCode);
        Assert.Empty(result.Execution.OutputRetrievals);
        Assert.Equal(1, worker.Calls);

    }

    private sealed class FitConstraintsWorker : IWorkerCommandExecutor
    {
        public int Calls { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            command = RoundTrip(WorkerControlMessage.Execute(Guid.NewGuid(), command)).Command!;
            Assert.Equal(SetObjectToObjectDirectionRelationshipFitConstraintsOperation.Descriptor.OperationId,
                command.OperationId);
            Assert.Equal(3, command.InputArguments.Count);
            Assert.Equal(new WorkerFitConstraintScalarOptionsValue(
                    new WorkerToleranceLimit(true, 4.5), new WorkerToleranceLimit(false, 0)),
                command.InputArguments[1].RequireValue<WorkerFitConstraintScalarOptionsValue>());
            Assert.Equal(new WorkerFitConstraintScalarOptionsValue(
                    new WorkerToleranceLimit(false, 0), new WorkerToleranceLimit(true, 3.25)),
                command.InputArguments[2].RequireValue<WorkerFitConstraintScalarOptionsValue>());
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
