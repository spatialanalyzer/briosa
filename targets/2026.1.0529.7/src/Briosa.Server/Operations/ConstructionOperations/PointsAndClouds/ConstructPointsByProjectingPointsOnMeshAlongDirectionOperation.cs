using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsByProjectingPointsOnMeshAlongDirectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_by_projecting_points_on_mesh_along_direction", "Construct Points By Projecting Points On Mesh Along Direction",
        "briosa.ConstructionOperations", "ConstructPointsByProjectingPointsOnMeshAlongDirection", "/briosa.ConstructionOperations/ConstructPointsByProjectingPointsOnMeshAlongDirection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_point_name_list", "Resultant Point Name List", WorkerMpValueKind.PointNameList)];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsByProjectingPointsOnMeshAlongDirectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Reference Point Names", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.ReferencePointNames, "reference_point_names"), "SetPointNameRefListArg"),
            new("Group Name For Projected Points", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupNameForProjectedPoints, "group_name_for_projected_points", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Object Providing Direction Reference", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ObjectProvidingDirectionReference, "object_providing_direction_reference", WorkerObjectTypeValue.Any), "SetCollectionObjectNameArg2"),
            new("Bi-directional projection?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasBiDirectionalProjection || request.BiDirectionalProjection), "SetBoolArg"),
            new("Mesh Serving As Projection Target", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.MeshServingAsProjectionTarget, "mesh_serving_as_projection_target", WorkerObjectTypeValue.ScanStripeMesh), "SetCollectionObjectNameArg2")
        ], [new("Resultant Point Name List", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg")]);
    }

    public static Api.ConstructPointsByProjectingPointsOnMeshAlongDirectionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.ConstructPointsByProjectingPointsOnMeshAlongDirectionResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerPointNameListValue>().Values)
            result.ResultantPointNameList.Add(PointNameMapper.ToProtocol(value));
        return result;
    }
}
