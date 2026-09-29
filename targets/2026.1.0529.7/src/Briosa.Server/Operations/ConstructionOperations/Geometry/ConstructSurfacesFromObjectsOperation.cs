using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructSurfacesFromObjectsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_surfaces_from_objects", "Construct Surfaces From Objects",
        "briosa.ConstructionOperations", "ConstructSurfacesFromObjects",
        "/briosa.ConstructionOperations/ConstructSurfacesFromObjects",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructSurfacesFromObjectsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Objects", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.Objects, "objects"), "SetCollectionObjectNameRefListArg")], []);
    }

    public static Api.ConstructSurfacesFromObjectsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
