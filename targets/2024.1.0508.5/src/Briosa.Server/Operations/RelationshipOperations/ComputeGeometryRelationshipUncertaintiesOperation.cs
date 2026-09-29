using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class ComputeGeometryRelationshipUncertaintiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.compute_geometry_relationship_uncertainties",
        "Compute Geometry Relationship Uncertainties",
        "briosa.RelationshipOperations", "ComputeGeometryRelationshipUncertainties",
        "/briosa.RelationshipOperations/ComputeGeometryRelationshipUncertainties",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ComputeGeometryRelationshipUncertaintiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("Display Results", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasDisplayResults && request.DisplayResults), "SetBoolArg")
            ], []);
    }

    public static Api.ComputeGeometryRelationshipUncertaintiesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
