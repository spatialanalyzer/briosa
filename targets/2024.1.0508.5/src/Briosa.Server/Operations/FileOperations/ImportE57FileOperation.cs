using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportE57FileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_e57_file", "Import E57 File", "briosa.FileOperations",
        "ImportE57File", "/briosa.FileOperations/ImportE57File", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportE57FileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("E57 File Path", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.E57FilePath, "e57_file_path"), "SetFilePathArg"),
            new("Save Converted File", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasSaveConvertedFile && request.SaveConvertedFile), "SetBoolArg"),
            new("Use Square Root of Intensity", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasUseSquareRootOfIntensity || request.UseSquareRootOfIntensity), "SetBoolArg"),
            new("Automatically Close Converter", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasAutomaticallyCloseConverter || request.AutomaticallyCloseConverter), "SetBoolArg"),
            new("Prioritize Color Over Intensity", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasPrioritizeColorOverIntensity || request.PrioritizeColorOverIntensity), "SetBoolArg"),
            new("Import Scan Blocks As Separate Clouds", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasImportScanBlocksAsSeparateClouds && request.ImportScanBlocksAsSeparateClouds), "SetBoolArg"),
            new("Units", WorkerMpValueKind.DistanceUnit, DistanceUnitMapper.OrDefault(
                request.HasUnits ? request.Units : null, Api.DistanceUnits.Inches), "SetDistanceUnitsArg")
        ], []);
    }

    public static Api.ImportE57FileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
