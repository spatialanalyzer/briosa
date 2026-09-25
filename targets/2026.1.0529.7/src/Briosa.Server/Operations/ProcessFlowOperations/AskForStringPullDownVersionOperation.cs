using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ProcessFlowOperations;

internal static class AskForStringPullDownVersionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "process_flow_operations.ask_for_string_pull_down_version", "Ask for String (Pull-Down Version)",
        "briosa.ProcessFlowOperations", "AskForStringPullDownVersion",
        "/briosa.ProcessFlowOperations/AskForStringPullDownVersion",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("answer", "Answer", WorkerMpValueKind.Text),
        new("answer_index", "Answer Index", WorkerMpValueKind.WholeNumber)
    ];

    public static WorkerMpCommand CreateCommand(Api.AskForStringPullDownVersionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.QuestionOrStatement.Count == 0 || request.PossibleAnswers.Count == 0)
            throw new ArgumentException("Question and possible answers are required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Question or Statement", WorkerMpValueKind.StringList,
                new WorkerStringListValue(request.QuestionOrStatement), "SetStringRefListArg"),
            new("Possible Answers", WorkerMpValueKind.StringList,
                new WorkerStringListValue(request.PossibleAnswers), "SetStringRefListArg"),
            new("Font", WorkerMpValueKind.Font, FontMapper.WithDefaults(request.Font), "SetFontTypeArg")
        ],
        [
            new("Answer", WorkerMpValueKind.Text, "GetStringArg"),
            new("Answer Index", WorkerMpValueKind.WholeNumber, "GetIntegerArg")
        ]);
    }

    public static Api.AskForStringPullDownVersionResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            Answer = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value,
            AnswerIndex = completed.Execution.OutputValues[1].RequireValue<WorkerIntegerValue>().Value,
            Execution = completed.Details
        };
}
