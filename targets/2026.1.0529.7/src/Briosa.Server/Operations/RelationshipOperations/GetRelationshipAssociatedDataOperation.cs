using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetRelationshipAssociatedDataOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_relationship_associated_data", "Get Relationship Associated Data",
        "briosa.RelationshipOperations", "GetRelationshipAssociatedData",
        "/briosa.RelationshipOperations/GetRelationshipAssociatedData",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("relationship_type", "Relationship Type", WorkerMpValueKind.Text),
        new("individual_points", "Individual Points", WorkerMpValueKind.PointNameList),
        new("point_groups", "Point Groups", WorkerMpValueKind.CollectionObjectNameList),
        new("point_clouds", "Point Clouds", WorkerMpValueKind.CollectionObjectNameList),
        new("objects", "Objects", WorkerMpValueKind.CollectionObjectNameList)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetRelationshipAssociatedDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                relationship, "SetCollectionObjectNameArg2")],
            [
                new("Relationship Type", WorkerMpValueKind.Text, "GetStringArg"),
                new("Individual Points", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg"),
                new("Point Groups", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg"),
                new("Point Clouds", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg"),
                new("Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")
            ]);
    }

    public static Api.GetRelationshipAssociatedDataResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        var data = new Api.RelationshipAssociatedData
        {
            RelationshipType = outputs[0].RequireValue<WorkerTextValue>().Value
        };
        AddPoints(data.IndividualPoints, outputs[1]);
        AddObjects(data.PointGroups, outputs[2]);
        AddObjects(data.PointClouds, outputs[3]);
        AddObjects(data.Objects, outputs[4]);
        return new() { AssociatedData = data, Execution = completed.Details };
    }

    private static void AddPoints(
        Google.Protobuf.Collections.RepeatedField<Api.PointName> destination,
        WorkerMpOutputValue output)
    {
        foreach (var point in output.RequireValue<WorkerPointNameListValue>().Values)
            destination.Add(PointNameMapper.ToProtocol(point));
    }

    private static void AddObjects(
        Google.Protobuf.Collections.RepeatedField<Api.CollectionObjectName> destination,
        WorkerMpOutputValue output)
    {
        foreach (var item in output.RequireValue<WorkerCollectionObjectNameListValue>().Values)
            destination.Add(CollectionObjectNameMapper.ToProtocol(item));
    }
}
