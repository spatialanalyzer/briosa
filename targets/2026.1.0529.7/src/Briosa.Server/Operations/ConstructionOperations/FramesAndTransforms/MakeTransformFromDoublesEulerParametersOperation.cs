using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeTransformFromDoublesEulerParametersOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_transform_from_doubles_euler_parameters",
        "Make a Transform from Doubles (Euler Parameters)", "briosa.ConstructionOperations",
        "MakeTransformFromDoublesEulerParameters", "/briosa.ConstructionOperations/MakeTransformFromDoublesEulerParameters",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_transform", "Resultant Transform", WorkerMpValueKind.Transform)];

    public static WorkerMpCommand CreateCommand(Api.MakeTransformFromDoublesEulerParametersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("X", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.X), "SetDoubleArg"),
            new("Y", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Y), "SetDoubleArg"),
            new("Z", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Z), "SetDoubleArg"),
            new("e1", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.E1), "SetDoubleArg"),
            new("e2", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.E2), "SetDoubleArg"),
            new("e3", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.E3), "SetDoubleArg"),
            new("e4", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.E4), "SetDoubleArg")
        ], [new("Resultant Transform", WorkerMpValueKind.Transform, "GetTransformArg")]);
    }

    public static Api.MakeTransformFromDoublesEulerParametersResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            ResultantTransform = TransformMapper.ToProtocol(completed.Execution.OutputValues[0]
                .RequireValue<WorkerTransformValue>()),
            Execution = completed.Details
        };
}
