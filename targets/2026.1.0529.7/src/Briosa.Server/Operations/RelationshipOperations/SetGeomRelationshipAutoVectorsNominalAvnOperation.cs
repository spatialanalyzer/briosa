using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetGeomRelationshipAutoVectorsNominalAvnOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_geom_relationship_auto_vectors_nominal_avn", "Set Geom Relationship Auto Vectors Nominal (AVN)",
        "briosa.RelationshipOperations", "SetGeomRelationshipAutoVectorsNominalAvn",
        "/briosa.RelationshipOperations/SetGeomRelationshipAutoVectorsNominalAvn",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetGeomRelationshipAutoVectorsNominalAvnRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var pointsType = request.HasPointsType
            ? request.PointsType
            : Api.PointFilterInputType.CardinalPoints;
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Create Auto Vectors AVN", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.CreateAutoVectorsAvn), "SetBoolArg"),
                new("Points Type", WorkerMpValueKind.PointFilterInputType,
                    new WorkerChoiceValue<WorkerPointFilterInputTypeValue>(ToWorker(pointsType)), "SetPointFilterInputTypeArg"),
                new("Use Vector Group Custom Prefix?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.UseVectorGroupCustomPrefix), "SetBoolArg"),
                new("Vector Group Custom Prefix", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasVectorGroupCustomPrefix ? request.VectorGroupCustomPrefix : string.Empty), "SetStringArg")
            ], []);
    }

    public static Api.SetGeomRelationshipAutoVectorsNominalAvnResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };

    private static WorkerPointFilterInputTypeValue ToWorker(Api.PointFilterInputType value) => value switch
    {
        Api.PointFilterInputType.CardinalPoints => WorkerPointFilterInputTypeValue.CardinalPoints,
        Api.PointFilterInputType.InputPoints => WorkerPointFilterInputTypeValue.InputPoints,
        Api.PointFilterInputType.NominalCardinalPoints => WorkerPointFilterInputTypeValue.NominalCardinalPoints,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Point filter input type is not supported.")
    };
}
