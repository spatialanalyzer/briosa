using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetEllipsePropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_ellipse_properties", "Get Ellipse Properties",
        "briosa.AnalysisOperations", "GetEllipseProperties", "/briosa.AnalysisOperations/GetEllipseProperties",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("center_coordinate", "Center Coordinate", WorkerMpValueKind.Vector),
        new("normal_direction", "Normal Direction", WorkerMpValueKind.Vector),
        new("major_axis_radius", "Major Axis Radius", WorkerMpValueKind.FloatingPoint),
        new("minor_axis_radius", "Minor Axis Radius", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetEllipsePropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Ellipse Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.EllipseName, "ellipse_name"), "SetCollectionObjectNameArg2")],
            [
                new("Center Coordinate", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Normal Direction", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Major Axis Radius", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Minor Axis Radius", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetEllipsePropertiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            CenterCoordinate = VectorMapper.ToProtocol(values[0].RequireValue<WorkerVectorValue>()),
            NormalDirection = VectorMapper.ToProtocol(values[1].RequireValue<WorkerVectorValue>()),
            MajorAxisRadius = values[2].RequireValue<WorkerDoubleValue>().Value,
            MinorAxisRadius = values[3].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
