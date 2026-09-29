using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetVectorGroupToVectorGroupCylindricalZoneOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_vector_group_to_vector_group_cylindrical_zone",
        "Set Vector Group To Vector Group Cylindrical Zone",
        "briosa.RelationshipOperations", "SetVectorGroupToVectorGroupCylindricalZone",
        "/briosa.RelationshipOperations/SetVectorGroupToVectorGroupCylindricalZone",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetVectorGroupToVectorGroupCylindricalZoneRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.VgToVgRelationship, "vg_to_vg_relationship", WorkerItemTypeValue.Relationship);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("VG To VG Relationship", WorkerMpValueKind.CollectionItemName,
                    relationship, "SetCollectionObjectNameArg2"),
                new("Radial Offset", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasRadialOffset ? request.RadialOffset : 1.0), "SetDoubleArg"),
                new("Minimum Axial Offset", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasMinimumAxialOffset ? request.MinimumAxialOffset : -10.0), "SetDoubleArg"),
                new("Maximum Axial Offset", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasMaximumAxialOffset ? request.MaximumAxialOffset : 10.0), "SetDoubleArg")
            ], []);
    }

    public static Api.SetVectorGroupToVectorGroupCylindricalZoneResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
