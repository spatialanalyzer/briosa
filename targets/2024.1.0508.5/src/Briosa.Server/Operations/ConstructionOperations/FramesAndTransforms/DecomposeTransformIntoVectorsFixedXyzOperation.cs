using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class DecomposeTransformIntoVectorsFixedXyzOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.decompose_transform_into_vectors_fixed_xyz",
        "Decompose Transform into Vectors (Fixed XYZ)", "briosa.ConstructionOperations",
        "DecomposeTransformIntoVectorsFixedXyz", "/briosa.ConstructionOperations/DecomposeTransformIntoVectorsFixedXyz",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("position_in_working", "Position in Working", WorkerMpValueKind.Vector),
        new("orientation_in_working", "Orientation in Working", WorkerMpValueKind.Vector)
    ];

    public static WorkerMpCommand CreateCommand(Api.DecomposeTransformIntoVectorsFixedXyzRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Input Transform", WorkerMpValueKind.Transform,
                TransformMapper.Required(request.InputTransform, "input_transform"), "SetTransformArg")],
        [
            new("Position in Working", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("Orientation in Working", WorkerMpValueKind.Vector, "GetVectorArg")
        ]);
    }

    public static Api.DecomposeTransformIntoVectorsFixedXyzResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            PositionInWorking = VectorMapper.ToProtocol(values[0].RequireValue<WorkerVectorValue>()),
            OrientationInWorking = VectorMapper.ToProtocol(values[1].RequireValue<WorkerVectorValue>()),
            Execution = completed.Details
        };
    }
}
