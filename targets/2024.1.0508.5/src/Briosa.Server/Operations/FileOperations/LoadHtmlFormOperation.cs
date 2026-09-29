using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class LoadHtmlFormOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.load_html_form", "Load HTML Form", "briosa.FileOperations",
        "LoadHtmlForm", "/briosa.FileOperations/LoadHtmlForm", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.LoadHtmlFormRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Input HTML Form Path", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.InputHtmlFormPath, "input_html_form_path"), "SetFilePathArg"),
            new("Window Width", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasWindowWidth ? request.WindowWidth : 1000), "SetIntegerArg"),
            new("Window Height", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasWindowHeight ? request.WindowHeight : 800), "SetIntegerArg"),
            new("Input DataShare File Path", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.InputDataShareFilePath, "input_data_share_file_path"), "SetFilePathArg"),
            new("Output DataShare File Path", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.OutputDataShareFilePath, "output_data_share_file_path"), "SetFilePathArg"),
            new("Save in Binary Format?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.SaveInBinaryFormat), "SetBoolArg"),
            new("Save Button Text", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasSaveButtonText ? request.SaveButtonText : "Save"), "SetStringArg"),
            new("Cancel Button Text", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasCancelButtonText ? request.CancelButtonText : "Cancel"), "SetStringArg"),
            new("Hide Save and Cancel buttons?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HideSaveAndCancelButtons), "SetBoolArg")
        ], []);
    }

    public static Api.LoadHtmlFormResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
