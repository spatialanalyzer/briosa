using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ShiftPlaneOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.shift_plane", "Shift Plane", "briosa.ConstructionOperations", "ShiftPlane",
        "/briosa.ConstructionOperations/ShiftPlane",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShiftPlaneRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Plane", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Plane, "plane"), "SetCollectionObjectNameArg2"),
            new("Shift Along Normal", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasShiftAlongNormal ? request.ShiftAlongNormal : 0), "SetDoubleArg"),
            new("Grow Bounds by Factor", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasGrowBoundsByFactor ? request.GrowBoundsByFactor : 0), "SetDoubleArg")
        ], []);
    }

    public static Api.ShiftPlaneResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
