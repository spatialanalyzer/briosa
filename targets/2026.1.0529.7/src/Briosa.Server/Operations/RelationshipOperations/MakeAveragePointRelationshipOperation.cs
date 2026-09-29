using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakeAveragePointRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_average_point_relationship", "Make Average Point Relationship",
        "briosa.RelationshipOperations", "MakeAveragePointRelationship",
        "/briosa.RelationshipOperations/MakeAveragePointRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeAveragePointRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                relationship, "SetCollectionObjectNameArg2"),
            new("Points in Relationship", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.PointsInRelationship, "points_in_relationship"), "SetPointNameRefListArg")
        };
        if (request.AveragePointName is not null)
        {
            inputs.Add(new("Average Point Name (Optional)", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.AveragePointName, "average_point_name"), "SetPointNameArg"));
        }
        if (request.NominalPointName is not null)
        {
            inputs.Add(new("Nominal Point Name (Optional)", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.NominalPointName, "nominal_point_name"), "SetPointNameArg"));
        }

        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.MakeAveragePointRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
