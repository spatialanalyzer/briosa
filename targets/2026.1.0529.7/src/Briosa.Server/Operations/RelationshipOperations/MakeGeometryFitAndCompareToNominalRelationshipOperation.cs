using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakeGeometryFitAndCompareToNominalRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_geometry_fit_and_compare_to_nominal_relationship",
        "Make Geometry Fit and Compare to Nominal Relationship",
        "briosa.RelationshipOperations", "MakeGeometryFitAndCompareToNominalRelationship",
        "/briosa.RelationshipOperations/MakeGeometryFitAndCompareToNominalRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeGeometryFitAndCompareToNominalRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
            new("Nominal Geometry", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.NominalGeometry, "nominal_geometry"), "SetCollectionObjectNameArg2"),
            new("Point Groups to Fit", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.PointGroupsToFit, "point_groups_to_fit"), "SetCollectionObjectNameRefListArg")
        };
        if (request.ResultingObjectName is not null)
            inputs.Add(new("Resulting Object Name (Optional)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingObjectName, "resulting_object_name"), "SetCollectionObjectNameArg2"));
        if (request.HasFitProfileName)
            inputs.Add(new("Fit Profile Name (Optional)", WorkerMpValueKind.Text,
                new WorkerTextValue(request.FitProfileName), "SetStringArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.MakeGeometryFitAndCompareToNominalRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
