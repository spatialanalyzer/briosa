using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class DecomposeTransformIntoVectorsOriginAndAxesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.decompose_transform_into_vectors_origin_and_axes",
        "Decompose Transform into Vectors (Origin and Axes)", "briosa.ConstructionOperations",
        "DecomposeTransformIntoVectorsOriginAndAxes",
        "/briosa.ConstructionOperations/DecomposeTransformIntoVectorsOriginAndAxes", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("origin", "Origin", WorkerMpValueKind.Vector),
        new("x_axis", "X Axis", WorkerMpValueKind.Vector),
        new("y_axis", "Y Axis", WorkerMpValueKind.Vector),
        new("z_axis", "Z Axis", WorkerMpValueKind.Vector)
    ];

    public static WorkerMpCommand CreateCommand(Api.DecomposeTransformIntoVectorsOriginAndAxesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Transform", WorkerMpValueKind.Transform,
                TransformMapper.Required(request.Transform, "transform"), "SetTransformArg")],
            OutputContracts.Select(value => new WorkerMpOutputArgument(value.ArgumentName, value.Kind, "GetVectorArg")).ToArray());
    }

    public static Api.DecomposeTransformIntoVectorsOriginAndAxesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Origin = VectorMapper.ToProtocol(values[0].RequireValue<WorkerVectorValue>()),
            XAxis = VectorMapper.ToProtocol(values[1].RequireValue<WorkerVectorValue>()),
            YAxis = VectorMapper.ToProtocol(values[2].RequireValue<WorkerVectorValue>()),
            ZAxis = VectorMapper.ToProtocol(values[3].RequireValue<WorkerVectorValue>()),
            Execution = completed.Details
        };
    }
}
