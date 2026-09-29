using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetRelationshipAutoVectorsFitAvfOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_relationship_auto_vectors_fit_avf", "Set Relationship Auto Vectors Fit (AVF)",
        "briosa.RelationshipOperations", "SetRelationshipAutoVectorsFitAvf",
        "/briosa.RelationshipOperations/SetRelationshipAutoVectorsFitAvf",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipAutoVectorsFitAvfRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Create Auto Vectors AVF", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasCreateAutoVectorsAvf && request.CreateAutoVectorsAvf), "SetBoolArg"),
                new("Use Vector Group Custom Prefix?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasUseVectorGroupCustomPrefix && request.UseVectorGroupCustomPrefix), "SetBoolArg"),
                new("Vector Group Custom Prefix", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasVectorGroupCustomPrefix ? request.VectorGroupCustomPrefix : string.Empty), "SetStringArg")
            ], []);
    }

    public static Api.SetRelationshipAutoVectorsFitAvfResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
