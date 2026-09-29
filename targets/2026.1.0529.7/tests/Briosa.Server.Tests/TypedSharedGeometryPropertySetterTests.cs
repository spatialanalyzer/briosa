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

public sealed class TypedSharedGeometryPropertySetterTests
{
    private static readonly string[] MigratedIds =
    ["analysis_operations.set_circle_properties", "analysis_operations.set_cylinder_properties"];

    [Fact]
    public void BothGeometrySettersHaveTypedRegistrationsAndPreserveTheirCommandDefaults()
    {
        foreach (var id in MigratedIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }

        var circle = SetCirclePropertiesOperation.CreateCommand(new()
        {
            CircleName = Object("circle"),
            CenterCoordinate = Vector(1, 2, 3),
            NormalDirection = Vector(0, 0, 1)
        });
        var cylinder = SetCylinderPropertiesOperation.CreateCommand(new()
        {
            CylinderName = Object("cylinder"),
            BeginCoordinate = Vector(1, 2, 3),
            AxisDirection = Vector(0, 0, 1)
        });

        AssertInputs(circle, "Set Circle Properties",
            ("Circle Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Center Coordinate", WorkerMpValueKind.Vector, "SetVectorArg"),
            ("Normal Direction", WorkerMpValueKind.Vector, "SetVectorArg"),
            ("Radius", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"));
        AssertInputs(cylinder, "Set Cylinder Properties",
            ("Cylinder Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Begin Coordinate", WorkerMpValueKind.Vector, "SetVectorArg"),
            ("Axis Direction", WorkerMpValueKind.Vector, "SetVectorArg"),
            ("Length", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Diameter", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Nominals Point Inward", WorkerMpValueKind.Logical, "SetBoolArg"),
            ("Facets", WorkerMpValueKind.WholeNumber, "SetIntegerArg"),
            ("Enable Theta Extent Display Mode", WorkerMpValueKind.Logical, "SetBoolArg"),
            ("Theta Start in Degrees", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Theta Span in Degrees", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"));

        Assert.Equal(new WorkerVectorValue(1, 2, 3), circle.InputArguments[1].RequireValue<WorkerVectorValue>());
        Assert.Equal(0d, circle.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, cylinder.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, cylinder.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.True(cylinder.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(32, cylinder.InputArguments[6].RequireValue<WorkerIntegerValue>().Value);
        Assert.True(cylinder.InputArguments[7].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0d, cylinder.InputArguments[8].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(360d, cylinder.InputArguments[9].RequireValue<WorkerDoubleValue>().Value);

        Assert.Throws<ArgumentException>(() => SetCirclePropertiesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetCylinderPropertiesOperation.CreateCommand(new()));
        Assert.Empty(circle.OutputArguments);
        Assert.Empty(cylinder.OutputArguments);
    }

    [Fact]
    public async Task GeneratedClientRoutesBothSharedGeometrySetters()
    {
        var worker = new RecordingWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.AnalysisOperations.AnalysisOperationsClient(channel);
        var deadline = DateTime.UtcNow.AddSeconds(20);
        var circle = await client.SetCirclePropertiesAsync(new()
        {
            CircleName = Object("circle"), CenterCoordinate = Vector(1, 2, 3), NormalDirection = Vector(0, 0, 1)
        }, new CallOptions(deadline: deadline));
        var cylinder = await client.SetCylinderPropertiesAsync(new()
        {
            CylinderName = Object("cylinder"), BeginCoordinate = Vector(1, 2, 3), AxisDirection = Vector(0, 0, 1)
        }, new CallOptions(deadline: deadline));

        Assert.Equal(MigratedIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(Api.MpExecutionState.Succeeded, circle.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, cylinder.Execution.State);
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
