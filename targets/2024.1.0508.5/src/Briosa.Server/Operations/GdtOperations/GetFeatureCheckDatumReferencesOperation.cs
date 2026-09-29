using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class GetFeatureCheckDatumReferencesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.get_feature_check_datum_references", "Get Feature Check Datum References",
        "briosa.GdtOperations", "GetFeatureCheckDatumReferences", "/briosa.GdtOperations/GetFeatureCheckDatumReferences",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        .. DatumContracts("datum_1", "Datum 1"),
        .. DatumContracts("datum_2", "Datum 2"),
        .. DatumContracts("datum_3", "Datum 3")
    ];

    public static WorkerMpCommand CreateCommand(Api.GetFeatureCheckDatumReferencesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var featureCheck = CollectionItemNameMapper.Required(request.FeatureCheck, "feature_check");
        if (featureCheck.ItemType == WorkerItemTypeValue.Any)
            featureCheck = featureCheck with { ItemType = WorkerItemTypeValue.FeatureCheck };
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Feature Check", WorkerMpValueKind.CollectionItemName, featureCheck, "SetCollectionObjectNameArg2")],
            [
                new("Datum 1 Reference String", WorkerMpValueKind.Text, "GetStringArg"),
                new("Datum 1 CAD Faces", WorkerMpValueKind.Text, "GetStringArg"),
                new("Datum 1 SA Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg"),
                new("Datum 1 Aux SA Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg"),
                new("Datum 1 Geometry Relationships", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg"),
                new("Datum 1 Aux Geometry Relationships", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg"),
                new("Datum 2 Reference String", WorkerMpValueKind.Text, "GetStringArg"),
                new("Datum 2 CAD Faces", WorkerMpValueKind.Text, "GetStringArg"),
                new("Datum 2 SA Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg"),
                new("Datum 2 Aux SA Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg"),
                new("Datum 2 Geometry Relationships", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg"),
                new("Datum 2 Aux Geometry Relationships", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg"),
                new("Datum 3 Reference String", WorkerMpValueKind.Text, "GetStringArg"),
                new("Datum 3 CAD Faces", WorkerMpValueKind.Text, "GetStringArg"),
                new("Datum 3 SA Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg"),
                new("Datum 3 Aux SA Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg"),
                new("Datum 3 Geometry Relationships", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg"),
                new("Datum 3 Aux Geometry Relationships", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg")
            ]);
    }

    public static Api.GetFeatureCheckDatumReferencesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Datum1 = CreateDatum(values, 0),
            Datum2 = CreateDatum(values, 6),
            Datum3 = CreateDatum(values, 12),
            Execution = completed.Details
        };
    }

    private static Api.FeatureCheckDatumReference CreateDatum(IReadOnlyList<WorkerMpOutputValue> values, int start)
    {
        var datum = new Api.FeatureCheckDatumReference
        {
            ReferenceString = values[start].RequireValue<WorkerTextValue>().Value,
            CadFaces = values[start + 1].RequireValue<WorkerTextValue>().Value
        };
        foreach (var value in values[start + 2].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            datum.SaObjects.Add(CollectionObjectNameMapper.ToProtocol(value));
        foreach (var value in values[start + 3].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            datum.AuxiliarySaObjects.Add(CollectionObjectNameMapper.ToProtocol(value));
        foreach (var value in values[start + 4].RequireValue<WorkerCollectionItemNameListValue>().Values)
            datum.GeometryRelationships.Add(CollectionItemNameMapper.ToProtocol(value));
        foreach (var value in values[start + 5].RequireValue<WorkerCollectionItemNameListValue>().Values)
            datum.AuxiliaryGeometryRelationships.Add(CollectionItemNameMapper.ToProtocol(value));
        return datum;
    }

    private static OperationOutputContract[] DatumContracts(string prefix, string label) =>
    [
        new($"{prefix}.reference_string", $"{label} Reference String", WorkerMpValueKind.Text),
        new($"{prefix}.cad_faces", $"{label} CAD Faces", WorkerMpValueKind.Text),
        new($"{prefix}.sa_objects", $"{label} SA Objects", WorkerMpValueKind.CollectionObjectNameList),
        new($"{prefix}.auxiliary_sa_objects", $"{label} Aux SA Objects", WorkerMpValueKind.CollectionObjectNameList),
        new($"{prefix}.geometry_relationships", $"{label} Geometry Relationships", WorkerMpValueKind.CollectionItemNameList),
        new($"{prefix}.auxiliary_geometry_relationships", $"{label} Aux Geometry Relationships", WorkerMpValueKind.CollectionItemNameList)
    ];
}
