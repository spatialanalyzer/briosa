using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedPipeRelationshipMutationTests
{
    [Fact]
    public void SettersPreserveOptionalDefaultsAndSdkArgumentOrder()
    {
        var relation = new Api.CollectionObjectName { ObjectName = "pipe relationship" };
        var segments = SetPipeRelationshipSegmentPropertiesOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            Pipe1InnerDiameter = 4.25
        });
        Assert.Equal(9, segments.InputArguments.Count);
        Assert.Equal("SetDoubleArg", segments.InputArguments[1].SdkBinding);
        Assert.Equal(new WorkerDoubleValue(4.25), segments.InputArguments[1].Value);
        Assert.Equal(new WorkerDoubleValue(0), segments.InputArguments[2].Value);
        Assert.Equal(new WorkerDoubleValue(0), segments.InputArguments[8].Value);

        var weights = SetPipeRelationshipWeightsOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            CenterPull = 0,
            ConstrainRegionAtOd = true
        });
        Assert.Equal(new WorkerDoubleValue(1), weights.InputArguments[1].Value);
        Assert.Equal(new WorkerDoubleValue(2), weights.InputArguments[2].Value);
        Assert.Equal(new WorkerDoubleValue(1), weights.InputArguments[3].Value);
        Assert.Equal(new WorkerDoubleValue(0), weights.InputArguments[4].Value);
        Assert.Equal(new WorkerDoubleValue(10), weights.InputArguments[5].Value);
        Assert.Equal(new WorkerDoubleValue(1), weights.InputArguments[6].Value);
        Assert.Equal(new WorkerBooleanValue(true), weights.InputArguments[7].Value);
        Assert.Equal(new WorkerBooleanValue(false), weights.InputArguments[8].Value);

        var pipe1Frame = new Api.CollectionObjectName { ObjectName = "pipe one frame" };
        var pipe2Frame = new Api.CollectionObjectName { ObjectName = "pipe two frame" };
        var makeCut = MakePipeRelationshipCutOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            Pipe1FrameName = pipe1Frame,
            Pipe2FrameName = pipe2Frame
        });
        Assert.Equal("Make Pipe Relationship Cut", makeCut.StepName);
        Assert.Equal(7, makeCut.InputArguments.Count);
        Assert.Equal(new WorkerBooleanValue(true), makeCut.InputArguments[1].Value);
        Assert.Equal(new WorkerBooleanValue(true), makeCut.InputArguments[2].Value);
        Assert.Equal(new WorkerBooleanValue(false), makeCut.InputArguments[3].Value);
        Assert.Equal(new WorkerBooleanValue(false), makeCut.InputArguments[4].Value);
        Assert.Equal("pipe one frame", makeCut.InputArguments[5].RequireValue<WorkerCollectionObjectNameValue>().ObjectName);
        Assert.Equal("pipe two frame", makeCut.InputArguments[6].RequireValue<WorkerCollectionObjectNameValue>().ObjectName);

        var forceCut = PipeRelationshipForceCutToFrameOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            Pipe1FrameName = pipe1Frame,
            Pipe2FrameName = pipe2Frame
        });
        Assert.Equal(5, forceCut.InputArguments.Count);
        Assert.Equal(new WorkerBooleanValue(true), forceCut.InputArguments[1].Value);
        Assert.Equal("pipe one frame", forceCut.InputArguments[2].RequireValue<WorkerCollectionObjectNameValue>().ObjectName);
        Assert.Equal(new WorkerBooleanValue(true), forceCut.InputArguments[3].Value);
        Assert.Equal("pipe two frame", forceCut.InputArguments[4].RequireValue<WorkerCollectionObjectNameValue>().ObjectName);
    }

    [Fact]
    public async Task GeneratedClientRoutesPipeCreationMutationsAndWeightQueries()
    {
        var worker = new PipeRelationshipWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);
        var relation = new Api.CollectionObjectName { ObjectName = "pipe relationship" };
        var deadline = DateTime.UtcNow.AddSeconds(10);

        var create = await client.MakePipeFittingRelationshipAsync(new()
        {
            RelationshipName = relation,
            Pipe1ObjectName = new() { ObjectName = "pipe one" },
            Pipe2ObjectName = new() { ObjectName = "pipe two" }
        }, deadline: deadline);
        var segments = await client.SetPipeRelationshipSegmentPropertiesAsync(new()
        {
            RelationshipName = relation,
            Pipe1InnerDiameter = 4.25,
            Pipe2OuterDiameter = 9.5
        }, deadline: deadline);
        var weightsSet = await client.SetPipeRelationshipWeightsAsync(new()
        {
            RelationshipName = relation,
            OverallWeight = 3.5,
            ConstrainRegionAtOd = true
        }, deadline: deadline);
        var weights = await client.GetPipeRelationshipWeightsAsync(new()
        {
            RelationshipName = relation
        }, deadline: deadline);
        var properties = await client.GetPipeRelationshipPropertiesAsync(new()
        {
            RelationshipName = relation
        }, deadline: deadline);
        var makeCut = await client.MakePipeRelationshipCutAsync(new()
        {
            RelationshipName = relation,
            Pipe1FrameName = new() { ObjectName = "pipe one frame" },
            Pipe2FrameName = new() { ObjectName = "pipe two frame" }
        }, deadline: deadline);
        var forceCut = await client.PipeRelationshipForceCutToFrameAsync(new()
        {
            RelationshipName = relation,
            Pipe1FrameName = new() { ObjectName = "pipe one frame" },
            Pipe2FrameName = new() { ObjectName = "pipe two frame" }
        }, deadline: deadline);

        Assert.Equal(Api.MpExecutionState.Succeeded, create.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, segments.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, weightsSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, properties.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, makeCut.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, forceCut.Execution.State);
        Assert.Equal(7, worker.Commands.Count);
        Assert.Equal(3, worker.Commands[0].InputArguments.Count);
        Assert.Equal(new WorkerDoubleValue(4.25), worker.Commands[1].InputArguments[1].Value);
        Assert.Equal(new WorkerDoubleValue(9.5), worker.Commands[1].InputArguments[6].Value);
        Assert.Equal(new WorkerDoubleValue(3.5), worker.Commands[2].InputArguments[1].Value);
        Assert.Equal(new WorkerBooleanValue(true), worker.Commands[2].InputArguments[7].Value);
        Assert.Equal(5.5, weights.OverallWeight);
        Assert.Equal(-1.25, weights.OutOfMaterialStaticOffset);
        Assert.False(weights.ConstrainRegionAtOd);
        Assert.True(weights.ConstrainIdOdOverlap);
        Assert.Equal(8, weights.Execution.OutputRetrievals.Count);
        Assert.Equal("pipe one", properties.Pipe1ObjectName.ObjectName);
        Assert.Equal("collection", properties.Pipe2ObjectName.CollectionName);
        Assert.Equal(4.5, properties.Pipe1InnerDiameter);
        Assert.Equal(8.5, properties.Pipe2OuterDiameter);
        Assert.Equal(10, properties.Execution.OutputRetrievals.Count);
        Assert.Equal("SetBoolArg", worker.Commands[5].InputArguments[1].SdkBinding);
        Assert.Equal("Pipe 2 - Make Cut", worker.Commands[5].InputArguments[2].Name);
        Assert.Equal("Pipe 1 - Frame Name", worker.Commands[5].InputArguments[5].Name);
        Assert.Equal("Pipe 1 - Force Cut to Frame?", worker.Commands[6].InputArguments[1].Name);
        Assert.Equal("Pipe 2 - Frame Name", worker.Commands[6].InputArguments[4].Name);

    }

    private sealed class PipeRelationshipWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            command = RoundTrip(WorkerControlMessage.Execute(Guid.NewGuid(), command)).Command!;
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                var id when id == GetPipeRelationshipWeightsOperation.Descriptor.OperationId =>
                [
                    new WorkerRetrievedOutput("Overall Weight", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(5.5)),
                    new WorkerRetrievedOutput("Axis Offset", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2)),
                    new WorkerRetrievedOutput("Axis Alignment", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1)),
                    new WorkerRetrievedOutput("Center Pull", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.1)),
                    new WorkerRetrievedOutput("Out of material - Weight", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(10)),
                    new WorkerRetrievedOutput("Out of material - Static Offset", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(-1.25)),
                    new WorkerRetrievedOutput("Constrain Region at OD", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
                    new WorkerRetrievedOutput("Constrain ID/OD overlap", WorkerMpValueKind.Logical, new WorkerBooleanValue(true))
                ],
                var id when id == GetPipeRelationshipPropertiesOperation.Descriptor.OperationId =>
                [
                    new WorkerRetrievedOutput("Pipe 1 - Object Name", WorkerMpValueKind.CollectionObjectName,
                        new WorkerCollectionObjectNameValue("collection", "pipe one", WorkerObjectTypeValue.Cloud)),
                    new WorkerRetrievedOutput("Pipe 1 - Inner Diameter", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(4.5)),
                    new WorkerRetrievedOutput("Pipe 1 - Outer Diameter", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(6.5)),
                    new WorkerRetrievedOutput("Pipe 1 - Cut Begin", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1.5)),
                    new WorkerRetrievedOutput("Pipe 1 - Cut End", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2.5)),
                    new WorkerRetrievedOutput("Pipe 2 - Object Name", WorkerMpValueKind.CollectionObjectName,
                        new WorkerCollectionObjectNameValue("collection", "pipe two", WorkerObjectTypeValue.Cloud)),
                    new WorkerRetrievedOutput("Pipe 2 - Inner Diameter", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(7.5)),
                    new WorkerRetrievedOutput("Pipe 2 - Outer Diameter", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(8.5)),
                    new WorkerRetrievedOutput("Pipe 2 - Cut Begin", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(3.5)),
                    new WorkerRetrievedOutput("Pipe 2 - Cut End", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(4.5))
                ],
                _ => []
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
