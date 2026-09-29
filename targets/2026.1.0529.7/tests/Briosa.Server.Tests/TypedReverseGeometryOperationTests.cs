using Briosa.Server.Operations;
using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedReverseGeometryOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.reverse_bsplines",
        "analysis_operations.reverse_plane_normals",
        "analysis_operations.reverse_surface_normals"
    ];

    [Fact]
    public void EachReverseOperationHasOneTypedRegistrationAndNoCatalogEntry()
    {
        foreach (var id in MigratedIds)
        {
            var descriptor = Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, descriptor.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, descriptor.ReplaySafety);
            Assert.Empty(descriptor.RiskFlags);
        }
    }

    [Fact]
    public void EachOperationRequiresItsNamedObjectListAndUsesTheReferenceListBinding()
    {
        Assert.Throws<ArgumentException>(() => ReverseBSplinesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => ReversePlaneNormalsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => ReverseSurfaceNormalsOperation.CreateCommand(new()));

        var objectName = new Api.CollectionObjectName { ObjectName = "object" };
        var spline = ReverseBSplinesOperation.CreateCommand(new() { BSplineList = { objectName } });
        var plane = ReversePlaneNormalsOperation.CreateCommand(new() { PlaneList = { objectName } });
        var surface = ReverseSurfaceNormalsOperation.CreateCommand(new() { SurfaceList = { objectName } });

        AssertCommand(spline, "Reverse B-Splines", "B-Spline List");
        AssertCommand(plane, "Reverse Plane Normals", "Plane List");
        AssertCommand(surface, "Reverse Surface Normals", "Surface List");
    }

    [Fact]
    public async Task GeneratedClientRoutesAllThreeReverseOperations()
    {
        var worker = new RecordingWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.AnalysisOperations.AnalysisOperationsClient(channel);
        var deadline = DateTime.UtcNow.AddSeconds(20);
        var spline = await client.ReverseBSplinesAsync(new()
        {
            BSplineList = { new Api.CollectionObjectName { ObjectName = "spline" } }
        }, new CallOptions(deadline: deadline));
        var plane = await client.ReversePlaneNormalsAsync(new()
        {
            PlaneList = { new Api.CollectionObjectName { ObjectName = "plane" } }
        }, new CallOptions(deadline: deadline));
        var surface = await client.ReverseSurfaceNormalsAsync(new()
        {
            SurfaceList = { new Api.CollectionObjectName { ObjectName = "surface" } }
        }, new CallOptions(deadline: deadline));

        Assert.Equal(MigratedIds, worker.Commands.Select(command => command.OperationId));
        Assert.All(new[] { spline.Execution, plane.Execution, surface.Execution },
            execution => Assert.Equal(Api.MpExecutionState.Succeeded, execution.State));
        Assert.Equal("spline", worker.Commands[0].InputArguments[0]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectName);
        Assert.Equal("plane", worker.Commands[1].InputArguments[0]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectName);
        Assert.Equal("surface", worker.Commands[2].InputArguments[0]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectName);
    }

    private static void AssertCommand(WorkerMpCommand command, string step, string inputName)
    {
        Assert.Equal(step, command.StepName);
        var input = Assert.Single(command.InputArguments);
        Assert.Equal(inputName, input.Name);
        Assert.Equal(WorkerMpValueKind.CollectionObjectNameList, input.Kind);
        Assert.Equal("SetCollectionObjectNameRefListArg", input.SdkBinding);
        Assert.Empty(command.OutputArguments);
    }

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
