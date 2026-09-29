using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetCylinderPropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_cylinder_properties", "Get Cylinder Properties",
        "briosa.AnalysisOperations", "GetCylinderProperties", "/briosa.AnalysisOperations/GetCylinderProperties",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("begin_coordinate", "Begin Coordinate", WorkerMpValueKind.Vector),
        new("end_coordinate", "End Coordinate", WorkerMpValueKind.Vector),
        new("axis_direction", "Axis Direction", WorkerMpValueKind.Vector),
        new("length", "Length", WorkerMpValueKind.FloatingPoint),
        new("radius", "Radius", WorkerMpValueKind.FloatingPoint),
        new("diameter", "Diameter", WorkerMpValueKind.FloatingPoint),
        new("nominals_point_inward", "Nominals Point Inward", WorkerMpValueKind.Logical),
        new("facets", "Facets", WorkerMpValueKind.WholeNumber),
        new("enable_theta_extent_display_mode", "Enable Theta Extent Display Mode", WorkerMpValueKind.Logical),
        new("theta_start_in_degrees", "Theta Start in Degrees", WorkerMpValueKind.FloatingPoint),
        new("theta_span_in_degrees", "Theta Span in Degrees", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetCylinderPropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Cylinder Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CylinderName, "cylinder_name"), "SetCollectionObjectNameArg2")],
            [
                new("Begin Coordinate", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("End Coordinate", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Axis Direction", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Length", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Radius", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Diameter", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Nominals Point Inward", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Facets", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Enable Theta Extent Display Mode", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Theta Start in Degrees", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Theta Span in Degrees", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetCylinderPropertiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            BeginCoordinate = VectorMapper.ToProtocol(values[0].RequireValue<WorkerVectorValue>()),
            EndCoordinate = VectorMapper.ToProtocol(values[1].RequireValue<WorkerVectorValue>()),
            AxisDirection = VectorMapper.ToProtocol(values[2].RequireValue<WorkerVectorValue>()),
            Length = values[3].RequireValue<WorkerDoubleValue>().Value,
            Radius = values[4].RequireValue<WorkerDoubleValue>().Value,
            Diameter = values[5].RequireValue<WorkerDoubleValue>().Value,
            NominalsPointInward = values[6].RequireValue<WorkerBooleanValue>().Value,
            Facets = values[7].RequireValue<WorkerIntegerValue>().Value,
            EnableThetaExtentDisplayMode = values[8].RequireValue<WorkerBooleanValue>().Value,
            ThetaStartInDegrees = values[9].RequireValue<WorkerDoubleValue>().Value,
            ThetaSpanInDegrees = values[10].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
