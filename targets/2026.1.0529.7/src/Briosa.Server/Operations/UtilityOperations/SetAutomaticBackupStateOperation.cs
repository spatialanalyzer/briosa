using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetAutomaticBackupStateOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_automatic_backup_state", "Set Automatic Backup State", "briosa.UtilityOperations",
        "SetAutomaticBackupState", "/briosa.UtilityOperations/SetAutomaticBackupState", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetAutomaticBackupStateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Auto Job File Restore Points Active?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasAutoJobFileRestorePointsActive || request.AutoJobFileRestorePointsActive), "SetBoolArg"),
            new("Auto Measurements Backup Active?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasAutoMeasurementsBackupActive || request.AutoMeasurementsBackupActive), "SetBoolArg")
        ], []);
    }

    public static Api.SetAutomaticBackupStateResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
