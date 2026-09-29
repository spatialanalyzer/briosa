using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeTransformFromDoublesFixedXyzOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_transform_from_doubles_fixed_xyz",
        "Make a Transform from Doubles (Fixed XYZ)", "briosa.ConstructionOperations",
        "MakeTransformFromDoublesFixedXyz", "/briosa.ConstructionOperations/MakeTransformFromDoublesFixedXyz",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_transform", "Resultant Transform", WorkerMpValueKind.Transform)];

    public static WorkerMpCommand CreateCommand(Api.MakeTransformFromDoublesFixedXyzRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("X", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.X), "SetDoubleArg"),
            new("Y", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Y), "SetDoubleArg"),
            new("Z", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Z), "SetDoubleArg"),
            new("Rx (Roll)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Rx), "SetDoubleArg"),
            new("Ry (Pitch)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Ry), "SetDoubleArg"),
            new("Rz (Yaw)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Rz), "SetDoubleArg")
        ], [new("Resultant Transform", WorkerMpValueKind.Transform, "GetTransformArg")]);
    }

    public static Api.MakeTransformFromDoublesFixedXyzResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            ResultantTransform = TransformMapper.ToProtocol(completed.Execution.OutputValues[0]
                .RequireValue<WorkerTransformValue>()),
            Execution = completed.Details
        };
}
