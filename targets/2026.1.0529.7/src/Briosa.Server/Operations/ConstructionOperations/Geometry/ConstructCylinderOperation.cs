using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructCylinderOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_cylinder", "Construct Cylinder",
        "briosa.ConstructionOperations", "ConstructCylinder", "/briosa.ConstructionOperations/ConstructCylinder",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructCylinderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cylinder Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CylinderName, "cylinder_name", WorkerObjectTypeValue.Cylinder), "SetCollectionObjectNameArg2"),
            new("Cylinder End Point (in working coordinates)", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.CylinderEndPoint, "cylinder_end_point"), "SetVectorArg"),
            new("Cylinder Axis (in working coordinates)", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.CylinderAxis, "cylinder_axis"), "SetVectorArg"),
            new("Cylinder Diameter", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasCylinderDiameter ? request.CylinderDiameter : 0), "SetDoubleArg"),
            new("Cylinder Length", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasCylinderLength ? request.CylinderLength : 0), "SetDoubleArg")
        ], []);
    }

    public static Api.ConstructCylinderResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
