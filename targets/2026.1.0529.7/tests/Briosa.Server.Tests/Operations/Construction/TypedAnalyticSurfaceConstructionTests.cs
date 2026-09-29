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

public sealed class TypedAnalyticSurfaceConstructionTests
{
    [Fact]
    public async Task GeneratedClientConstructsAnalyticSurfacesFromTypedPrimitiveObjects()
    {
        var worker = new AnalyticSurfaceWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var fromCone = await client.ConstructSurfaceFromConeAsync(new()
        {
            ResultingSurfaceName = Object("Surfaces", "ConeSkin"),
            ConeName = Object("Shapes", "Cone")
        }, options);
        var fromCylinder = await client.ConstructSurfaceFromCylinderAsync(new()
        {
            ResultingSurfaceName = Object("Surfaces", "CylinderSkin"),
            CylinderName = Object("Shapes", "Cylinder")
        }, options);
        var fromPlane = await client.ConstructSurfaceFromPlaneAsync(new()
        {
            ResultingSurfaceName = Object("Surfaces", "PlaneSkin"),
            PlaneName = Object("Shapes", "Plane")
        }, options);
        var fromSphere = await client.ConstructSurfaceFromSphereAsync(new()
        {
            ResultingSurfaceName = Object("Surfaces", "SphereSkin"),
            SphereName = Object("Shapes", "Sphere")
        }, options);

        Assert.All(new[] { fromCone.Execution, fromCylinder.Execution, fromPlane.Execution, fromSphere.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal(4, worker.Commands.Count);
        Assert.Equal(WorkerObjectTypeValue.Surface, ObjectArgument(worker.Commands[0], 0).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Cone, ObjectArgument(worker.Commands[0], 1).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Surface, ObjectArgument(worker.Commands[1], 0).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Cylinder, ObjectArgument(worker.Commands[1], 1).ObjectType);
        Assert.True(worker.Commands[1].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(worker.Commands[1].InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Surface, ObjectArgument(worker.Commands[2], 0).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Plane, ObjectArgument(worker.Commands[2], 1).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Sphere, ObjectArgument(worker.Commands[3], 1).ObjectType);

        var missingCone = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructSurfaceFromConeAsync(new()
            {
                ResultingSurfaceName = Object("Surfaces", "Invalid")
            }, options));
        var missingCylinder = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructSurfaceFromCylinderAsync(new()
            {
                CylinderName = Object("Shapes", "Cylinder")
            }, options));
        var missingPlane = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructSurfaceFromPlaneAsync(new()
            {
                ResultingSurfaceName = Object("Surfaces", "Invalid")
            }, options));
        var missingSphere = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructSurfaceFromSphereAsync(new()
            {
                ResultingSurfaceName = Object("Surfaces", "Invalid")
            }, options));
        Assert.All(new[] { missingCone, missingCylinder, missingPlane, missingSphere },
            exception => Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode));
        Assert.Equal(4, worker.Commands.Count);
    }

    [Fact]
    public void AnalyticSurfaceOperationsAreUnsafeGlobalMutationsOutsideTheGenericCatalog()
    {
        var operations = new[]
        {
            ConstructSurfaceFromConeOperation.Descriptor,
            ConstructSurfaceFromCylinderOperation.Descriptor,
            ConstructSurfaceFromPlaneOperation.Descriptor,
            ConstructSurfaceFromSphereOperation.Descriptor
        };
        foreach (var operation in operations)
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

    private static WorkerCollectionObjectNameValue ObjectArgument(WorkerMpCommand command, int index) =>
        command.InputArguments[index].RequireValue<WorkerCollectionObjectNameValue>();

    private sealed class AnalyticSurfaceWorker : IWorkerCommandExecutor
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
