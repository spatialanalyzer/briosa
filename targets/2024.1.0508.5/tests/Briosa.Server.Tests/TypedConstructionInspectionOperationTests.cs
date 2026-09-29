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

public sealed class TypedConstructionInspectionOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.mushroom_target_hole_inspection",
        "analysis_operations.patch_normal_shift_hole_pin",
        "analysis_operations.patch_normal_shift_point",
        "analysis_operations.sphere_axis_check"
    ];

    [Fact]
    public void ConstructionAndInspectionOperationsHaveTypedMutationRegistrations()
    {
        foreach (var id in MigratedIds)
        {
            var descriptor = Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, descriptor.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, descriptor.ReplaySafety);
            Assert.Empty(descriptor.RiskFlags);
        }

        var mushroom = MushroomTargetHoleInspectionOperation.CreateCommand(new()
        {
            SpherePointsGroupName = Object("sphere points"),
            TargetContactPlane = Object("plane"),
            PointToCreateAtHole = Point("hole")
        });
        var holePin = PatchNormalShiftHolePinOperation.CreateCommand(new()
        {
            PlanePointsGroupName = Object("plane points"),
            PerimeterPointsGroupName = Object("perimeter"),
            ResultingPointName = Point("pin")
        });
        var pointShift = PatchNormalShiftPointOperation.CreateCommand(new()
        {
            PlanePointsGroupName = Object("plane points"),
            PointToShift = Point("original"),
            ResultingPointName = Point("shifted")
        });
        var sphereAxis = SphereAxisCheckOperation.CreateCommand(new()
        {
            SpherePointsGroupName = Object("sphere points"),
            PointToCreateAtSphereCenter = Point("center"),
            LineDefiningTheAxis = Object("axis")
        });

        AssertInputs(mushroom, "Mushroom Target Hole Inspection",
            ("Name Prefix for Intermediate Constructions", WorkerMpValueKind.Text, "SetStringArg"),
            ("Sphere Points Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Sphere Target Radius", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Target Contact Plane", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Point To Create at Hole", WorkerMpValueKind.PointName, "SetPointNameArg"));
        AssertInputs(holePin, "Patch Normal Shift - Hole / Pin",
            ("Plane Points Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Perimeter Points Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Resulting Point Name", WorkerMpValueKind.PointName, "SetPointNameArg"),
            ("Additional Material Thickness", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"));
        AssertInputs(pointShift, "Patch Normal Shift - Point",
            ("Plane Points Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Point to Shift", WorkerMpValueKind.PointName, "SetPointNameArg"),
            ("Resulting Point Name", WorkerMpValueKind.PointName, "SetPointNameArg"),
            ("Additional Material Thickness", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"));
        AssertInputs(sphereAxis, "Sphere Axis Check",
            ("Sphere Points Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Sphere Target Radius", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Point To Create at Sphere Center", WorkerMpValueKind.PointName, "SetPointNameArg"),
            ("Line defining the axis", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"));

        Assert.Equal(string.Empty, mushroom.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0d, mushroom.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, holePin.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, pointShift.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, sphereAxis.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(["Sphere Fit RMS Error", "Sphere Fit Max Error"],
            mushroom.OutputArguments.Select(argument => argument.Name));
        Assert.Equal(["Sphere Fit RMS Error", "Sphere Fit Max Error", "Vector Representation", "X Value", "Y Value", "Z Value", "Magnitude"],
            sphereAxis.OutputArguments.Select(argument => argument.Name));
        Assert.Empty(holePin.OutputArguments);
        Assert.Empty(pointShift.OutputArguments);

        Assert.Throws<ArgumentException>(() => MushroomTargetHoleInspectionOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => PatchNormalShiftHolePinOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => PatchNormalShiftPointOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SphereAxisCheckOperation.CreateCommand(new()));
    }

    [Fact]
    public async Task GeneratedClientRoutesAllFourOperationsAndMapsMixedOutputs()
    {
        var worker = new RecordingWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.AnalysisOperations.AnalysisOperationsClient(channel);
        var deadline = DateTime.UtcNow.AddSeconds(20);
        var mushroom = await client.MushroomTargetHoleInspectionAsync(new()
        {
            SpherePointsGroupName = Object("sphere points"),
            TargetContactPlane = Object("plane"),
            PointToCreateAtHole = Point("hole")
        }, new CallOptions(deadline: deadline));
        var holePin = await client.PatchNormalShiftHolePinAsync(new()
        {
            PlanePointsGroupName = Object("plane points"),
            PerimeterPointsGroupName = Object("perimeter"),
            ResultingPointName = Point("pin")
        }, new CallOptions(deadline: deadline));
        var pointShift = await client.PatchNormalShiftPointAsync(new()
        {
            PlanePointsGroupName = Object("plane points"),
            PointToShift = Point("original"),
            ResultingPointName = Point("shifted")
        }, new CallOptions(deadline: deadline));
        var sphereAxis = await client.SphereAxisCheckAsync(new()
        {
            SpherePointsGroupName = Object("sphere points"),
            PointToCreateAtSphereCenter = Point("center"),
            LineDefiningTheAxis = Object("axis")
        }, new CallOptions(deadline: deadline));

        Assert.Equal(MigratedIds, worker.Commands.Select(command => command.OperationId));
        Assert.All(new[] { mushroom.Execution, holePin.Execution, pointShift.Execution, sphereAxis.Execution },
            execution => Assert.Equal(Api.MpExecutionState.Succeeded, execution.State));
        Assert.Equal(1d, mushroom.SphereFitRmsError);
        Assert.Equal(2d, mushroom.SphereFitMaxError);
        Assert.Equal(1d, sphereAxis.SphereFitRmsError);
        Assert.Equal(2d, sphereAxis.SphereFitMaxError);
        Assert.Equal(new Api.Vector { X = 10, Y = 11, Z = 12 }, sphereAxis.VectorRepresentation);
        Assert.Equal(4d, sphereAxis.XValue);
        Assert.Equal(5d, sphereAxis.YValue);
        Assert.Equal(6d, sphereAxis.ZValue);
        Assert.Equal(7d, sphereAxis.Magnitude);
    }

    private static Api.CollectionObjectName Object(string name) => new() { ObjectName = name };

    private static Api.PointName Point(string name) => new() { TargetName = name };

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
            var outputs = command.OutputArguments.Select((argument, index) =>
            {
                WorkerMpValue value = argument.Kind switch
                {
                    WorkerMpValueKind.FloatingPoint => new WorkerDoubleValue(index + 1),
                    WorkerMpValueKind.Vector => new WorkerVectorValue(10, 11, 12),
                    _ => throw new InvalidOperationException($"Unexpected fake output kind {argument.Kind}.")
                };
                return (WorkerMpOutputValue)new WorkerRetrievedOutput(argument.Name, argument.Kind, value);
            }).ToArray();
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
