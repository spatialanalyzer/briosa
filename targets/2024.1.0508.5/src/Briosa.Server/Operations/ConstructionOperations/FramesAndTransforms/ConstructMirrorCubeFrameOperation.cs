using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructMirrorCubeFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_mirror_cube_frame", "Construct Mirror Cube Frame",
        "briosa.ConstructionOperations", "ConstructMirrorCubeFrame",
        "/briosa.ConstructionOperations/ConstructMirrorCubeFrame", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("total_angular_error", "Total Angular Error", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.ConstructMirrorCubeFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Mirror Cube Frame Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.MirrorCubeFrameName, "mirror_cube_frame_name",
                    WorkerObjectTypeValue.Frame), "SetCollectionObjectNameArg2"),
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg"),
            new("Use Current Measurements Marked as Mirror Shots", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasUseCurrentMeasurementsMarkedAsMirrorShots ||
                    request.UseCurrentMeasurementsMarkedAsMirrorShots), "SetBoolArg"),
            new("Nominal Cube Face Angle", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasNominalCubeFaceAngle ? request.NominalCubeFaceAngle : 90d),
                "SetDoubleArg")
        ], [new("Total Angular Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.ConstructMirrorCubeFrameResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            TotalAngularError = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
}
