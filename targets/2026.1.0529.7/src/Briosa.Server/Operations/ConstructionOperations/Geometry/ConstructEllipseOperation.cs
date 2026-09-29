using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructEllipseOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_ellipse", "Construct Ellipse",
        "briosa.ConstructionOperations", "ConstructEllipse", "/briosa.ConstructionOperations/ConstructEllipse",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructEllipseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Ellipse Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.EllipseName, "ellipse_name", WorkerObjectTypeValue.Ellipse), "SetCollectionObjectNameArg2"),
            new("Center Coordinate", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.CenterCoordinate, "center_coordinate"), "SetVectorArg"),
            new("Normal Direction", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.NormalDirection, "normal_direction"), "SetVectorArg"),
            new("Major Axis Radius", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMajorAxisRadius ? request.MajorAxisRadius : 0), "SetDoubleArg"),
            new("Minor Axis Radius", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMinorAxisRadius ? request.MinorAxisRadius : 0), "SetDoubleArg")
        ], []);
    }

    public static Api.ConstructEllipseResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
