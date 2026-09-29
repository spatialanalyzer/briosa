using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsFromCylinderOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_from_cylinder", "Construct Points from Cylinder",
        "briosa.ConstructionOperations", "ConstructPointsFromCylinder",
        "/briosa.ConstructionOperations/ConstructPointsFromCylinder",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsFromCylinderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cylinder Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CylinderName, "cylinder_name", WorkerObjectTypeValue.Cylinder), "SetCollectionObjectNameArg2"),
            new("Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupName, "group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructPointsFromCylinderResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
