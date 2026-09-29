using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructSurfaceFromConeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_surface_from_cone", "Construct Surface From Cone",
        "briosa.ConstructionOperations", "ConstructSurfaceFromCone", "/briosa.ConstructionOperations/ConstructSurfaceFromCone",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructSurfaceFromConeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Resulting Surface Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingSurfaceName, "resulting_surface_name", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2"),
            new("Cone Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ConeName, "cone_name", WorkerObjectTypeValue.Cone), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructSurfaceFromConeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
