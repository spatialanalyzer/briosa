using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructSurfacesByProjectingPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_surfaces_by_projecting_points", "Construct Surfaces By Projecting Points",
        "briosa.ConstructionOperations", "ConstructSurfacesByProjectingPoints",
        "/briosa.ConstructionOperations/ConstructSurfacesByProjectingPoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructSurfacesByProjectingPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Projection Target Name List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ProjectionTargetNameList, "projection_target_name_list"), "SetCollectionObjectNameRefListArg"),
            new("Point List", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.PointList, "point_list"), "SetPointNameRefListArg"),
            new("Resulting Surface Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingSurfaceName, "resulting_surface_name", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructSurfacesByProjectingPointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
