using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class MushroomTargetHoleInspectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.mushroom_target_hole_inspection", "Mushroom Target Hole Inspection",
        "briosa.AnalysisOperations", "MushroomTargetHoleInspection",
        "/briosa.AnalysisOperations/MushroomTargetHoleInspection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("sphere_fit_rms_error", "Sphere Fit RMS Error", WorkerMpValueKind.FloatingPoint),
        new("sphere_fit_max_error", "Sphere Fit Max Error", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.MushroomTargetHoleInspectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Name Prefix for Intermediate Constructions", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasNamePrefixForIntermediateConstructions
                        ? request.NamePrefixForIntermediateConstructions : string.Empty), "SetStringArg"),
                new("Sphere Points Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.SpherePointsGroupName, "sphere_points_group_name"), "SetCollectionObjectNameArg2"),
                new("Sphere Target Radius", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasSphereTargetRadius ? request.SphereTargetRadius : 0d), "SetDoubleArg"),
                new("Target Contact Plane", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.TargetContactPlane, "target_contact_plane"), "SetCollectionObjectNameArg2"),
                new("Point To Create at Hole", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.PointToCreateAtHole, "point_to_create_at_hole"), "SetPointNameArg")
            ],
            [
                new("Sphere Fit RMS Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Sphere Fit Max Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.MushroomTargetHoleInspectionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            SphereFitRmsError = values[0].RequireValue<WorkerDoubleValue>().Value,
            SphereFitMaxError = values[1].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
