using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructSurfaceFromPointGroupsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_surface_from_point_groups", "Construct Surface From Point Groups",
        "briosa.ConstructionOperations", "ConstructSurfaceFromPointGroups",
        "/briosa.ConstructionOperations/ConstructSurfaceFromPointGroups",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructSurfaceFromPointGroupsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Group Name List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.GroupNameList, "group_name_list"), "SetCollectionObjectNameRefListArg"),
            new("B-Spline Fit Options", WorkerMpValueKind.BSplineFitOptions,
                BSplineFitOptionsMapper.Map(request.BSplineFitOptions), "SetBSplineFitOptionsArg"),
            new("Resulting Surface Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingSurfaceName, "resulting_surface_name", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructSurfaceFromPointGroupsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
