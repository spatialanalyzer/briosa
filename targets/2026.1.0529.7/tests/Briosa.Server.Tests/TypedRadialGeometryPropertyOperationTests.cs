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

public sealed class TypedRadialGeometryPropertyOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.get_circle_properties",
        "analysis_operations.get_ellipse_properties",
        "analysis_operations.get_sphere_properties",
        "analysis_operations.get_torus_properties"
    ];

    [Fact]
    public void EachGeometryQueryHasOneTypedRegistrationAndNoCatalogEntry()
    {
        foreach (var id in MigratedIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    [Fact]
    public void GeometryQueriesRequireTheirObjectAndRetainSdkBindings()
    {
        Assert.Throws<ArgumentException>(() => GetCirclePropertiesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetEllipsePropertiesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetSpherePropertiesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetTorusPropertiesOperation.CreateCommand(new()));

        var circle = GetCirclePropertiesOperation.CreateCommand(new()
            { CircleName = new Api.CollectionObjectName { ObjectName = "circle" } });
        var ellipse = GetEllipsePropertiesOperation.CreateCommand(new()
            { EllipseName = new Api.CollectionObjectName { ObjectName = "ellipse" } });
        var sphere = GetSpherePropertiesOperation.CreateCommand(new()
            { SphereName = new Api.CollectionObjectName { ObjectName = "sphere" } });
        var torus = GetTorusPropertiesOperation.CreateCommand(new()
            { TorusName = new Api.CollectionObjectName { ObjectName = "torus" } });
        foreach (var command in new[] { circle, ellipse, sphere, torus })
            Assert.Equal("SetCollectionObjectNameArg2", Assert.Single(command.InputArguments).SdkBinding);
        Assert.Equal(["GetVectorArg", "GetVectorArg", "GetDoubleArg", "GetDoubleArg"],
            circle.OutputArguments.Select(item => item.SdkBinding));
        Assert.Equal(["GetVectorArg", "GetDoubleArg", "GetDoubleArg"],
            sphere.OutputArguments.Select(item => item.SdkBinding));
    }

    [Fact]
    public void GeometryResultsKeepVectorAndRadiusOrder()
    {
        var center = new WorkerVectorValue(1, 2, 3);
        var normal = new WorkerVectorValue(0, 0, 1);
        var circle = GetCirclePropertiesOperation.CreateResult(Completed(
            Output("Center Coordinate", center), Output("Normal Direction", normal),
            Output("Radius", new WorkerDoubleValue(4)), Output("Diameter", new WorkerDoubleValue(8))));
        var ellipse = GetEllipsePropertiesOperation.CreateResult(Completed(
            Output("Center Coordinate", center), Output("Normal Direction", normal),
            Output("Major Axis Radius", new WorkerDoubleValue(5)),
            Output("Minor Axis Radius", new WorkerDoubleValue(2))));
        var sphere = GetSpherePropertiesOperation.CreateResult(Completed(
            Output("Center Coordinate", center), Output("Radius", new WorkerDoubleValue(6)),
            Output("Diameter", new WorkerDoubleValue(12))));
        var torus = GetTorusPropertiesOperation.CreateResult(Completed(
            Output("Center Coordinate", center), Output("Normal Direction", normal),
            Output("Major Radius", new WorkerDoubleValue(7)),
            Output("Minor Radius", new WorkerDoubleValue(1))));

        Assert.Equal(3, circle.CenterCoordinate.Z);
        Assert.Equal(4, circle.Radius);
        Assert.Equal(2, ellipse.MinorAxisRadius);
        Assert.Equal(12, sphere.Diameter);
        Assert.Equal(7, torus.MajorRadius);
        Assert.True(torus.HasMinorRadius);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedCircleRoute()
    {
        var worker = new CircleWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var result = await new Api.AnalysisOperations.AnalysisOperationsClient(channel)
            .GetCirclePropertiesAsync(new()
            {
                CircleName = new Api.CollectionObjectName { ObjectName = "circle" }
            }, new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));
        Assert.Equal(4, result.Radius);
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(MigratedIds[0], Assert.Single(worker.Commands).OperationId);
    }

    private static WorkerRetrievedOutput Output(string name, WorkerMpValue value) => new(
        name, value switch
        {
            WorkerVectorValue => WorkerMpValueKind.Vector,
            WorkerDoubleValue => WorkerMpValueKind.FloatingPoint,
            _ => throw new InvalidOperationException()
        }, value);

    private static SuccessfulOperationExecution Completed(params WorkerMpOutputValue[] outputs) => new(
        WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed"),
        new Api.MpExecutionDetails());

    private sealed class CircleWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            Assert.Equal("Get Circle Properties", command.StepName);
            var outputs = new[]
            {
                Output("Center Coordinate", new WorkerVectorValue(1, 2, 3)),
                Output("Normal Direction", new WorkerVectorValue(0, 0, 1)),
                Output("Radius", new WorkerDoubleValue(4)),
                Output("Diameter", new WorkerDoubleValue(8))
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
