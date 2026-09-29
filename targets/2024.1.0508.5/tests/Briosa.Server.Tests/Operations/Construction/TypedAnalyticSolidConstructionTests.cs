using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedAnalyticSolidConstructionTests
{
    [Fact]
    public async Task GeneratedClientConstructsEllipsoidWithTypedArgumentsAndDefaults()
    {
        var worker = new AnalyticSolidWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var result = await client.ConstructEllipsoidAsync(new()
        {
            EllipseName = Object("Shapes", "Ellipsoid"),
            TransformInWorkingCoordinates = Transform(Enumerable.Range(0, 16).Select(static value => (double)value))
        }, options);

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        var command = Assert.Single(worker.Commands);
        Assert.Equal("Construct Ellipsoid", command.StepName);
        Assert.Equal(WorkerObjectTypeValue.Any, ObjectArgument(command, 0).ObjectType);
        Assert.Equal(5, command.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(4, command.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(3, command.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(1, command.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(command.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(Enumerable.Range(0, 16).Select(static value => (double)value),
            command.InputArguments[6].RequireValue<WorkerTransformValue>().Values);
        Assert.Equal(new WorkerRgbColorValue(255, 0, 0), command.InputArguments[7].RequireValue<WorkerRgbColorValue>());

        var missingTransform = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructEllipsoidAsync(new() { EllipseName = Object("Shapes", "Invalid") }, options));
        Assert.Equal(StatusCode.InvalidArgument, missingTransform.StatusCode);
        Assert.Single(worker.Commands);
    }

    [Fact]
    public void EllipsoidIsTypedUnsafeGlobalMutationOutsideTheGenericCatalog()
    {
        var operation = ConstructEllipsoidOperation.Descriptor;
        Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
        Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
        Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
        Assert.Empty(operation.RiskFlags);
    }

    private static Api.CollectionObjectName Object(string collection, string name) => new()
    {
        CollectionName = collection,
        ObjectName = name
    };

    private static Api.Transform Transform(IEnumerable<double> values)
    {
        var transform = new Api.Transform();
        transform.Values.Add(values);
        return transform;
    }

    private static WorkerCollectionObjectNameValue ObjectArgument(WorkerMpCommand command, int index) =>
        command.InputArguments[index].RequireValue<WorkerCollectionObjectNameValue>();

    private sealed class AnalyticSolidWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
