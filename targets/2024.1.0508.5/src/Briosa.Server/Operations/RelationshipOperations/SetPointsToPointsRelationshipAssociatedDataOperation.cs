using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetPointsToPointsRelationshipAssociatedDataOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_points_to_points_relationship_associated_data",
        "Set Points to Points Relationship Associated Data",
        "briosa.RelationshipOperations", "SetPointsToPointsRelationshipAssociatedData",
        "/briosa.RelationshipOperations/SetPointsToPointsRelationshipAssociatedData",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetPointsToPointsRelationshipAssociatedDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship);
        var nominalPoints = request.NominalPoints?.Values ??
            throw RequestFieldViolation.Required("nominal_points");
        var actualPoints = request.ActualPoints?.Values ??
            throw RequestFieldViolation.Required("actual_points");
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    relationship, "SetCollectionObjectNameArg2"),
                new("Nominal Points", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(nominalPoints, "nominal_points.values"), "SetPointNameRefListArg"),
                new("Actual Points", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(actualPoints, "actual_points.values"), "SetPointNameRefListArg"),
                new("Ignore Empty Arguments?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasIgnoreEmptyArguments || request.IgnoreEmptyArguments), "SetBoolArg")
            ], []);
    }

    public static Api.SetPointsToPointsRelationshipAssociatedDataResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
