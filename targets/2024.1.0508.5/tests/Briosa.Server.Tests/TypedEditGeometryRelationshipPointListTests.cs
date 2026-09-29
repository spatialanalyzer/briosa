using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedEditGeometryRelationshipPointListTests
{
    [Fact]
    public void CreateCommandMapsDocumentedModesAndDefault()
    {
        var defaultCommand = EditGeometryRelationshipPointListOperation.CreateCommand(Request());
        Assert.Equal("relationship_operations.edit_geometry_relationship_point_list", defaultCommand.OperationId);
        AssertRelationship(defaultCommand.InputArguments[0]);
        Assert.Equal("Point List", defaultCommand.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("SetStringArg", defaultCommand.InputArguments[1].SdkBinding);

        Assert.Equal("Point Graph", Mode(Api.GeometryRelationshipPointEditMode.PointGraph));
        Assert.Equal("Sub-Sampler Settings", Mode(Api.GeometryRelationshipPointEditMode.SubSamplerSettings));
        Assert.Throws<ArgumentOutOfRangeException>(() => Mode(Api.GeometryRelationshipPointEditMode.Unspecified));
    }

    [Fact]
    public async Task GeneratedClientExecutesTypedPointEditMode()
    {
        var worker = new PointListWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        var result = await client.EditGeometryRelationshipPointListAsync(Request(
            Api.GeometryRelationshipPointEditMode.PointGraph),
            deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(2, result.Execution.MpResultCode);
        var command = Assert.Single(worker.Commands);
        Assert.Equal(EditGeometryRelationshipPointListOperation.Descriptor.OperationId, command.OperationId);
        AssertRelationship(command.InputArguments[0]);
        Assert.Equal("Point Graph", command.InputArguments[1].RequireValue<WorkerTextValue>().Value);

    }

    private static Api.EditGeometryRelationshipPointListRequest Request(
        Api.GeometryRelationshipPointEditMode? mode = null)
    {
        var request = new Api.EditGeometryRelationshipPointListRequest
        {
            RelationshipName = new() { CollectionName = "Relations", ItemName = "R1" }
        };
        if (mode.HasValue)
            request.PointEditMode = mode.Value;
        return request;
    }

    private static string Mode(Api.GeometryRelationshipPointEditMode mode) =>
        EditGeometryRelationshipPointListOperation.CreateCommand(Request(mode))
            .InputArguments[1].RequireValue<WorkerTextValue>().Value;

    private static void AssertRelationship(WorkerMpInputArgument argument)
    {
        Assert.Equal("Relationship Name", argument.Name);
        Assert.Equal("SetCollectionObjectNameArg2", argument.SdkBinding);
        Assert.Equal(new WorkerCollectionItemNameValue("Relations", "R1", WorkerItemTypeValue.Relationship),
            argument.RequireValue<WorkerCollectionItemNameValue>());
    }

    private sealed class PointListWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            var requestId = Guid.NewGuid();
            command = RoundTrip(WorkerControlMessage.Execute(requestId, command)).Command!;
            Commands.Add(command);
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
