using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetInteractionModeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_interaction_mode", "Set Interaction Mode", "briosa.UtilityOperations",
        "SetInteractionMode", "/briosa.UtilityOperations/SetInteractionMode", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetInteractionModeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("SA Interaction Mode", WorkerMpValueKind.SaInteractionMode,
                new WorkerChoiceValue<WorkerSaInteractionModeValue>(ToWorker(request.SaInteractionMode, request.HasSaInteractionMode)), "SetSAInteractionModeArg"),
            new("Measurement Plan Interaction Mode", WorkerMpValueKind.MpInteractionMode,
                new WorkerChoiceValue<WorkerMpInteractionModeValue>(ToWorker(request.MeasurementPlanInteractionMode, request.HasMeasurementPlanInteractionMode)), "SetMPInteractionModeArg"),
            new("Measurement Plan Dialog Interaction Mode", WorkerMpValueKind.MpDialogInteractionMode,
                new WorkerChoiceValue<WorkerMpDialogInteractionModeValue>(ToWorker(request.MeasurementPlanDialogInteractionMode, request.HasMeasurementPlanDialogInteractionMode)), "SetMPDialogInteractionModeArg")
        ], []);
    }

    public static Api.SetInteractionModeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };

    private static WorkerSaInteractionModeValue ToWorker(Api.SaInteractionMode value, bool supplied) =>
        supplied ? value switch
        {
            Api.SaInteractionMode.Manual => WorkerSaInteractionModeValue.Manual,
            Api.SaInteractionMode.Automatic => WorkerSaInteractionModeValue.Automatic,
            Api.SaInteractionMode.Silent => WorkerSaInteractionModeValue.Silent,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "SA interaction mode is required.")
        } : throw new ArgumentException("Request field 'sa_interaction_mode' is required.", nameof(value));

    private static WorkerMpInteractionModeValue ToWorker(Api.MpInteractionMode value, bool supplied) =>
        supplied ? value switch
        {
            Api.MpInteractionMode.HaltOnFailureOnly => WorkerMpInteractionModeValue.HaltOnFailureOnly,
            Api.MpInteractionMode.HaltOnFailureOrPartialSuccess => WorkerMpInteractionModeValue.HaltOnFailureOrPartialSuccess,
            Api.MpInteractionMode.NeverHalt => WorkerMpInteractionModeValue.NeverHalt,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "MP interaction mode is required.")
        } : throw new ArgumentException("Request field 'measurement_plan_interaction_mode' is required.", nameof(value));

    private static WorkerMpDialogInteractionModeValue ToWorker(Api.MpDialogInteractionMode value, bool supplied) =>
        supplied ? value switch
        {
            Api.MpDialogInteractionMode.BlockApplicationInteraction => WorkerMpDialogInteractionModeValue.BlockApplicationInteraction,
            Api.MpDialogInteractionMode.AllowApplicationInteraction => WorkerMpDialogInteractionModeValue.AllowApplicationInteraction,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "MP dialog interaction mode is required.")
        } : throw new ArgumentException("Request field 'measurement_plan_dialog_interaction_mode' is required.", nameof(value));
}
