using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SetPointOfViewFromInstrumentUpdatesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.set_point_of_view_from_instrument_updates", "Set Point of View from Instrument Updates", "briosa.ViewControl",
        "SetPointOfViewFromInstrumentUpdates", "/briosa.ViewControl/SetPointOfViewFromInstrumentUpdates", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetPointOfViewFromInstrumentUpdatesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                CollectionInstrumentIdMapper.Required(request.InstrumentId, "instrument_id"), "SetColInstIdArg"),
            new("Display View Control", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasDisplayViewControl || request.DisplayViewControl), "SetBoolArg"),
            new("Enable Set Viewpoint From Instrument Updates", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.EnableSetViewpointFromInstrumentUpdates), "SetBoolArg"),
            new("Update View Percent", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasUpdateViewPercent ? request.UpdateViewPercent : 75d), "SetDoubleArg"),
            new("Clip Behind Probe", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ClipBehindProbe), "SetBoolArg"),
            new("Automatic Zoom When Trapping", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.AutomaticZoomWhenTrapping), "SetBoolArg"),
            new("Enable Directional Cloud Points", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.EnableDirectionalCloudPoints), "SetBoolArg"),
            new("Angle Reset Threshold", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasAngleResetThreshold ? request.AngleResetThreshold : 45d), "SetDoubleArg"),
            new("Animation Steps", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasAnimationSteps ? request.AnimationSteps : 8), "SetIntegerArg"),
            new("Reference Frame Object", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReferenceFrameObject, "reference_frame_object"), "SetCollectionObjectNameArg2"),
            new("Use Scan Stripe for View Focus", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasUseScanStripeForViewFocus || request.UseScanStripeForViewFocus), "SetBoolArg"),
            new("Zoom Factor", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasZoomFactor ? request.ZoomFactor : 1d), "SetDoubleArg")
        ], []);
    }

    public static Api.SetPointOfViewFromInstrumentUpdatesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
