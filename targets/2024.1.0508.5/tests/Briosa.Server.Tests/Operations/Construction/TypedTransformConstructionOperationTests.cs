using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedTransformConstructionOperationTests
{
    private static readonly string[] Ids =
    [
        "construction_operations.decompose_transform_into_doubles_euler_xyz",
        "construction_operations.decompose_transform_into_doubles_euler_zxz",
        "construction_operations.decompose_transform_into_doubles_euler_zyx",
        "construction_operations.decompose_transform_into_doubles_euler_zyz",
        "construction_operations.decompose_transform_into_doubles_fixed_xyz",
        "construction_operations.decompose_transform_into_vectors_fixed_xyz",
        "construction_operations.decompose_transform_into_vectors_origin_and_axes",
        "construction_operations.decompose_world_transform_operator_into_doubles_fixed_xyz_in_world",
        "construction_operations.decompose_world_transform_operator_into_vectors_fixed_xyz_in_world",
        "construction_operations.get_working_transform_of_object_fixed_xyz",
        "construction_operations.invert_transform",
        "construction_operations.make_transform_from_doubles_euler_parameters",
        "construction_operations.make_transform_from_doubles_fixed_xyz"
    ];

    private static readonly string[] ZxzOutputNames = ["X", "Y", "Z", "Euler Rz", "Euler Rx", "Euler Rz"];

    [Fact]
    public void TransformRoutesAreTypedAndRegistered()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, descriptor => descriptor.OperationId == id);
        }
    }

    [Fact]
    public void TransformInputsAndOutputsPreserveExactBindings()
    {
        Assert.Throws<ArgumentException>(() => InvertTransformOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => InvertTransformOperation.CreateCommand(new()
            { Transform = new Api.Transform { Values = { 1d } } }));
        Assert.Throws<ArgumentException>(() => GetWorkingTransformOfObjectFixedXyzOperation.CreateCommand(new()));
        var working = GetWorkingTransformOfObjectFixedXyzOperation.CreateCommand(new()
            { ObjectName = new Api.CollectionObjectName { CollectionName = "C", ObjectName = "O" } });
        Assert.Equal("SetCollectionObjectNameArg2", Assert.Single(working.InputArguments).SdkBinding);
        Assert.Equal("GetTransformArg", Assert.Single(working.OutputArguments).SdkBinding);
        Assert.Equal("SetTransformArg", Assert.Single(InvertTransformOperation.CreateCommand(new()
            { Transform = Matrix() }).InputArguments).SdkBinding);

        var fixedXyz = MakeTransformFromDoublesFixedXyzOperation.CreateCommand(new() { X = 3, Rz = 6 });
        Assert.Equal(6, fixedXyz.InputArguments.Count);
        Assert.All(fixedXyz.InputArguments, input => Assert.Equal("SetDoubleArg", input.SdkBinding));
        Assert.Equal(3d, fixedXyz.InputArguments[0].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("GetTransformArg", Assert.Single(fixedXyz.OutputArguments).SdkBinding);
        var eulerParameters = MakeTransformFromDoublesEulerParametersOperation.CreateCommand(new() { E4 = 7 });
        Assert.Equal(7, eulerParameters.InputArguments.Count);
        Assert.Equal(7d, eulerParameters.InputArguments[6].RequireValue<WorkerDoubleValue>().Value);

        var matrix = Matrix();
        var sixDoubleCommands = new[]
        {
            DecomposeTransformIntoDoublesFixedXyzOperation.CreateCommand(new() { InputTransform = matrix }),
            DecomposeTransformIntoDoublesEulerXyzOperation.CreateCommand(new() { InputTransform = matrix }),
            DecomposeTransformIntoDoublesEulerZyxOperation.CreateCommand(new() { InputTransform = matrix }),
            DecomposeTransformIntoDoublesEulerZxzOperation.CreateCommand(new() { InputTransform = matrix }),
            DecomposeTransformIntoDoublesEulerZyzOperation.CreateCommand(new() { InputTransform = matrix })
        };
        foreach (var command in sixDoubleCommands)
        {
            Assert.Equal("SetTransformArg", Assert.Single(command.InputArguments).SdkBinding);
            Assert.Equal(6, command.OutputArguments.Count);
            Assert.All(command.OutputArguments, output => Assert.Equal("GetDoubleArg", output.SdkBinding));
        }
        Assert.Equal(["X", "Y", "Z", "Euler Rz", "Euler Rx", "Euler Rz"],
            sixDoubleCommands[3].OutputArguments.Select(output => output.Name));
        Assert.Equal(["X", "Y", "Z", "Euler Rz", "Euler Ry", "Euler Rx"],
            sixDoubleCommands[2].OutputArguments.Select(output => output.Name));

        var vectors = DecomposeTransformIntoVectorsFixedXyzOperation.CreateCommand(new() { InputTransform = matrix });
        Assert.Equal(["GetVectorArg", "GetVectorArg"],
            vectors.OutputArguments.Select(output => output.SdkBinding));
        var axes = DecomposeTransformIntoVectorsOriginAndAxesOperation.CreateCommand(new() { Transform = matrix });
        Assert.Equal(["Origin", "X Axis", "Y Axis", "Z Axis"],
            axes.OutputArguments.Select(output => output.Name));
        Assert.Throws<ArgumentException>(() => DecomposeWorldTransformOperatorIntoDoublesFixedXyzInWorldOperation
            .CreateCommand(new()));
        var world = new Api.WorldTransform { Transform = matrix };
        var worldDoubles = DecomposeWorldTransformOperatorIntoDoublesFixedXyzInWorldOperation.CreateCommand(new()
            { InputWorldTransformOperator = world });
        Assert.Equal("SetWorldTransformArg", Assert.Single(worldDoubles.InputArguments).SdkBinding);
        Assert.Equal(1d, worldDoubles.InputArguments[0].RequireValue<WorkerWorldTransformValue>().ScaleFactor);
        Assert.Equal(7, worldDoubles.OutputArguments.Count);
        var worldVectors = DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorldOperation.CreateCommand(new()
            { InputWorldTransformOperator = world });
        Assert.Equal("GetVectorArg,GetVectorArg,GetDoubleArg",
            string.Join(',', worldVectors.OutputArguments.Select(output => output.SdkBinding)));
    }

    [Fact]
    public void ResultMappersKeepTransformVectorAndEulerOutputOrder()
    {
        var transform = new WorkerRetrievedOutput("Inverse Transform", WorkerMpValueKind.Transform,
            new WorkerTransformValue(Enumerable.Range(0, 16).Select(index => (double)index).ToArray()));
        Assert.Equal(15d, InvertTransformOperation.CreateResult(Success([transform])).InverseTransform.Values[15]);
        var doubles = ZxzOutputNames
            .Select((name, index) => new WorkerRetrievedOutput(name, WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(index + 1d))).ToArray();
        var zxz = DecomposeTransformIntoDoublesEulerZxzOperation.CreateResult(Success(doubles));
        Assert.Equal((4d, 5d, 6d), (zxz.FirstRz, zxz.Rx, zxz.SecondRz));
        var vectorOutputs = new WorkerMpOutputValue[]
        {
            new WorkerRetrievedOutput("Position in Working", WorkerMpValueKind.Vector, new WorkerVectorValue(1, 2, 3)),
            new WorkerRetrievedOutput("Orientation in Working", WorkerMpValueKind.Vector, new WorkerVectorValue(4, 5, 6)),
            new WorkerRetrievedOutput("Scale", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2.5))
        };
        var world = DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorldOperation
            .CreateResult(Success(vectorOutputs));
        Assert.Equal(3d, world.PositionInWorking.Z);
        Assert.Equal(5d, world.OrientationInWorking.Y);
        Assert.Equal(2.5, world.Scale);
    }

    [Fact]
    public async Task GeneratedClientUsesTypedTransformRoute()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(host.Channel);
        var result = await client.MakeTransformFromDoublesFixedXyzAsync(new() { X = 3 },
            new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(16, result.ResultantTransform.Values.Count);
        Assert.Equal("construction_operations.make_transform_from_doubles_fixed_xyz",
            Assert.Single(worker.Commands).OperationId);
    }

    private static Api.Transform Matrix() => new()
        { Values = { Enumerable.Range(0, 16).Select(index => (double)index) } };

    private static SuccessfulOperationExecution Success(IReadOnlyList<WorkerMpOutputValue> outputs)
    {
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
        return new(execution, new Api.MpExecutionDetails());
    }

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs =
            [
                new WorkerRetrievedOutput("Resultant Transform", WorkerMpValueKind.Transform,
                    new WorkerTransformValue(Enumerable.Range(0, 16).Select(index => (double)index).ToArray()))
            ];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
