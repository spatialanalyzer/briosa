using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetObservationMirrorCubeShotFaceOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.set_observation_mirror_cube_shot_face", "Set Observation Mirror Cube Shot Face",
        "briosa.InstrumentOperations", "SetObservationMirrorCubeShotFace",
        "/briosa.InstrumentOperations/SetObservationMirrorCubeShotFace",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetObservationMirrorCubeShotFaceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var observationIndex = request.HasObservationIndex ? request.ObservationIndex : 0;
        var isMirrorCubeShot = request.HasIsMirrorCubeShot && request.IsMirrorCubeShot;
        var mirrorCubeShotFace = request.HasMirrorCubeShotFace ? request.MirrorCubeShotFace : 1;

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.Point, "point"), "SetPointNameArg"),
            new("Observation Index", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(observationIndex), "SetIntegerArg"),
            new("Is Mirror Cube Shot? (FALSE = Normal)", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(isMirrorCubeShot), "SetBoolArg"),
            new("Mirror Cube Shot Face (1 .. 6)", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(mirrorCubeShotFace), "SetIntegerArg")
        ], []);
    }

    public static Api.SetObservationMirrorCubeShotFaceResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
