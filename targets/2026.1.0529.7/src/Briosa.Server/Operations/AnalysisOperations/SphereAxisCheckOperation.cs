using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class SphereAxisCheckOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.sphere_axis_check", "Sphere Axis Check",
        "briosa.AnalysisOperations", "SphereAxisCheck", "/briosa.AnalysisOperations/SphereAxisCheck",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("sphere_fit_rms_error", "Sphere Fit RMS Error", WorkerMpValueKind.FloatingPoint),
        new("sphere_fit_max_error", "Sphere Fit Max Error", WorkerMpValueKind.FloatingPoint),
        new("vector_representation", "Vector Representation", WorkerMpValueKind.Vector),
        new("x_value", "X Value", WorkerMpValueKind.FloatingPoint),
        new("y_value", "Y Value", WorkerMpValueKind.FloatingPoint),
        new("z_value", "Z Value", WorkerMpValueKind.FloatingPoint),
        new("magnitude", "Magnitude", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.SphereAxisCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Sphere Points Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.SpherePointsGroupName, "sphere_points_group_name"), "SetCollectionObjectNameArg2"),
                new("Sphere Target Radius", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasSphereTargetRadius ? request.SphereTargetRadius : 0d), "SetDoubleArg"),
                new("Point To Create at Sphere Center", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.PointToCreateAtSphereCenter, "point_to_create_at_sphere_center"), "SetPointNameArg"),
                new("Line defining the axis", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.LineDefiningTheAxis, "line_defining_the_axis"), "SetCollectionObjectNameArg2")
            ],
            [
                new("Sphere Fit RMS Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Sphere Fit Max Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Vector Representation", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("X Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Y Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Z Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Magnitude", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.SphereAxisCheckResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            SphereFitRmsError = values[0].RequireValue<WorkerDoubleValue>().Value,
            SphereFitMaxError = values[1].RequireValue<WorkerDoubleValue>().Value,
            VectorRepresentation = VectorMapper.ToProtocol(values[2].RequireValue<WorkerVectorValue>()),
            XValue = values[3].RequireValue<WorkerDoubleValue>().Value,
            YValue = values[4].RequireValue<WorkerDoubleValue>().Value,
            ZValue = values[5].RequireValue<WorkerDoubleValue>().Value,
            Magnitude = values[6].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
