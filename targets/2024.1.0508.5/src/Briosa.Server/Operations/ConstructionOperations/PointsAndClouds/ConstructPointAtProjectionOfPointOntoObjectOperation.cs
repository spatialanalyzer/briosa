using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointAtProjectionOfPointOntoObjectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_at_projection_of_point_onto_object", "Construct a Point at Projection of Point onto An Object",
        "briosa.ConstructionOperations", "ConstructPointAtProjectionOfPointOntoObject", "/briosa.ConstructionOperations/ConstructPointAtProjectionOfPointOntoObject",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointAtProjectionOfPointOntoObjectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point to Project", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointToProject, "point_to_project"), "SetPointNameArg"),
            new("Object Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ObjectName, "object_name", WorkerObjectTypeValue.Any), "SetCollectionObjectNameArg2"),
            new("Resulting Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.ResultingPointName, "resulting_point_name"), "SetPointNameArg")
        ], []);
    }

    public static Api.ConstructPointAtProjectionOfPointOntoObjectResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
