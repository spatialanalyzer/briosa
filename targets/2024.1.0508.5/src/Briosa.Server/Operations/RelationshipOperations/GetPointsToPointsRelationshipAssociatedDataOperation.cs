using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetPointsToPointsRelationshipAssociatedDataOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_points_to_points_relationship_associated_data",
        "Get Points to Points Relationship Associated Data",
        "briosa.RelationshipOperations", "GetPointsToPointsRelationshipAssociatedData",
        "/briosa.RelationshipOperations/GetPointsToPointsRelationshipAssociatedData",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("nominal_points", "Nominal Points", WorkerMpValueKind.PointNameList),
        new("actual_points", "Actual Points", WorkerMpValueKind.PointNameList)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetPointsToPointsRelationshipAssociatedDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                relationship, "SetCollectionObjectNameArg2")],
            [
                new("Nominal Points", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg"),
                new("Actual Points", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg")
            ]);
    }

    public static Api.GetPointsToPointsRelationshipAssociatedDataResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        var result = new Api.GetPointsToPointsRelationshipAssociatedDataResult
        {
            AssociatedData = new(),
            Execution = completed.Details
        };
        AddPoints(result.AssociatedData.NominalPoints, outputs[0]);
        AddPoints(result.AssociatedData.ActualPoints, outputs[1]);
        return result;
    }

    private static void AddPoints(
        Google.Protobuf.Collections.RepeatedField<Api.PointName> destination,
        WorkerMpOutputValue output)
    {
        foreach (var point in output.RequireValue<WorkerPointNameListValue>().Values)
        {
            destination.Add(PointNameMapper.ToProtocol(point));
        }
    }
}
