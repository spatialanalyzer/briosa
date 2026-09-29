using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakePointsToPointsRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_points_to_points_relationship", "Make Points to Points Relationship",
        "briosa.RelationshipOperations", "MakePointsToPointsRelationship",
        "/briosa.RelationshipOperations/MakePointsToPointsRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakePointsToPointsRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("Nominal Points", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.NominalPoints, "nominal_points"), "SetPointNameRefListArg"),
                new("Measured Points", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.MeasuredPoints, "measured_points"), "SetPointNameRefListArg"),
                new("Auto Update a Vector Group?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasAutoUpdateAVectorGroup && request.AutoUpdateAVectorGroup), "SetBoolArg"),
                new("Tolerance", WorkerMpValueKind.ToleranceVectorOptions,
                    ToleranceVectorOptionsMapper.Required(request.Tolerance, "tolerance"), "SetToleranceVectorOptionsArg"),
                new("Constraint", WorkerMpValueKind.ToleranceVectorOptions,
                    ToleranceVectorOptionsMapper.Required(request.Constraint, "constraint"), "SetToleranceVectorOptionsArg")
            ], []);
    }

    public static Api.MakePointsToPointsRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
