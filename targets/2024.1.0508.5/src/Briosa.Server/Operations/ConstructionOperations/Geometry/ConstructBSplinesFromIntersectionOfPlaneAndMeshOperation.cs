using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructBSplinesFromIntersectionOfPlaneAndMeshOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_b_splines_from_intersection_of_plane_and_mesh", "Construct B-Splines From Intersection of Plane and Mesh",
        "briosa.ConstructionOperations", "ConstructBSplinesFromIntersectionOfPlaneAndMesh", "/briosa.ConstructionOperations/ConstructBSplinesFromIntersectionOfPlaneAndMesh",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("b_spline_list", "B-Spline List", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.ConstructBSplinesFromIntersectionOfPlaneAndMeshRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Resulting B-Spline Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingBSplineName, "resulting_b_spline_name", WorkerObjectTypeValue.BSpline), "SetCollectionObjectNameArg2"),
            new("Plane Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.PlaneName, "plane_name", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2"),
            new("Mesh Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.MeshName, "mesh_name", WorkerObjectTypeValue.ScanStripeMesh), "SetCollectionObjectNameArg2"),
            new("Delete closed lines whose number of segment is less than this value", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasClosedLineSegmentLimit ? request.ClosedLineSegmentLimit : 3), "SetIntegerArg"),
            new("Delete unclosed lines whose number of segment is less than this value", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasUnclosedLineSegmentLimit ? request.UnclosedLineSegmentLimit : 3), "SetIntegerArg"),
            new("Create Intersection Points?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasCreateIntersectionPoints || request.CreateIntersectionPoints), "SetBoolArg")
        ], [new("B-Spline List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.ConstructBSplinesFromIntersectionOfPlaneAndMeshResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.ConstructBSplinesFromIntersectionOfPlaneAndMeshResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.BSplineList.Add(CollectionObjectNameMapper.ToProtocol(value));
        return result;
    }
}
