using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class MakeGdtDatumAnnotationOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.make_gdt_datum_annotation", "Make GD&T Datum Annotation",
        "briosa.GdtOperations", "MakeGdtDatumAnnotation", "/briosa.GdtOperations/MakeGdtDatumAnnotation",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeGdtDatumAnnotationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var auxiliaryRelationship = CollectionItemNameMapper.Required(
            request.AuxiliaryGeometryRelationship, "auxiliary_geometry_relationship");
        if (auxiliaryRelationship.ItemType == WorkerItemTypeValue.Any)
            auxiliaryRelationship = auxiliaryRelationship with { ItemType = WorkerItemTypeValue.Relationship };

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Datum Name", WorkerMpValueKind.Text, new WorkerTextValue(request.DatumName), "SetStringArg"),
            new("Objects", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.Objects, "objects"), "SetCollectionObjectNameRefListArg"),
            new("Geometry Relationships", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.GeometryRelationships, "geometry_relationships"), "SetCollectionObjectNameRefListArg"),
            new("Surface Faces", WorkerMpValueKind.Text,
                new WorkerTextValue(request.SurfaceFaces?.Value ?? string.Empty), "SetStringArg"),
            new("Auxiliary Object", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.AuxiliaryObject, "auxiliary_object"), "SetCollectionObjectNameArg2"),
            new("Auxiliary Geometry Relationship", WorkerMpValueKind.CollectionItemName,
                auxiliaryRelationship, "SetCollectionObjectNameArg2"),
            new("Is Slot?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.IsSlot), "SetBoolArg"),
            new("Force Surface Feature?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ForceSurfaceFeature), "SetBoolArg")
        ], []);
    }

    public static Api.MakeGdtDatumAnnotationResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
