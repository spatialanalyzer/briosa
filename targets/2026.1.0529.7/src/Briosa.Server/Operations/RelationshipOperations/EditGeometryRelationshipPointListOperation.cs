using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class EditGeometryRelationshipPointListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.edit_geometry_relationship_point_list", "Edit Geometry Relationship Point List",
        "briosa.RelationshipOperations", "EditGeometryRelationshipPointList",
        "/briosa.RelationshipOperations/EditGeometryRelationshipPointList",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.EditGeometryRelationshipPointListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship);
        var mode = request.HasPointEditMode
            ? ToStepValue(request.PointEditMode)
            : "Point List";
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    relationship, "SetCollectionObjectNameArg2"),
                new("Point Edit Mode", WorkerMpValueKind.Text,
                    new WorkerTextValue(mode), "SetStringArg")
            ], []);
    }

    private static string ToStepValue(Api.GeometryRelationshipPointEditMode mode) => mode switch
    {
        Api.GeometryRelationshipPointEditMode.PointList => "Point List",
        Api.GeometryRelationshipPointEditMode.PointGraph => "Point Graph",
        Api.GeometryRelationshipPointEditMode.SubSamplerSettings => "Sub-Sampler Settings",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Point edit mode is not supported by this SA target.")
    };

    public static Api.EditGeometryRelationshipPointListResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
