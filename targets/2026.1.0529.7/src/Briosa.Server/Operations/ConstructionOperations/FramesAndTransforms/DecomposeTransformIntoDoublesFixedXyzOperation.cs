using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class DecomposeTransformIntoDoublesFixedXyzOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.decompose_transform_into_doubles_fixed_xyz",
        "Decompose Transform into Doubles (Fixed XYZ)", "briosa.ConstructionOperations",
        "DecomposeTransformIntoDoublesFixedXyz", "/briosa.ConstructionOperations/DecomposeTransformIntoDoublesFixedXyz",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("x", "X", WorkerMpValueKind.FloatingPoint),
        new("y", "Y", WorkerMpValueKind.FloatingPoint),
        new("z", "Z", WorkerMpValueKind.FloatingPoint),
        new("rx", "Rx (Roll)", WorkerMpValueKind.FloatingPoint),
        new("ry", "Ry (Pitch)", WorkerMpValueKind.FloatingPoint),
        new("rz", "Rz (Yaw)", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.DecomposeTransformIntoDoublesFixedXyzRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Input Transform", WorkerMpValueKind.Transform,
                TransformMapper.Required(request.InputTransform, "input_transform"), "SetTransformArg")],
            OutputContracts.Select(value => new WorkerMpOutputArgument(value.ArgumentName, value.Kind, "GetDoubleArg")).ToArray());
    }

    public static Api.DecomposeTransformIntoDoublesFixedXyzResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            X = values[0].RequireValue<WorkerDoubleValue>().Value,
            Y = values[1].RequireValue<WorkerDoubleValue>().Value,
            Z = values[2].RequireValue<WorkerDoubleValue>().Value,
            Rx = values[3].RequireValue<WorkerDoubleValue>().Value,
            Ry = values[4].RequireValue<WorkerDoubleValue>().Value,
            Rz = values[5].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
