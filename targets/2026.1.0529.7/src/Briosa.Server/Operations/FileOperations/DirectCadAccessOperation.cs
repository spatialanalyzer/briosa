using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class DirectCadAccessOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.direct_cad_access", "Direct CAD Access", "briosa.FileOperations", "DirectCadAccess",
        "/briosa.FileOperations/DirectCadAccess", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("import_warnings", "Import Warnings", WorkerMpValueKind.Logical),
        new("import_warning_messages", "Import Warning Messages", WorkerMpValueKind.Text),
        new("extents_min", "Extents Min", WorkerMpValueKind.Vector),
        new("extents_max", "Extents Max", WorkerMpValueKind.Vector)
    ];

    public static WorkerMpCommand CreateCommand(Api.DirectCadAccessRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("CAD File Name", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.CadFileName, "cad_file_name"), "SetFilePathArg"),
            new("Import Solids", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasImportSolids || request.ImportSolids), "SetBoolArg"),
            new("Import Surfaces", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasImportSurfaces || request.ImportSurfaces), "SetBoolArg"),
            new("Import Polygonized Surfaces", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasImportPolygonizedSurfaces || request.ImportPolygonizedSurfaces), "SetBoolArg"),
            new("Import Annotations", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasImportAnnotations || request.ImportAnnotations), "SetBoolArg"),
            new("Import Vectors", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasImportVectors || request.ImportVectors), "SetBoolArg"),
            new("Import Points", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasImportPoints || request.ImportPoints), "SetBoolArg"),
            new("Point Group Name", WorkerMpValueKind.Text, new WorkerTextValue(request.HasPointGroupName ? request.PointGroupName : "CAD pts"), "SetStringArg"),
            new("Import Attributes/Metadata", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasImportAttributesMetadata || request.ImportAttributesMetadata), "SetBoolArg"),
            new("Import Cooordinate Frames", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasImportCooordinateFrames || request.ImportCooordinateFrames), "SetBoolArg"),
            new("Import Planes", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasImportPlanes || request.ImportPlanes), "SetBoolArg"),
            new("Import 3D Curves - Lines", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasImport3DCurvesLines || request.Import3DCurvesLines), "SetBoolArg"),
            new("Import 3D Curves - Circles", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasImport3DCurvesCircles || request.Import3DCurvesCircles), "SetBoolArg"),
            new("Import 3D Curves - General Curves", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasImport3DCurvesGeneralCurves || request.Import3DCurvesGeneralCurves), "SetBoolArg"),
            new("Import Construction Geometry", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasImportConstructionGeometry && request.ImportConstructionGeometry), "SetBoolArg"),
            new("Import Hidden Entities", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasImportHiddenEntities && request.ImportHiddenEntities), "SetBoolArg"),
            new("Import all Surfaces as Mesh Graphical Entities", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasImportAllSurfacesAsMeshGraphicalEntities && request.ImportAllSurfacesAsMeshGraphicalEntities), "SetBoolArg"),
            new("Do Not Import Fillets", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasDoNotImportFillets && request.DoNotImportFillets), "SetBoolArg"),
            new("Do Not Import Dittos", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasDoNotImportDittos && request.DoNotImportDittos), "SetBoolArg"),
            new("Ditto Threshold", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasDittoThreshold ? request.DittoThreshold : 1), "SetIntegerArg"),
            new("Center View on Imported Objects", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasCenterViewOnImportedObjects || request.CenterViewOnImportedObjects), "SetBoolArg"),
            new("Import into Folders matching CAD file hierarchy", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasImportIntoFoldersMatchingCadFileHierarchy && request.ImportIntoFoldersMatchingCadFileHierarchy), "SetBoolArg"),
            new("Remove Empty Folders", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasRemoveEmptyFolders || request.RemoveEmptyFolders), "SetBoolArg"),
            new("Surface Normals Mode (1 or 2)", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasSurfaceNormalsMode ? request.SurfaceNormalsMode : 1), "SetIntegerArg"),
            new("Prompt on Missing Components", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasPromptOnMissingComponents || request.PromptOnMissingComponents), "SetBoolArg"),
            new("Selective Import", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasSelectiveImport && request.SelectiveImport), "SetBoolArg"),
            new("Surface Compatibility Mode", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasSurfaceCompatibilityMode || request.SurfaceCompatibilityMode), "SetBoolArg"),
            new("Explode Surfaces", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasExplodeSurfaces && request.ExplodeSurfaces), "SetBoolArg"),
            new("CAD File Units (leave blank to use the units specified in the file)", WorkerMpValueKind.Text, new WorkerTextValue(request.HasCadFileUnits ? request.CadFileUnits : string.Empty), "SetStringArg"),
            new("Build Callout Views", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasBuildCalloutViews || request.BuildCalloutViews), "SetBoolArg")
        ],
        [
            new("Import Warnings", WorkerMpValueKind.Logical, "GetBoolArg"),
            new("Import Warning Messages", WorkerMpValueKind.Text, "GetStringArg"),
            new("Extents Min", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("Extents Max", WorkerMpValueKind.Vector, "GetVectorArg")
        ]);
    }

    public static Api.DirectCadAccessResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        ImportWarnings = completed.Execution.OutputValues[0].RequireValue<WorkerBooleanValue>().Value,
        ImportWarningMessages = completed.Execution.OutputValues[1].RequireValue<WorkerTextValue>().Value,
        ExtentsMin = VectorMapper.ToProtocol(completed.Execution.OutputValues[2].RequireValue<WorkerVectorValue>()),
        ExtentsMax = VectorMapper.ToProtocol(completed.Execution.OutputValues[3].RequireValue<WorkerVectorValue>()),
        Execution = completed.Details
    };
}
