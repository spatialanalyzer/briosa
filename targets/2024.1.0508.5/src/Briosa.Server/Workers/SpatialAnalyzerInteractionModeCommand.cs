using Briosa.Server.Operations.UtilityOperations;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Workers;

/// <summary>
/// The Set Interaction Mode command for <see cref="SpatialAnalyzerInteractionMode"/>.
/// The MP sets three values at once. The setting chooses the SA mode. The MP mode
/// and MP dialog mode are the pair that probe 25 verified on both targets
/// (maintainer decision on #313, 2026-10-25): SA documents the MP mode as the value
/// it returns to when a script exits, and the dialog mode lets the operator use SA
/// while a prompt is open. SA keeps the dialog mode across restarts, as it keeps
/// the SA mode.
/// </summary>
internal static class SpatialAnalyzerInteractionModeCommand
{
    internal const Api.MpInteractionMode MpInteractionMode =
        Api.MpInteractionMode.HaltOnFailureOrPartialSuccess;

    internal const Api.MpDialogInteractionMode MpDialogInteractionMode =
        Api.MpDialogInteractionMode.AllowApplicationInteraction;

    /// <summary>Returns <see langword="null"/> for <see cref="SpatialAnalyzerInteractionMode.Unchanged"/>.</summary>
    public static WorkerMpCommand? Create(SpatialAnalyzerInteractionMode mode) =>
        mode == SpatialAnalyzerInteractionMode.Unchanged
            ? null
            : SetInteractionModeOperation.CreateCommand(new Api.SetInteractionModeRequest
            {
                SaInteractionMode = mode switch
                {
                    SpatialAnalyzerInteractionMode.Silent => Api.SaInteractionMode.Silent,
                    SpatialAnalyzerInteractionMode.Automatic => Api.SaInteractionMode.Automatic,
                    SpatialAnalyzerInteractionMode.Manual => Api.SaInteractionMode.Manual,
                    _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
                },
                MeasurementPlanInteractionMode = MpInteractionMode,
                MeasurementPlanDialogInteractionMode = MpDialogInteractionMode
            });
}
