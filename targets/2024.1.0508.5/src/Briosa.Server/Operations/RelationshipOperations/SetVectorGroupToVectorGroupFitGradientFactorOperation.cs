using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetVectorGroupToVectorGroupFitGradientFactorOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_vector_group_to_vector_group_fit_gradient_factor",
        "Set Vector Group To Vector Group Fit Gradient Factor",
        "briosa.RelationshipOperations", "SetVectorGroupToVectorGroupFitGradientFactor",
        "/briosa.RelationshipOperations/SetVectorGroupToVectorGroupFitGradientFactor",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetVectorGroupToVectorGroupFitGradientFactorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.VgToVgRelationship, "vg_to_vg_relationship", WorkerItemTypeValue.Relationship);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("VG To VG Relationship", WorkerMpValueKind.CollectionItemName,
                    relationship, "SetCollectionObjectNameArg2"),
                new("Fit Gradient Factor", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasFitGradientFactor ? request.FitGradientFactor : 50.0), "SetDoubleArg")
            ], []);
    }

    public static Api.SetVectorGroupToVectorGroupFitGradientFactorResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
