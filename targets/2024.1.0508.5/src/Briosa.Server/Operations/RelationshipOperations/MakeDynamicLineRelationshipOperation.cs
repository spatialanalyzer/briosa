using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class MakeDynamicLineRelationshipOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.make_dynamic_line_relationship", "Make Dynamic Line Relationship",
        "briosa.RelationshipOperations", "MakeDynamicLineRelationship",
        "/briosa.RelationshipOperations/MakeDynamicLineRelationship",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeDynamicLineRelationshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("Construction Mode", WorkerMpValueKind.DynamicLineMode,
                    DynamicRelationshipModeMapper.Required(request.HasConstructionMode ? request.ConstructionMode : null, "construction_mode"), "SetDynamicLineModeArg"),
                new("First Reference Geometry", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.FirstReferenceGeometry, "first_reference_geometry"), "SetCollectionObjectNameArg2"),
                new("Second Reference Geometry", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.SecondReferenceGeometry, "second_reference_geometry"), "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.MakeDynamicLineRelationshipResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
