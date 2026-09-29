using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructCylinderFromEndPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_cylinder_from_end_points", "Construct Cylinder From End Points",
        "briosa.ConstructionOperations", "ConstructCylinderFromEndPoints", "/briosa.ConstructionOperations/ConstructCylinderFromEndPoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructCylinderFromEndPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cylinder Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CylinderName, "cylinder_name", WorkerObjectTypeValue.Cylinder), "SetCollectionObjectNameArg2"),
            new("Cylinder End Point A (in working coordinates)", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.CylinderEndPointA, "cylinder_end_point_a"), "SetVectorArg"),
            new("Cylinder End Point B (in working coordinates)", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.CylinderEndPointB, "cylinder_end_point_b"), "SetVectorArg"),
            new("Cylinder Diameter", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasCylinderDiameter ? request.CylinderDiameter : 0), "SetDoubleArg")
        ], []);
    }

    public static Api.ConstructCylinderFromEndPointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
