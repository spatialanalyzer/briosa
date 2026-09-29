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
    public async Task GeneratedClientConstructsEllipsoidAndEllipseWithTypedArgumentsAndDefaults()
    {
        var worker = new AnalyticSolidWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var ellipsoid = await client.ConstructEllipsoidAsync(new()
        {
            EllipseName = Object("Shapes", "Ellipsoid"),
            TransformInWorkingCoordinates = Transform(Enumerable.Range(0, 16).Select(static value => (double)value))
        }, options);
        var ellipse = await client.ConstructEllipseAsync(new()
        {
            EllipseName = Object("Shapes", "Ellipse"),
            CenterCoordinate = Vector(1, 2, 3),
            NormalDirection = Vector(0, 0, 1),
            MajorAxisRadius = 8,
            MinorAxisRadius = 4
        }, options);

        Assert.Equal(Api.MpExecutionState.Succeeded, ellipsoid.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, ellipse.Execution.State);
        Assert.Equal(2, worker.Commands.Count);

        var ellipsoidCommand = worker.Commands[0];
        Assert.Equal("Construct Ellipsoid", ellipsoidCommand.StepName);
        Assert.Equal(WorkerObjectTypeValue.Any, ObjectArgument(ellipsoidCommand, 0).ObjectType);
        Assert.Equal(5, ellipsoidCommand.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(4, ellipsoidCommand.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(3, ellipsoidCommand.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(1, ellipsoidCommand.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(ellipsoidCommand.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(Enumerable.Range(0, 16).Select(static value => (double)value),
            ellipsoidCommand.InputArguments[6].RequireValue<WorkerTransformValue>().Values);
        Assert.Equal(new WorkerRgbColorValue(255, 0, 0),
            ellipsoidCommand.InputArguments[7].RequireValue<WorkerRgbColorValue>());

        var ellipseCommand = worker.Commands[1];
        Assert.Equal("Construct Ellipse", ellipseCommand.StepName);
        Assert.Equal(WorkerObjectTypeValue.Ellipse, ObjectArgument(ellipseCommand, 0).ObjectType);
        Assert.Equal(new WorkerVectorValue(1, 2, 3), ellipseCommand.InputArguments[1].RequireValue<WorkerVectorValue>());
        Assert.Equal(new WorkerVectorValue(0, 0, 1), ellipseCommand.InputArguments[2].RequireValue<WorkerVectorValue>());
        Assert.Equal(8, ellipseCommand.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(4, ellipseCommand.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);

        var missingTransform = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructEllipsoidAsync(new() { EllipseName = Object("Shapes", "Invalid") }, options));
        var missingNormal = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructEllipseAsync(new()
            {
                EllipseName = Object("Shapes", "Invalid"),
                CenterCoordinate = Vector(0, 0, 0)
            }, options));
        Assert.Equal(StatusCode.InvalidArgument, missingTransform.StatusCode);
        Assert.Equal(StatusCode.InvalidArgument, missingNormal.StatusCode);
        Assert.Equal(2, worker.Commands.Count);
    }

    [Fact]
    public void EllipseAndEllipsoidAreTypedUnsafeGlobalMutationsOutsideTheGenericCatalog()
    {
        foreach (var operation in new[] { ConstructEllipseOperation.Descriptor, ConstructEllipsoidOperation.Descriptor })
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
            Assert.Empty(operation.RiskFlags);
        }
    }

    private static Api.CollectionObjectName Object(string collection, string name) => new()
    {
        CollectionName = collection,
        ObjectName = name
    };

    private static Api.Vector Vector(double x, double y, double z) => new() { X = x, Y = y, Z = z };

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
