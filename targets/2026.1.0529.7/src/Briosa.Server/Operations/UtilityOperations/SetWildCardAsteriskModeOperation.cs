using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetWildCardAsteriskModeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_wild_card_asterisk_mode", "Set Wild Card Asterisk Mode", "briosa.UtilityOperations",
        "SetWildCardAsteriskMode", "/briosa.UtilityOperations/SetWildCardAsteriskMode", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetWildCardAsteriskModeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Auto Wrap Search String?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasAutoWrapSearchString || request.AutoWrapSearchString), "SetBoolArg")], []);
    }

    public static Api.SetWildCardAsteriskModeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
