using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class DecomposeTransformIntoDoublesEulerZyzOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.decompose_transform_into_doubles_euler_zyz",
        "Decompose Transform into Doubles (Euler ZYZ)", "briosa.ConstructionOperations",
        "DecomposeTransformIntoDoublesEulerZyz", "/briosa.ConstructionOperations/DecomposeTransformIntoDoublesEulerZyz",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("x", "X", WorkerMpValueKind.FloatingPoint),
        new("y", "Y", WorkerMpValueKind.FloatingPoint),
        new("z", "Z", WorkerMpValueKind.FloatingPoint),
        new("first_rz", "Euler Rz", WorkerMpValueKind.FloatingPoint),
        new("ry", "Euler Ry", WorkerMpValueKind.FloatingPoint),
        new("second_rz", "Euler Rz", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.DecomposeTransformIntoDoublesEulerZyzRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Input Transform", WorkerMpValueKind.Transform,
                TransformMapper.Required(request.InputTransform, "input_transform"), "SetTransformArg")],
            OutputContracts.Select(value => new WorkerMpOutputArgument(value.ArgumentName, value.Kind, "GetDoubleArg")).ToArray());
    }

    public static Api.DecomposeTransformIntoDoublesEulerZyzResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            X = values[0].RequireValue<WorkerDoubleValue>().Value,
            Y = values[1].RequireValue<WorkerDoubleValue>().Value,
            Z = values[2].RequireValue<WorkerDoubleValue>().Value,
            FirstRz = values[3].RequireValue<WorkerDoubleValue>().Value,
            Ry = values[4].RequireValue<WorkerDoubleValue>().Value,
            SecondRz = values[5].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
