using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ProcessFlowOperations;

internal static class AskForStringOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "process_flow_operations.ask_for_string", "Ask for String",
        "briosa.ProcessFlowOperations", "AskForString", "/briosa.ProcessFlowOperations/AskForString",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("answer", "Answer", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.AskForStringRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Question to ask", WorkerMpValueKind.Text, new WorkerTextValue(request.QuestionToAsk), "SetStringArg"),
            new("Password Entry?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.PasswordEntry), "SetBoolArg"),
            new("Initial Answer", WorkerMpValueKind.Text, new WorkerTextValue(request.InitialAnswer), "SetStringArg"),
            new("Font", WorkerMpValueKind.Font, FontMapper.WithDefaults(request.Font), "SetFontTypeArg")
        ], [new("Answer", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.AskForStringResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Answer = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value,
            Execution = completed.Details };
}
