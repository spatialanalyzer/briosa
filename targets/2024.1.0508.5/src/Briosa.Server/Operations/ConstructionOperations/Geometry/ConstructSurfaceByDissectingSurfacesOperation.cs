using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructSurfaceByDissectingSurfacesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_surface_by_dissecting_surfaces", "Construct Surface by Dissecting Surface(s)",
        "briosa.ConstructionOperations", "ConstructSurfaceByDissectingSurfaces",
        "/briosa.ConstructionOperations/ConstructSurfaceByDissectingSurfaces",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_surfaces_list", "Resultant Surfaces List", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.ConstructSurfaceByDissectingSurfacesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var mode = request.DissectionMode switch
        {
            Api.SurfaceDissectionMode.EntireSolid => WorkerSurfaceDissectionModeTypeValue.EntireSolid,
            Api.SurfaceDissectionMode.SelectFaces => WorkerSurfaceDissectionModeTypeValue.SelectFaces,
            _ => throw new ArgumentException("A supported dissection_mode is required.", nameof(request))
        };
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Dissection Mode", WorkerMpValueKind.SurfaceDissectionModeType,
                new WorkerChoiceValue<WorkerSurfaceDissectionModeTypeValue>(mode), "SetSurfDissectModeTypeArg")],
            [new("Resultant Surfaces List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.ConstructSurfaceByDissectingSurfacesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.ConstructSurfaceByDissectingSurfacesResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.ResultantSurfacesList.Add(CollectionObjectNameMapper.ToProtocol(value));
        return result;
    }
}
