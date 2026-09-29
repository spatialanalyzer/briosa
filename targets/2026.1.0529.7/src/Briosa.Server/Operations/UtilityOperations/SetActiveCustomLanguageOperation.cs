using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetActiveCustomLanguageOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_active_custom_language", "Set Active Custom Language", "briosa.UtilityOperations",
        "SetActiveCustomLanguage", "/briosa.UtilityOperations/SetActiveCustomLanguage", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetActiveCustomLanguageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Language File Name", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.LanguageFileName, "language_file_name"), "SetFilePathArg"),
            new("Font", WorkerMpValueKind.Font, FontMapper.WithDefaults(request.Font), "SetFontTypeArg")
        ], []);
    }

    public static Api.SetActiveCustomLanguageResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
