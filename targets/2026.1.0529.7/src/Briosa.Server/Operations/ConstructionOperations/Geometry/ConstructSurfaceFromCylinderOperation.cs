using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructSurfaceFromCylinderOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_surface_from_cylinder", "Construct Surface From Cylinder",
        "briosa.ConstructionOperations", "ConstructSurfaceFromCylinder", "/briosa.ConstructionOperations/ConstructSurfaceFromCylinder",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructSurfaceFromCylinderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Resulting Surface Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingSurfaceName, "resulting_surface_name", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2"),
            new("Cylinder Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CylinderName, "cylinder_name", WorkerObjectTypeValue.Cylinder), "SetCollectionObjectNameArg2"),
            new("Internal Cylinder?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasInternalCylinder ? request.InternalCylinder : true), "SetBoolArg"),
            new("Use Theta Extent Mode?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasUseThetaExtentMode && request.UseThetaExtentMode), "SetBoolArg")
        ], []);
    }

    public static Api.ConstructSurfaceFromCylinderResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
