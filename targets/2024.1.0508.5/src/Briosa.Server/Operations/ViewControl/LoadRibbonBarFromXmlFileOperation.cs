using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class LoadRibbonBarFromXmlFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.load_ribbon_bar_from_xml_file", "Load Ribbon Bar from XML File", "briosa.ViewControl",
        "LoadRibbonBarFromXmlFile", "/briosa.ViewControl/LoadRibbonBarFromXmlFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.LoadRibbonBarFromXmlFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("File Path", WorkerMpValueKind.FileReference,
            FileReferenceMapper.Required(request.FilePath, "file_path"), "SetFilePathArg")], []);
    }

    public static Api.LoadRibbonBarFromXmlFileResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
