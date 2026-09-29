using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGdtFeatureCheckAnnotationTests
{
    private static readonly int[] DefaultBooleanArgumentIndexes = [5, 6, 7, 8, 11, 12, 13];
    private static readonly int[] DefaultDoubleArgumentIndexes = [14, 15, 16, 17, 18, 19, 24, 25, 26, 29];

    [Fact]
    public void MapsTypedArgumentsAndChoiceLabels()
    {
        var request = new Api.MakeGdtFeatureCheckAnnotationRequest
        {
            FeatureAnnotationName = "FC-1",
            FeatureType = Api.GdtFeatureType.AngleBetween,
            DatumReferences = "A|B",
            Tolerance = "0.25",
            IsSlot = true,
            PerUnitLengthOrArea = true,
            CircularArea = true,
            PerUnitLengthDistance = 1.1,
            PerUnitLengthStepOverPercent = 2.2,
            PerUnitAreaWidthDistance = 3.3,
            PerUnitAreaWidthStepOverPercent = 4.4,
            PerUnitAreaCircleDiameter = 5.5,
            PerUnitAreaDiameterStepOver = 6.6,
            AuxiliaryObject = new() { CollectionName = "C", ObjectName = "Aux" },
            AuxiliaryGeometryRelationship = new()
                { CollectionName = "C", ItemName = "AuxRel", ItemType = Api.ItemType.Relationship },
            UseNominalForDimensionTolerance = false,
            UseReferenceObjectForNominal = false,
            NominalDimensionTolerance = 7.7,
            LowDimensionTolerance = -8.8,
            HighDimensionTolerance = 9.9,
            ToleranceZoneType = Api.GdtToleranceZoneType.RadialArc,
            UseProjectedToleranceZone = true,
            ProjectedToleranceZone = 10.1
        };
        request.Objects.Add(new Api.CollectionObjectName { CollectionName = "C", ObjectName = "Part" });
        request.GeometryRelationships.Add(new Api.CollectionItemName
            { CollectionName = "C", ItemName = "Rel", ItemType = Api.ItemType.Relationship });

        var command = MakeGdtFeatureCheckAnnotationOperation.CreateCommand(request);

        Assert.Equal(30, command.InputArguments.Count);
        Assert.Equal(
            ["Feature Annotation Name", "Feature Type", "Objects", "Geometry Relationships", "Surface Faces",
                "Decompose Multiple Features?", "Auto Create Diameter Checks?", "Auto Create Slot Width Checks?",
                "Auto Create Slot Length Checks?", "Datum References", "Tolerance", "Is Slot?",
                "Per unit length/area", "Circular area? (Rectangular default)", "Per unit (area) length distance",
                "Per unit (area) length step over %", "Per unit area width distance", "Per unit area width step over %",
                "Per unit area circle diameter", "Per unit area diameter step over", "Auxiliary Object",
                "Auxiliary Geometry Relationship", "Use Nominal for Dimension Tolerance",
                "Use Reference Object for Nominal", "Nominal Dimension Tolerance", "Low Dimension Tolerance",
                "High Dimension Tolerance", "Tolerance Zone Type", "Use Projected Tolerance Zone?",
                "Projected Tolerance Zone"], command.InputArguments.Select(x => x.Name));
        Assert.Equal(
            ["SetStringArg", "SetStringArg", "SetCollectionObjectNameRefListArg", "SetCollectionObjectNameRefListArg",
                "SetStringArg", "SetBoolArg", "SetBoolArg", "SetBoolArg", "SetBoolArg", "SetStringArg", "SetStringArg",
                "SetBoolArg", "SetBoolArg", "SetBoolArg", "SetDoubleArg", "SetDoubleArg", "SetDoubleArg", "SetDoubleArg",
                "SetDoubleArg", "SetDoubleArg", "SetCollectionObjectNameArg2", "SetCollectionObjectNameArg2", "SetBoolArg",
                "SetBoolArg", "SetDoubleArg", "SetDoubleArg", "SetDoubleArg", "SetStringArg", "SetBoolArg", "SetDoubleArg"],
            command.InputArguments.Select(x => x.SdkBinding));

        Assert.Equal("FC-1", Text(command, 0));
        Assert.Equal("Angle Between", Text(command, 1));
        Assert.Equal("Part", command.InputArguments[2].RequireValue<WorkerCollectionObjectNameListValue>().Values.Single().ObjectName);
        Assert.Equal("Rel", command.InputArguments[3].RequireValue<WorkerCollectionItemNameListValue>().Values.Single().ItemName);
        Assert.Equal("", Text(command, 4));
        Assert.Equal([false, false, false, false], Enumerable.Range(5, 4)
            .Select(i => command.InputArguments[i].RequireValue<WorkerBooleanValue>().Value));
        Assert.Equal("A|B", Text(command, 9));
        Assert.Equal("0.25", Text(command, 10));
        Assert.Equal([true, true, true], Enumerable.Range(11, 3)
            .Select(i => command.InputArguments[i].RequireValue<WorkerBooleanValue>().Value));
        Assert.Equal([1.1, 2.2, 3.3, 4.4, 5.5, 6.6], Enumerable.Range(14, 6)
            .Select(i => command.InputArguments[i].RequireValue<WorkerDoubleValue>().Value));
        Assert.Equal("Aux", command.InputArguments[20].RequireValue<WorkerCollectionObjectNameValue>().ObjectName);
        Assert.Equal(WorkerItemTypeValue.Relationship,
            command.InputArguments[21].RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.Equal([false, false], Enumerable.Range(22, 2)
            .Select(i => command.InputArguments[i].RequireValue<WorkerBooleanValue>().Value));
        Assert.Equal([7.7, -8.8, 9.9], Enumerable.Range(24, 3)
            .Select(i => command.InputArguments[i].RequireValue<WorkerDoubleValue>().Value));
        Assert.Equal("Radial Arc", Text(command, 27));
        Assert.True(command.InputArguments[28].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(10.1, command.InputArguments[29].RequireValue<WorkerDoubleValue>().Value);
    }

    [Fact]
    public void AppliesCatalogDefaultsAndRejectsUnspecifiedChoices()
    {
        var request = new Api.MakeGdtFeatureCheckAnnotationRequest
        {
            AuxiliaryObject = new() { CollectionName = "C", ObjectName = "Aux" },
            AuxiliaryGeometryRelationship = new() { CollectionName = "C", ItemName = "AuxRel" }
        };
        request.Objects.Add(new Api.CollectionObjectName { CollectionName = "C", ObjectName = "Part" });
        request.GeometryRelationships.Add(new Api.CollectionItemName { CollectionName = "C", ItemName = "Rel" });

        var command = MakeGdtFeatureCheckAnnotationOperation.CreateCommand(request);

        Assert.Equal("True Position", Text(command, 1));
        Assert.Equal([false, false, false, false, false, false, false], DefaultBooleanArgumentIndexes
            .Select(i => command.InputArguments[i].RequireValue<WorkerBooleanValue>().Value));
        Assert.Equal([0d, 50d, 0d, 50d, 0d, 50d, 0d, -0.1d, 0.1d, 0d],
            DefaultDoubleArgumentIndexes
                .Select(i => command.InputArguments[i].RequireValue<WorkerDoubleValue>().Value));
        Assert.Equal("None", Text(command, 27));
        Assert.Equal(WorkerItemTypeValue.Relationship,
            command.InputArguments[21].RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.Throws<ArgumentOutOfRangeException>(() => MakeGdtFeatureCheckAnnotationOperation.CreateCommand(
            With(request, new() { FeatureType = Api.GdtFeatureType.Unspecified })));
        Assert.Throws<ArgumentOutOfRangeException>(() => MakeGdtFeatureCheckAnnotationOperation.CreateCommand(
            With(request, new() { ToleranceZoneType = Api.GdtToleranceZoneType.Unspecified })));
        Assert.Throws<ArgumentException>(() => MakeGdtFeatureCheckAnnotationOperation.CreateCommand(new()));
        var id = MakeGdtFeatureCheckAnnotationOperation.Descriptor.OperationId;
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
    }

    private static string Text(WorkerMpCommand command, int index) =>
        command.InputArguments[index].RequireValue<WorkerTextValue>().Value;

    private static Api.MakeGdtFeatureCheckAnnotationRequest With(
        Api.MakeGdtFeatureCheckAnnotationRequest source, Api.MakeGdtFeatureCheckAnnotationRequest choices) => new()
        {
            AuxiliaryObject = source.AuxiliaryObject,
            AuxiliaryGeometryRelationship = source.AuxiliaryGeometryRelationship,
            FeatureType = choices.FeatureType,
            ToleranceZoneType = choices.ToleranceZoneType
        };
}
