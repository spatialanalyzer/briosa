using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructLineProjectLineToObjectReferencePlaneOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_line_project_line_to_object_reference_plane", "Construct Line - Project Line to Object Reference Plane",
        "briosa.ConstructionOperations", "ConstructLineProjectLineToObjectReferencePlane", "/briosa.ConstructionOperations/ConstructLineProjectLineToObjectReferencePlane",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructLineProjectLineToObjectReferencePlaneRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Line To Create", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.LineToCreate, "line_to_create", WorkerObjectTypeValue.Line), "SetCollectionObjectNameArg2"),
            new("Line To Project", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.LineToProject, "line_to_project", WorkerObjectTypeValue.Line), "SetCollectionObjectNameArg2"),
            new("Object to project to", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ObjectToProjectTo, "object_to_project_to", WorkerObjectTypeValue.Any), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructLineProjectLineToObjectReferencePlaneResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
