using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class WaitForTrappingToCompleteOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.wait_for_trapping_to_complete", "Wait For Trapping To Complete",
        "briosa.InstrumentOperations", "WaitForTrappingToComplete",
        "/briosa.InstrumentOperations/WaitForTrappingToComplete",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.WaitForTrappingToCompleteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [], []);
    }

    public static Api.WaitForTrappingToCompleteResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
