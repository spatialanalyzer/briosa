using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ProcessFlowOperations;

internal static class AskForIntegerOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "process_flow_operations.ask_for_integer", "Ask for Integer",
        "briosa.ProcessFlowOperations", "AskForInteger", "/briosa.ProcessFlowOperations/AskForInteger",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("answer", "Answer", WorkerMpValueKind.WholeNumber)];

    public static WorkerMpCommand CreateCommand(Api.AskForIntegerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Question to ask", WorkerMpValueKind.Text, new WorkerTextValue(request.QuestionToAsk), "SetStringArg"),
            new("Initial Value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.InitialValue), "SetIntegerArg"),
            new("Enforce Min/Max Values?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.EnforceMinMaxValues), "SetBoolArg"),
            new("Min Value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.MinValue), "SetIntegerArg"),
            new("Max Value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.MaxValue), "SetIntegerArg"),
            new("Font", WorkerMpValueKind.Font, FontMapper.WithDefaults(request.Font), "SetFontTypeArg")
        ], [new("Answer", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }

    public static Api.AskForIntegerResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Answer = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
            Execution = completed.Details };
}
