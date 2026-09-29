using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetVectorGroupToVectorGroupFitWeightsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_vector_group_to_vector_group_fit_weights",
        "Set Vector Group To Vector Group Fit Weights",
        "briosa.RelationshipOperations", "SetVectorGroupToVectorGroupFitWeights",
        "/briosa.RelationshipOperations/SetVectorGroupToVectorGroupFitWeights",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetVectorGroupToVectorGroupFitWeightsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.VgToVgRelationship, "vg_to_vg_relationship", WorkerItemTypeValue.Relationship);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("VG To VG Relationship", WorkerMpValueKind.CollectionItemName,
                    relationship, "SetCollectionObjectNameArg2"),
                new("Minimum Gap", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasMinimumGap ? request.MinimumGap : 0.0), "SetDoubleArg"),
                new("Minimum Gap Fit Weight", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasMinimumGapFitWeight ? request.MinimumGapFitWeight : 10.0), "SetDoubleArg"),
                new("Maximum Gap", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasMaximumGap ? request.MaximumGap : 0.0), "SetDoubleArg"),
                new("Maximum Gap Fit Weight", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasMaximumGapFitWeight ? request.MaximumGapFitWeight : 10.0), "SetDoubleArg"),
                new("Nominal Gap", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasNominalGap ? request.NominalGap : 0.0), "SetDoubleArg"),
                new("Nominal Gap Fit Weight", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasNominalGapFitWeight ? request.NominalGapFitWeight : 1.0), "SetDoubleArg")
            ], []);
    }

    public static Api.SetVectorGroupToVectorGroupFitWeightsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
