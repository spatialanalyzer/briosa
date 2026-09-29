using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class MakeGdtFeatureCheckAnnotationOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.make_gdt_feature_check_annotation", "Make GD&T Feature Check Annotation",
        "briosa.GdtOperations", "MakeGdtFeatureCheckAnnotation",
        "/briosa.GdtOperations/MakeGdtFeatureCheckAnnotation",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeGdtFeatureCheckAnnotationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var auxiliaryRelationship = CollectionItemNameMapper.Required(
            request.AuxiliaryGeometryRelationship, "auxiliary_geometry_relationship");
        if (auxiliaryRelationship.ItemType == WorkerItemTypeValue.Any)
            auxiliaryRelationship = auxiliaryRelationship with { ItemType = WorkerItemTypeValue.Relationship };

        var featureType = request.HasFeatureType ? request.FeatureType : Api.GdtFeatureType.TruePosition;
        var toleranceZoneType = request.HasToleranceZoneType
            ? request.ToleranceZoneType
            : Api.GdtToleranceZoneType.None;

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Feature Annotation Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.FeatureAnnotationName), "SetStringArg"),
            new("Feature Type", WorkerMpValueKind.Text,
                new WorkerTextValue(FeatureTypeName(featureType)), "SetStringArg"),
            new("Objects", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.Objects, "objects"), "SetCollectionObjectNameRefListArg"),
            new("Geometry Relationships", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.GeometryRelationships, "geometry_relationships"), "SetCollectionObjectNameRefListArg"),
            new("Surface Faces", WorkerMpValueKind.Text,
                new WorkerTextValue(request.SurfaceFaces?.Value ?? string.Empty), "SetStringArg"),
            new("Decompose Multiple Features?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.DecomposeMultipleFeatures), "SetBoolArg"),
            new("Auto Create Diameter Checks?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.AutoCreateDiameterChecks), "SetBoolArg"),
            new("Auto Create Slot Width Checks?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.AutoCreateSlotWidthChecks), "SetBoolArg"),
            new("Auto Create Slot Length Checks?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.AutoCreateSlotLengthChecks), "SetBoolArg"),
            new("Datum References", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasDatumReferences ? request.DatumReferences : string.Empty), "SetStringArg"),
            new("Tolerance", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasTolerance ? request.Tolerance : string.Empty), "SetStringArg"),
            new("Is Slot?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.IsSlot), "SetBoolArg"),
            new("Per unit length/area", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.PerUnitLengthOrArea), "SetBoolArg"),
            new("Circular area? (Rectangular default)", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.CircularArea), "SetBoolArg"),
            new("Per unit (area) length distance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasPerUnitLengthDistance ? request.PerUnitLengthDistance : 0), "SetDoubleArg"),
            new("Per unit (area) length step over %", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasPerUnitLengthStepOverPercent ? request.PerUnitLengthStepOverPercent : 50), "SetDoubleArg"),
            new("Per unit area width distance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasPerUnitAreaWidthDistance ? request.PerUnitAreaWidthDistance : 0), "SetDoubleArg"),
            new("Per unit area width step over %", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasPerUnitAreaWidthStepOverPercent ? request.PerUnitAreaWidthStepOverPercent : 50), "SetDoubleArg"),
            new("Per unit area circle diameter", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasPerUnitAreaCircleDiameter ? request.PerUnitAreaCircleDiameter : 0), "SetDoubleArg"),
            new("Per unit area diameter step over", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasPerUnitAreaDiameterStepOver ? request.PerUnitAreaDiameterStepOver : 50), "SetDoubleArg"),
            new("Auxiliary Object", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.AuxiliaryObject, "auxiliary_object"), "SetCollectionObjectNameArg2"),
            new("Auxiliary Geometry Relationship", WorkerMpValueKind.CollectionItemName,
                auxiliaryRelationship, "SetCollectionObjectNameArg2"),
            new("Use Nominal for Dimension Tolerance", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasUseNominalForDimensionTolerance
                    ? request.UseNominalForDimensionTolerance : true), "SetBoolArg"),
            new("Use Reference Object for Nominal", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasUseReferenceObjectForNominal
                    ? request.UseReferenceObjectForNominal : true), "SetBoolArg"),
            new("Nominal Dimension Tolerance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasNominalDimensionTolerance ? request.NominalDimensionTolerance : 0), "SetDoubleArg"),
            new("Low Dimension Tolerance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasLowDimensionTolerance ? request.LowDimensionTolerance : -0.1), "SetDoubleArg"),
            new("High Dimension Tolerance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasHighDimensionTolerance ? request.HighDimensionTolerance : 0.1), "SetDoubleArg"),
            new("Tolerance Zone Type", WorkerMpValueKind.Text,
                new WorkerTextValue(ToleranceZoneTypeName(toleranceZoneType)), "SetStringArg"),
            new("Use Projected Tolerance Zone?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.UseProjectedToleranceZone), "SetBoolArg"),
            new("Projected Tolerance Zone", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasProjectedToleranceZone ? request.ProjectedToleranceZone : 0), "SetDoubleArg")
        ], []);
    }

    private static string FeatureTypeName(Api.GdtFeatureType value) => value switch
    {
        Api.GdtFeatureType.Diameter => "Diameter",
        Api.GdtFeatureType.Radius => "Radius",
        Api.GdtFeatureType.DistanceBetween => "Distance Between",
        Api.GdtFeatureType.Width => "Width",
        Api.GdtFeatureType.Length => "Length",
        Api.GdtFeatureType.AngleBetween => "Angle Between",
        Api.GdtFeatureType.Angularity => "Angularity",
        Api.GdtFeatureType.Perpendicularity => "Perpendicularity",
        Api.GdtFeatureType.Parallelism => "Parallelism",
        Api.GdtFeatureType.Circularity => "Circularity",
        Api.GdtFeatureType.Concentricity => "Concentricity",
        Api.GdtFeatureType.Cylindricity => "Cylindricity",
        Api.GdtFeatureType.Straightness => "Straightness",
        Api.GdtFeatureType.SurfaceProfile => "Surface Profile",
        Api.GdtFeatureType.LineProfile => "Line Profile",
        Api.GdtFeatureType.CompositeSurfaceProfile => "Composite Surface Profile",
        Api.GdtFeatureType.Flatness => "Flatness",
        Api.GdtFeatureType.TruePosition => "True Position",
        Api.GdtFeatureType.CompositeTruePosition => "Composite True Position",
        Api.GdtFeatureType.CircularRunout => "Circular Runout",
        Api.GdtFeatureType.TotalRunout => "Total Runout",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Feature type is not supported by this SA target.")
    };

    private static string ToleranceZoneTypeName(Api.GdtToleranceZoneType value) => value switch
    {
        Api.GdtToleranceZoneType.None => "None",
        Api.GdtToleranceZoneType.Cylindrical => "Cylindrical",
        Api.GdtToleranceZoneType.Planar => "Planar",
        Api.GdtToleranceZoneType.Spherical => "Spherical",
        Api.GdtToleranceZoneType.RadialArc => "Radial Arc",
        Api.GdtToleranceZoneType.RadialPlanar => "Radial Planar",
        Api.GdtToleranceZoneType.Boundary => "Boundary",
        Api.GdtToleranceZoneType.PlanarMedian => "Planar Median",
        Api.GdtToleranceZoneType.Surface => "Surface",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Tolerance zone type is not supported by this SA target.")
    };

    public static Api.MakeGdtFeatureCheckAnnotationResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
