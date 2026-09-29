using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ProcessFlowOperations;

internal static class AskForUserDecisionFromStringsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "process_flow_operations.ask_for_user_decision_from_strings", "Ask for User Decision from Strings",
        "briosa.ProcessFlowOperations", "AskForUserDecisionFromStrings",
        "/briosa.ProcessFlowOperations/AskForUserDecisionFromStrings",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("answer", "Answer", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.AskForUserDecisionFromStringsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.QuestionOrStatement.Count == 0)
            throw new ArgumentException("Question or statement is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Question or Statement", WorkerMpValueKind.EditText,
                new WorkerStringListValue(request.QuestionOrStatement), "SetEditTextArg"),
            new("Font", WorkerMpValueKind.Font, FontMapper.WithDefaults(request.Font), "SetFontTypeArg"),
            new("Button1 Text (Empty to hide button)", WorkerMpValueKind.Text, new WorkerTextValue(request.Button1Text), "SetStringArg"),
            new("Button2 Text (Empty to hide button)", WorkerMpValueKind.Text, new WorkerTextValue(request.Button2Text), "SetStringArg"),
            new("Button3 Text (Empty to hide button)", WorkerMpValueKind.Text, new WorkerTextValue(request.Button3Text), "SetStringArg")
        ], [new("Answer", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.AskForUserDecisionFromStringsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Answer = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value,
            Execution = completed.Details };
}
