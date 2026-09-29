using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakeGeometryFitOnlyRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_geometry_fit_only_relationship", "Make Geometry Fit Only Relationship",
        "briosa.RelationshipOperations", "MakeGeometryFitOnlyRelationship",
        "/briosa.RelationshipOperations/MakeGeometryFitOnlyRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeGeometryFitOnlyRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.HasGeometryType)
            throw new ArgumentException("Request field 'geometry_type' is required.", nameof(request));
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
            new("Point Groups to Fit", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.PointGroupsToFit, "point_groups_to_fit"), "SetCollectionObjectNameRefListArg"),
            new("Geometry Type", WorkerMpValueKind.GeometryType,
                GeometryTypeMapper.Required(request.GeometryType, "geometry_type"), "SetGeometryTypeArg")
        };
        if (request.ResultingObjectName is not null)
            inputs.Add(new("Resulting Object Name (Optional)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingObjectName, "resulting_object_name"), "SetCollectionObjectNameArg2"));
        if (request.HasFitProfileName)
            inputs.Add(new("Fit Profile Name (Optional)", WorkerMpValueKind.Text,
                new WorkerTextValue(request.FitProfileName), "SetStringArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.MakeGeometryFitOnlyRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
