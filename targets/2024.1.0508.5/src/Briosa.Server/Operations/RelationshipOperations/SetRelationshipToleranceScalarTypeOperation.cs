using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetRelationshipToleranceScalarTypeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_relationship_tolerance_scalar_type", "Set Relationship Tolerance (Scalar Type)",
        "briosa.RelationshipOperations", "SetRelationshipToleranceScalarType",
        "/briosa.RelationshipOperations/SetRelationshipToleranceScalarType",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipToleranceScalarTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Tolerance Options", WorkerMpValueKind.ToleranceScalarOptions,
                    ToleranceScalarOptionsMapper.ToWorker(request.ToleranceOptions), "SetToleranceScalarOptionsArg")
            ], []);
    }

    public static Api.SetRelationshipToleranceScalarTypeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
