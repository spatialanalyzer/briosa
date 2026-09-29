using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorldOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.decompose_world_transform_operator_into_vectors_fixed_xyz_in_world",
        "Decompose World Transform Operator into Vectors (Fixed XYZ in World)", "briosa.ConstructionOperations",
        "DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorld",
        "/briosa.ConstructionOperations/DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorld",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("position_in_working", "Position in Working", WorkerMpValueKind.Vector),
        new("orientation_in_working", "Orientation in Working", WorkerMpValueKind.Vector),
        new("scale", "Scale", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorldRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Input World Transform Operator", WorkerMpValueKind.WorldTransform,
                WorldTransformMapper.Required(request.InputWorldTransformOperator, "input_world_transform_operator"),
                "SetWorldTransformArg")],
        [
            new("Position in Working", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("Orientation in Working", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("Scale", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
        ]);
    }

    public static Api.DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorldResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            PositionInWorking = VectorMapper.ToProtocol(values[0].RequireValue<WorkerVectorValue>()),
            OrientationInWorking = VectorMapper.ToProtocol(values[1].RequireValue<WorkerVectorValue>()),
            Scale = values[2].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
