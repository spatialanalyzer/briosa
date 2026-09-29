using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class SetEllipsePropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.set_ellipse_properties", "Set Ellipse Properties",
        "briosa.AnalysisOperations", "SetEllipseProperties", "/briosa.AnalysisOperations/SetEllipseProperties",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetEllipsePropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Ellipse Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.EllipseName, "ellipse_name"), "SetCollectionObjectNameArg2"),
                new("Center Coordinate", WorkerMpValueKind.Vector,
                    VectorMapper.Required(request.CenterCoordinate, "center_coordinate"), "SetVectorArg"),
                new("Normal Direction", WorkerMpValueKind.Vector,
                    VectorMapper.Required(request.NormalDirection, "normal_direction"), "SetVectorArg"),
                new("Major Axis Radius", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasMajorAxisRadius ? request.MajorAxisRadius : 0d), "SetDoubleArg"),
                new("Minor Axis Radius", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasMinorAxisRadius ? request.MinorAxisRadius : 0d), "SetDoubleArg")
            ], []);
    }

    public static Api.SetEllipsePropertiesResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
