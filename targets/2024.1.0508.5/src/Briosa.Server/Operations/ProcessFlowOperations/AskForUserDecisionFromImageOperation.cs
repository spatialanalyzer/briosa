using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ProcessFlowOperations;

internal static class AskForUserDecisionFromImageOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "process_flow_operations.ask_for_user_decision_from_image", "Ask for User Decision from Image",
        "briosa.ProcessFlowOperations", "AskForUserDecisionFromImage",
        "/briosa.ProcessFlowOperations/AskForUserDecisionFromImage",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("user_choice", "User Choice", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.AskForUserDecisionFromImageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Image File", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.ImageFile, "image_file"), "SetFilePathArg"),
            new("Image Map XML File", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.ImageMapXmlFile, "image_map_xml_file"), "SetFilePathArg"),
            new("Window Caption", WorkerMpValueKind.Text, new WorkerTextValue(request.WindowCaption), "SetStringArg"),
            new("Window Width (0 = default)", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.WindowWidth), "SetIntegerArg"),
            new("Window Height (0 = default)", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.WindowHeight), "SetIntegerArg")
        ], [new("User Choice", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.AskForUserDecisionFromImageResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { UserChoice = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value,
            Execution = completed.Details };
}
