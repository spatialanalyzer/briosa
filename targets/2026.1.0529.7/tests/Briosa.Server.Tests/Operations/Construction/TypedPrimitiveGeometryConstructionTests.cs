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

public sealed class TypedPrimitiveGeometryConstructionTests
{
    [Fact]
    public async Task GeneratedClientConstructsPrimitiveGeometryWithTypedArgumentsAndDefaults()
    {
        var worker = new PrimitiveGeometryWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var circle = await client.ConstructCircleAsync(new()
        {
            CircleName = Object("Shapes", "Circle"),
            CircleCenter = Vector(1, 2, 3),
            CircleNormal = Vector(0, 0, 1)
        }, options);
        var cone = await client.ConstructConeAsync(new()
        {
            ConeName = Object("Shapes", "Cone"),
            ConeEndPoint = Vector(0, 0, 4),
            ConeAxis = Vector(0, 0, 1),
            ConeLength = 12,
            ConeThetaStart = 5,
            ConeThetaSpan = 120,
            ConeIncludedAngle = 45
        }, options);
        var cylinder = await client.ConstructCylinderAsync(new()
        {
            CylinderName = Object("Shapes", "Cylinder"),
            CylinderEndPoint = Vector(1, 0, 0),
            CylinderAxis = Vector(0, 1, 0),
            CylinderDiameter = 10,
            CylinderLength = 25
        }, options);
        var cylinderFromEnds = await client.ConstructCylinderFromEndPointsAsync(new()
        {
            CylinderName = Object("Shapes", "CylinderFromEnds"),
            CylinderEndPointA = Vector(-1, 0, 0),
            CylinderEndPointB = Vector(1, 0, 0)
        }, options);
        var plane = await client.ConstructPlaneAsync(new()
        {
            PlaneName = Object("Shapes", "Plane"),
            PlaneCenter = Vector(0, 0, 0),
            PlaneNormal = Vector(0, 0, 1),
            PlaneEdgeDimension = 100
        }, options);
        var sphere = await client.ConstructSphereAsync(new()
        {
            SphereName = Object("Shapes", "Sphere"),
            SphereCenter = Vector(3, 2, 1),
            SphereRadius = 5
        }, options);

        Assert.All(new[] { circle.Execution, cone.Execution, cylinder.Execution,
                cylinderFromEnds.Execution, plane.Execution, sphere.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal(6, worker.Commands.Count);

        Assert.Equal(WorkerObjectTypeValue.Circle, ObjectArgument(worker.Commands[0], 0).ObjectType);
        Assert.Equal(new WorkerVectorValue(1, 2, 3), worker.Commands[0].InputArguments[1].RequireValue<WorkerVectorValue>());
        Assert.Equal(new WorkerVectorValue(0, 0, 1), worker.Commands[0].InputArguments[2].RequireValue<WorkerVectorValue>());
        Assert.Equal(0, worker.Commands[0].InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Cone, ObjectArgument(worker.Commands[1], 0).ObjectType);
        Assert.Equal(12, worker.Commands[1].InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(5, worker.Commands[1].InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(120, worker.Commands[1].InputArguments[5].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(45, worker.Commands[1].InputArguments[6].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Cylinder, ObjectArgument(worker.Commands[2], 0).ObjectType);
        Assert.Equal(10, worker.Commands[2].InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(25, worker.Commands[2].InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(new WorkerVectorValue(-1, 0, 0), worker.Commands[3].InputArguments[1].RequireValue<WorkerVectorValue>());
        Assert.Equal(new WorkerVectorValue(1, 0, 0), worker.Commands[3].InputArguments[2].RequireValue<WorkerVectorValue>());
        Assert.Equal(0, worker.Commands[3].InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Plane, ObjectArgument(worker.Commands[4], 0).ObjectType);
        Assert.Equal(100, worker.Commands[4].InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Sphere, ObjectArgument(worker.Commands[5], 0).ObjectType);
        Assert.Equal(new WorkerVectorValue(3, 2, 1), worker.Commands[5].InputArguments[1].RequireValue<WorkerVectorValue>());
        Assert.Equal(5, worker.Commands[5].InputArguments[2].RequireValue<WorkerDoubleValue>().Value);

        var missingCircleNormal = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructCircleAsync(new()
            {
                CircleName = Object("Shapes", "Invalid"),
                CircleCenter = Vector(0, 0, 0)
            }, options));
        var missingConeAxis = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructConeAsync(new()
            {
                ConeName = Object("Shapes", "Invalid"),
                ConeEndPoint = Vector(0, 0, 0)
            }, options));
        var missingCylinderName = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructCylinderAsync(new()
            {
                CylinderEndPoint = Vector(0, 0, 0),
                CylinderAxis = Vector(0, 0, 1)
            }, options));
        var missingEndpoint = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructCylinderFromEndPointsAsync(new()
            {
                CylinderName = Object("Shapes", "Invalid"),
                CylinderEndPointA = Vector(0, 0, 0)
            }, options));
        var missingPlaneNormal = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPlaneAsync(new()
            {
                PlaneName = Object("Shapes", "Invalid"),
                PlaneCenter = Vector(0, 0, 0)
            }, options));
        var missingSphereCenter = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructSphereAsync(new()
            {
                SphereName = Object("Shapes", "Invalid")
            }, options));
        Assert.All(new[] { missingCircleNormal, missingConeAxis, missingCylinderName,
                missingEndpoint, missingPlaneNormal, missingSphereCenter },
            exception => Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode));
        Assert.Equal(6, worker.Commands.Count);
    }

    [Fact]
    public void PrimitiveGeometryOperationsAreUnsafeGlobalMutationsOutsideTheGenericCatalog()
    {
        var operations = new[]
        {
            ConstructCircleOperation.Descriptor,
            ConstructConeOperation.Descriptor,
            ConstructCylinderOperation.Descriptor,
            ConstructCylinderFromEndPointsOperation.Descriptor,
            ConstructPlaneOperation.Descriptor,
            ConstructSphereOperation.Descriptor
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

    private static Api.Vector Vector(double x, double y, double z) => new() { X = x, Y = y, Z = z };

    private static WorkerCollectionObjectNameValue ObjectArgument(WorkerMpCommand command, int index) =>
        command.InputArguments[index].RequireValue<WorkerCollectionObjectNameValue>();

    private sealed class PrimitiveGeometryWorker : IWorkerCommandExecutor
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
