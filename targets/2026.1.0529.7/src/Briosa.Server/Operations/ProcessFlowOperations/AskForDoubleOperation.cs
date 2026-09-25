using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ProcessFlowOperations;

internal static class AskForDoubleOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "process_flow_operations.ask_for_double", "Ask for Double",
        "briosa.ProcessFlowOperations", "AskForDouble", "/briosa.ProcessFlowOperations/AskForDouble",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("answer", "Answer", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.AskForDoubleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Question to ask", WorkerMpValueKind.Text, new WorkerTextValue(request.QuestionToAsk), "SetStringArg"),
            new("Initial Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.InitialValue), "SetDoubleArg"),
            new("Enforce Min/Max Values?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.EnforceMinMaxValues), "SetBoolArg"),
            new("Min Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.MinValue), "SetDoubleArg"),
            new("Max Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.MaxValue), "SetDoubleArg"),
            new("Font", WorkerMpValueKind.Font, FontMapper.WithDefaults(request.Font), "SetFontTypeArg")
        ], [new("Answer", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.AskForDoubleResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Answer = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details };
}
