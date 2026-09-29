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

public sealed class TypedMeshIntersectionConstructionTests
{
    [Fact]
    public async Task GeneratedClientConstructsBSplinesFromPlaneAndMeshIntersection()
    {
        var worker = new MeshIntersectionWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var result = await client.ConstructBSplinesFromIntersectionOfPlaneAndMeshAsync(new()
        {
            ResultingBSplineName = Object("Results", "Intersection"),
            PlaneName = Object("Geometry", "SectionPlane"),
            MeshName = Object("Scans", "Mesh")
        }, options);

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        var command = Assert.Single(worker.Commands);
        Assert.Equal("Construct B-Splines From Intersection of Plane and Mesh", command.StepName);
        Assert.Equal(WorkerObjectTypeValue.BSpline, ObjectArgument(command, 0).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Plane, ObjectArgument(command, 1).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.ScanStripeMesh, ObjectArgument(command, 2).ObjectType);
        Assert.Equal(3, command.InputArguments[3].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(3, command.InputArguments[4].RequireValue<WorkerIntegerValue>().Value);
        Assert.True(command.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        var spline = Assert.Single(result.BSplineList);
        Assert.Equal(new Api.CollectionObjectName
        {
            CollectionName = "Results",
            ObjectName = "IntersectionSpline",
            ObjectType = Api.ObjectType.BSpline
        }, spline);

        var missingMesh = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructBSplinesFromIntersectionOfPlaneAndMeshAsync(new()
            {
                ResultingBSplineName = Object("Results", "Invalid"),
                PlaneName = Object("Geometry", "SectionPlane")
            }, options));
        Assert.Equal(StatusCode.InvalidArgument, missingMesh.StatusCode);
        Assert.Single(worker.Commands);
    }

    [Fact]
    public void MeshIntersectionSplineOperationIsUnsafeGlobalMutationOutsideTheGenericCatalog()
    {
        var operation = ConstructBSplinesFromIntersectionOfPlaneAndMeshOperation.Descriptor;
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

    private static WorkerCollectionObjectNameValue ObjectArgument(WorkerMpCommand command, int index) =>
        command.InputArguments[index].RequireValue<WorkerCollectionObjectNameValue>();

    private sealed class MeshIntersectionWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs =
            [
                new WorkerRetrievedOutput("B-Spline List", WorkerMpValueKind.CollectionObjectNameList,
                    new WorkerCollectionObjectNameListValue([
                        new WorkerCollectionObjectNameValue("Results", "IntersectionSpline", WorkerObjectTypeValue.BSpline)
                    ]))
            ];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
