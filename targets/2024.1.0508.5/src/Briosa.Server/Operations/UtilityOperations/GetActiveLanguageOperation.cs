using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class GetActiveLanguageOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.get_active_language", "Get Active Language", "briosa.UtilityOperations",
        "GetActiveLanguage", "/briosa.UtilityOperations/GetActiveLanguage", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("language_file_name", "Language File Name", WorkerMpValueKind.FileReference),
        new("custom_language", "Custom Language?", WorkerMpValueKind.Logical)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetActiveLanguageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [],
        [
            new("Language File Name", WorkerMpValueKind.FileReference, "GetFilePathArg"),
            new("Custom Language?", WorkerMpValueKind.Logical, "GetBoolArg")
        ]);
    }

    public static Api.GetActiveLanguageResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        var file = outputs[0].RequireValue<WorkerFileReferenceValue>();
        return new()
        {
            LanguageFileName = new Api.FileReference { Path = file.Path, EmbeddedFile = file.EmbeddedFile },
            CustomLanguage = outputs[1].RequireValue<WorkerBooleanValue>().Value,
            Execution = completed.Details
        };
    }
}
