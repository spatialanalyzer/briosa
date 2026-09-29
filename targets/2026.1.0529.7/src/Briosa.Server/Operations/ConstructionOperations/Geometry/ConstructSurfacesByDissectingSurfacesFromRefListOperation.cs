using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructSurfacesByDissectingSurfacesFromRefListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_surfaces_by_dissecting_surfaces_from_ref_list",
        "Construct Surfaces by Dissecting Surfaces from Ref List", "briosa.ConstructionOperations",
        "ConstructSurfacesByDissectingSurfacesFromRefList",
        "/briosa.ConstructionOperations/ConstructSurfacesByDissectingSurfacesFromRefList",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_surfaces_list", "Resultant Surfaces List", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.ConstructSurfacesByDissectingSurfacesFromRefListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Surfaces to Dissect", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.SurfacesToDissect, "surfaces_to_dissect"),
                "SetCollectionObjectNameRefListArg")],
            [new("Resultant Surfaces List", WorkerMpValueKind.CollectionObjectNameList,
                "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.ConstructSurfacesByDissectingSurfacesFromRefListResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.ConstructSurfacesByDissectingSurfacesFromRefListResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.ResultantSurfacesList.Add(CollectionObjectNameMapper.ToProtocol(value));
        return result;
    }
}
