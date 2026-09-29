using System.Collections.Concurrent;
using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedVectorGroupRelationshipTuningTests
{
    [Fact]
    public void CreateCommandsUseReviewedDefaultsAndTypedRelationshipNames()
    {
        var relationship = RelationshipName();
        var cylinder = SetVectorGroupToVectorGroupCylindricalZoneOperation.CreateCommand(new()
            { VgToVgRelationship = relationship });
        var gradient = SetVectorGroupToVectorGroupFitGradientFactorOperation.CreateCommand(new()
            { VgToVgRelationship = relationship });
        var weights = SetVectorGroupToVectorGroupFitWeightsOperation.CreateCommand(new()
            { VgToVgRelationship = relationship });
        var polarity = SetVectorGroupToVectorGroupRelativePolarityOperation.CreateCommand(new()
            { VgToVgRelationship = relationship });

        AssertRelationship(cylinder.InputArguments[0]);
        Assert.Equal([1.0, -10.0, 10.0], cylinder.InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));
        AssertRelationship(gradient.InputArguments[0]);
        Assert.Equal(50.0, gradient.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        AssertRelationship(weights.InputArguments[0]);
        Assert.Equal([0.0, 10.0, 0.0, 10.0, 0.0, 1.0], weights.InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));
        AssertRelationship(polarity.InputArguments[0]);
        Assert.True(polarity.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public void CreateCommandsPreserveExplicitZeroAndFalseValues()
    {
        var relationship = RelationshipName();
        var cylinder = SetVectorGroupToVectorGroupCylindricalZoneOperation.CreateCommand(new()
        {
            VgToVgRelationship = relationship,
            RadialOffset = 0,
            MinimumAxialOffset = 0,
            MaximumAxialOffset = 0
        });
        var gradient = SetVectorGroupToVectorGroupFitGradientFactorOperation.CreateCommand(new()
        {
            VgToVgRelationship = relationship,
            FitGradientFactor = 0
        });
        var weights = SetVectorGroupToVectorGroupFitWeightsOperation.CreateCommand(new()
        {
            VgToVgRelationship = relationship,
            MinimumGap = 0,
            MinimumGapFitWeight = 0,
            MaximumGap = 0,
            MaximumGapFitWeight = 0,
            NominalGap = 0,
            NominalGapFitWeight = 0
        });
        var polarity = SetVectorGroupToVectorGroupRelativePolarityOperation.CreateCommand(new()
        {
            VgToVgRelationship = relationship,
            SetOpposingVectorGroupPolarity = false
        });

        Assert.All(cylinder.InputArguments.Skip(1), argument =>
            Assert.Equal(0.0, argument.RequireValue<WorkerDoubleValue>().Value));
        Assert.Equal(0.0, gradient.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.All(weights.InputArguments.Skip(1), argument =>
            Assert.Equal(0.0, argument.RequireValue<WorkerDoubleValue>().Value));
        Assert.False(polarity.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientExecutesAllFourTypedRoutes()
    {
        var worker = new RelationshipTuningWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);
        var relationship = RelationshipName();

        await client.SetVectorGroupToVectorGroupCylindricalZoneAsync(new()
            { VgToVgRelationship = relationship }, deadline: DateTime.UtcNow.AddSeconds(10));
        await client.SetVectorGroupToVectorGroupFitGradientFactorAsync(new()
            { VgToVgRelationship = relationship, FitGradientFactor = 75 }, deadline: DateTime.UtcNow.AddSeconds(10));
        await client.SetVectorGroupToVectorGroupFitWeightsAsync(new()
        {
            VgToVgRelationship = relationship,
            MinimumGap = 0.1,
            MinimumGapFitWeight = 2,
            MaximumGap = 0.3,
            MaximumGapFitWeight = 4,
            NominalGap = 0.5,
            NominalGapFitWeight = 6
        }, deadline: DateTime.UtcNow.AddSeconds(10));
        await client.SetVectorGroupToVectorGroupRelativePolarityAsync(new()
            { VgToVgRelationship = relationship, SetOpposingVectorGroupPolarity = false },
            deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Collection(worker.Commands,
            command => AssertCommand(command,
                "relationship_operations.set_vector_group_to_vector_group_cylindrical_zone", [1.0, -10.0, 10.0]),
            command => AssertCommand(command,
                "relationship_operations.set_vector_group_to_vector_group_fit_gradient_factor", [75.0]),
            command => AssertCommand(command,
                "relationship_operations.set_vector_group_to_vector_group_fit_weights", [0.1, 2.0, 0.3, 4.0, 0.5, 6.0]),
            command =>
            {
                Assert.Equal("relationship_operations.set_vector_group_to_vector_group_relative_polarity", command.OperationId);
                AssertRelationship(command.InputArguments[0]);
                Assert.False(command.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
            });

    }

    private static Api.CollectionItemName RelationshipName() => new()
    {
        CollectionName = "Relations",
        ItemName = "VG relationship"
    };

    private static void AssertRelationship(WorkerMpInputArgument argument)
    {
        Assert.Equal("VG To VG Relationship", argument.Name);
        Assert.Equal("SetCollectionObjectNameArg2", argument.SdkBinding);
        Assert.Equal(new WorkerCollectionItemNameValue("Relations", "VG relationship", WorkerItemTypeValue.Relationship),
            argument.RequireValue<WorkerCollectionItemNameValue>());
    }

    private static void AssertCommand(WorkerMpCommand command, string operationId, double[] values)
    {
        Assert.Equal(operationId, command.OperationId);
        AssertRelationship(command.InputArguments[0]);
        Assert.Equal(values, command.InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));
    }

    private sealed class RelationshipTuningWorker : IWorkerCommandExecutor
    {
        public ConcurrentQueue<WorkerMpCommand> Commands { get; } = new();

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            var requestId = Guid.NewGuid();
            command = RoundTrip(WorkerControlMessage.Execute(requestId, command)).Command!;
            Commands.Enqueue(command);
            var response = RoundTrip(WorkerControlMessage.ExecutionResult(requestId, new(
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
