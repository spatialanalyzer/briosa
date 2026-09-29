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

public sealed class TypedTargetSpecificGeometryPropertySetterTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.set_cone_properties",
        "analysis_operations.set_ellipse_properties",
        "analysis_operations.set_line_properties"
    ];

    [Fact]
    public void TargetSpecificSettersPreserveEvery2026ArgumentAndDefault()
    {
        foreach (var id in MigratedIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }

        var cone = SetConePropertiesOperation.CreateCommand(new()
        {
            ConeName = Object("cone"), ConeEndPoint = Vector(1, 2, 3), ConeAxis = Vector(0, 0, 1)
        });
        var ellipse = SetEllipsePropertiesOperation.CreateCommand(new()
        {
            EllipseName = Object("ellipse"), CenterCoordinate = Vector(1, 2, 3), NormalDirection = Vector(0, 0, 1)
        });
        var line = SetLinePropertiesOperation.CreateCommand(new()
        {
            LineName = Object("line"), BeginCoordinate = Vector(1, 2, 3), EndCoordinate = Vector(4, 5, 6)
        });

        AssertInputs(cone, "Set Cone Properties",
            ("Cone Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Cone End Point (in working coordinates)", WorkerMpValueKind.Vector, "SetVectorArg"),
            ("Cone Axis (in working coordinates)", WorkerMpValueKind.Vector, "SetVectorArg"),
            ("Cone Length", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Cone Theta Start", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Cone Theta Span", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Cone Included Angle", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Cut Length from Apex", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"));
        AssertInputs(ellipse, "Set Ellipse Properties",
            ("Ellipse Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Center Coordinate", WorkerMpValueKind.Vector, "SetVectorArg"),
            ("Normal Direction", WorkerMpValueKind.Vector, "SetVectorArg"),
            ("Major Axis Radius", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Minor Axis Radius", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"));
        AssertInputs(line, "Set Line Properties",
            ("Line Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Begin Coordinate", WorkerMpValueKind.Vector, "SetVectorArg"),
            ("End Coordinate", WorkerMpValueKind.Vector, "SetVectorArg"),
            ("Length (optional)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"));

        Assert.Equal(0d, cone.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, cone.InputArguments[7].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, ellipse.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, ellipse.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, line.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Empty(cone.OutputArguments);
        Assert.Empty(ellipse.OutputArguments);
        Assert.Empty(line.OutputArguments);

        Assert.Throws<ArgumentException>(() => SetConePropertiesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetEllipsePropertiesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetLinePropertiesOperation.CreateCommand(new()));
    }

    [Fact]
    public async Task GeneratedClientRoutesTheThree2026OnlySetters()
    {
        var worker = new RecordingWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.AnalysisOperations.AnalysisOperationsClient(channel);
        var deadline = DateTime.UtcNow.AddSeconds(20);
        var cone = await client.SetConePropertiesAsync(new()
        {
            ConeName = Object("cone"), ConeEndPoint = Vector(1, 2, 3), ConeAxis = Vector(0, 0, 1)
        }, new CallOptions(deadline: deadline));
        var ellipse = await client.SetEllipsePropertiesAsync(new()
        {
            EllipseName = Object("ellipse"), CenterCoordinate = Vector(1, 2, 3), NormalDirection = Vector(0, 0, 1)
        }, new CallOptions(deadline: deadline));
        var line = await client.SetLinePropertiesAsync(new()
        {
            LineName = Object("line"), BeginCoordinate = Vector(1, 2, 3), EndCoordinate = Vector(4, 5, 6)
        }, new CallOptions(deadline: deadline));

        Assert.Equal(MigratedIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(Api.MpExecutionState.Succeeded, cone.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, ellipse.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, line.Execution.State);
    }

    private static Api.CollectionObjectName Object(string name) => new() { ObjectName = name };

    private static Api.Vector Vector(double x, double y, double z) => new() { X = x, Y = y, Z = z };

    private static void AssertInputs(WorkerMpCommand command, string step,
        params (string Name, WorkerMpValueKind Kind, string? Binding)[] expected)
    {
        Assert.Equal(step, command.StepName);
        Assert.Equal(expected,
            command.InputArguments.Select(argument => (argument.Name, argument.Kind, argument.SdkBinding)).ToArray());
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
