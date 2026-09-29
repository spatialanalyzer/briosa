using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetRelationshipAssociatedDataOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_relationship_associated_data", "Set Relationship Associated Data",
        "briosa.RelationshipOperations", "SetRelationshipAssociatedData",
        "/briosa.RelationshipOperations/SetRelationshipAssociatedData",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipAssociatedDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship);
        var individualPoints = request.IndividualPoints?.Values ??
            throw new ArgumentException("Request field 'individual_points' is required.", nameof(request));
        var pointGroups = request.PointGroups?.Values ??
            throw new ArgumentException("Request field 'point_groups' is required.", nameof(request));
        var pointClouds = request.PointClouds?.Values ??
            throw new ArgumentException("Request field 'point_clouds' is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    relationship, "SetCollectionObjectNameArg2"),
                new("Individual Points", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(individualPoints, "individual_points"), "SetPointNameRefListArg"),
                new("Point Groups", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(pointGroups, "point_groups"), "SetCollectionObjectNameRefListArg"),
                new("Point Clouds", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(pointClouds, "point_clouds"), "SetCollectionObjectNameRefListArg"),
                new("Objects", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.Objects?.Values ??
                        throw new ArgumentException("Request field 'objects' is required.", nameof(request)), "objects"),
                    "SetCollectionObjectNameRefListArg"),
                new("Ignore Empty Arguments?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasIgnoreEmptyArguments || request.IgnoreEmptyArguments), "SetBoolArg")
            ], []);
    }

    public static Api.SetRelationshipAssociatedDataResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
