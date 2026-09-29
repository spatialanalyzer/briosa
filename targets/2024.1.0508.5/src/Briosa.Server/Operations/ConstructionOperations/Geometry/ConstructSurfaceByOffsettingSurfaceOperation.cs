using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructSurfaceByOffsettingSurfaceOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_surface_by_offsetting_surface", "Construct surface by offsetting a surface",
        "briosa.ConstructionOperations", "ConstructSurfaceByOffsettingSurface",
        "/briosa.ConstructionOperations/ConstructSurfaceByOffsettingSurface",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructSurfaceByOffsettingSurfaceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Reference Surface", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ReferenceSurface, "reference_surface"), "SetCollectionObjectNameRefListArg"),
            new("Surface offset", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasSurfaceOffset ? request.SurfaceOffset : 0), "SetDoubleArg"),
            new("Hide original surface?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasHideOriginalSurface || request.HideOriginalSurface), "SetBoolArg")
        ], []);
    }

    public static Api.ConstructSurfaceByOffsettingSurfaceResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
