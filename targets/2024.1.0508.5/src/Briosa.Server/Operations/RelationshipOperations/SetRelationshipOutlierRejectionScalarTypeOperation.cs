using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetRelationshipOutlierRejectionScalarTypeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_relationship_outlier_rejection_scalar_type",
        "Set Relationship Outlier Rejection (Scalar Type)",
        "briosa.RelationshipOperations", "SetRelationshipOutlierRejectionScalarType",
        "/briosa.RelationshipOperations/SetRelationshipOutlierRejectionScalarType",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe,
        ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipOutlierRejectionScalarTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.SetRelationshipOutlierRejectionScalarTypeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
