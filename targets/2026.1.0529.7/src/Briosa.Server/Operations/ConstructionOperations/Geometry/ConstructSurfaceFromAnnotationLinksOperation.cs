using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructSurfaceFromAnnotationLinksOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_surface_from_annotation_links", "Construct Surface From Annotation Links",
        "briosa.ConstructionOperations", "ConstructSurfaceFromAnnotationLinks",
        "/briosa.ConstructionOperations/ConstructSurfaceFromAnnotationLinks",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructSurfaceFromAnnotationLinksRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Annotation List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.AnnotationList, "annotation_list"), "SetCollectionObjectNameRefListArg"),
            new("Resulting Surface Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingSurfaceName, "resulting_surface_name", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructSurfaceFromAnnotationLinksResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
