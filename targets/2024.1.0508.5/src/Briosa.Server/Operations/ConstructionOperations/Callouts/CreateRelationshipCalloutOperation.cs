using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class CreateRelationshipCalloutOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.create_relationship_callout", "Create Relationship Callout",
        "briosa.ConstructionOperations", "CreateRelationshipCallout", "/briosa.ConstructionOperations/CreateRelationshipCallout",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CreateRelationshipCalloutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Destination Callout View", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.DestinationCalloutView, "destination_callout_view"), "SetCollectionObjectNameArg2"),
            new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
            new("View X Position", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasViewXPosition ? request.ViewXPosition : 0), "SetDoubleArg"),
            new("View Y Position", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasViewYPosition ? request.ViewYPosition : 0), "SetDoubleArg"),
            new("Additional Notes (blank for none)", WorkerMpValueKind.EditText,
                new WorkerStringListValue(request.AdditionalNotes), "SetEditTextArg")
        ], []);
    }

    public static Api.CreateRelationshipCalloutResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
