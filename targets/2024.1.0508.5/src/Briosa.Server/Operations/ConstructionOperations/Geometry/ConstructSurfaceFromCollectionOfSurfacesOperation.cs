using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructSurfaceFromCollectionOfSurfacesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_surface_from_collection_of_surfaces", "Construct Surface From a Collection of Surfaces",
        "briosa.ConstructionOperations", "ConstructSurfaceFromCollectionOfSurfaces",
        "/briosa.ConstructionOperations/ConstructSurfaceFromCollectionOfSurfaces",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructSurfaceFromCollectionOfSurfacesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Surfaces to Combine", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.SurfacesToCombine, "surfaces_to_combine"), "SetCollectionObjectNameRefListArg"),
            new("Resulting Surface Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingSurfaceName, "resulting_surface_name", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2"),
            new("Hide Original Surfaces?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasHideOriginalSurfaces || request.HideOriginalSurfaces), "SetBoolArg"),
            new("Delete Original Surfaces?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasDeleteOriginalSurfaces && request.DeleteOriginalSurfaces), "SetBoolArg"),
            new("Enable Sewing Tolerance?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasEnableSewingTolerance && request.EnableSewingTolerance), "SetBoolArg"),
            new("Sewing Tolerance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasSewingTolerance ? request.SewingTolerance : -1), "SetDoubleArg")
        ], []);
    }

    public static Api.ConstructSurfaceFromCollectionOfSurfacesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
