using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class ImportGeometryFitProfilesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.import_geometry_fit_profiles", "Import Geometry Fit Profiles",
        "briosa.AnalysisOperations", "ImportGeometryFitProfiles", "/briosa.AnalysisOperations/ImportGeometryFitProfiles",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe,
        ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportGeometryFitProfilesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Geometry Fit Profiles File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.GeometryFitProfilesFilePath, "geometry_fit_profiles_file_path"), "SetFilePathArg"),
                new("Overwrite Profiles with Same Name?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasOverwriteProfilesWithSameName && request.OverwriteProfilesWithSameName), "SetBoolArg")
            ], []);
    }

    public static Api.ImportGeometryFitProfilesResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
