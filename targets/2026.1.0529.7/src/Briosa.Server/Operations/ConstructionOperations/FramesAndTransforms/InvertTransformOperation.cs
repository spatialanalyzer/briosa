using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class InvertTransformOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.invert_transform", "Invert Transform", "briosa.ConstructionOperations",
        "InvertTransform", "/briosa.ConstructionOperations/InvertTransform", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("inverse_transform", "Inverse Transform", WorkerMpValueKind.Transform)];

    public static WorkerMpCommand CreateCommand(Api.InvertTransformRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Transform", WorkerMpValueKind.Transform,
                TransformMapper.Required(request.Transform, "transform"), "SetTransformArg")],
            [new("Inverse Transform", WorkerMpValueKind.Transform, "GetTransformArg")]);
    }

    public static Api.InvertTransformResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            InverseTransform = TransformMapper.ToProtocol(completed.Execution.OutputValues[0]
                .RequireValue<WorkerTransformValue>()),
            Execution = completed.Details
        };
}
