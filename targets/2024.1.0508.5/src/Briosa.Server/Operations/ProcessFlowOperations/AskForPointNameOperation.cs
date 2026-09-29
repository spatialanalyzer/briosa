using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ProcessFlowOperations;

internal static class AskForPointNameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "process_flow_operations.ask_for_point_name", "Ask for Point Name",
        "briosa.ProcessFlowOperations", "AskForPointName", "/briosa.ProcessFlowOperations/AskForPointName",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("answer", "Answer", WorkerMpValueKind.PointName)];

    public static WorkerMpCommand CreateCommand(Api.AskForPointNameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Question to ask", WorkerMpValueKind.Text, new WorkerTextValue(request.QuestionToAsk), "SetStringArg"),
            new("Initial Value", WorkerMpValueKind.PointName, PointNameMapper.Required(request.InitialValue, "initial_value"), "SetPointNameArg"),
            new("Font", WorkerMpValueKind.Font, FontMapper.WithDefaults(request.Font), "SetFontTypeArg")
        ], [new("Answer", WorkerMpValueKind.PointName, "GetPointNameArg")]);
    }

    public static Api.AskForPointNameResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Answer = PointNameMapper.ToProtocol(completed.Execution.OutputValues[0].RequireValue<WorkerPointNameValue>()),
            Execution = completed.Details };
}
