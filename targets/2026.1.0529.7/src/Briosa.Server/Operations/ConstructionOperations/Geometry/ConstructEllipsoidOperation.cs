using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructEllipsoidOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_ellipsoid", "Construct Ellipsoid",
        "briosa.ConstructionOperations", "ConstructEllipsoid", "/briosa.ConstructionOperations/ConstructEllipsoid",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructEllipsoidRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var color = request.EllipseColor;
        if (color is not null && (color.Red > byte.MaxValue || color.Green > byte.MaxValue || color.Blue > byte.MaxValue))
            throw new ArgumentOutOfRangeException(nameof(request), "Color channels must be in 0..255.");

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Ellipse Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.EllipseName, "ellipse_name", WorkerObjectTypeValue.Any), "SetCollectionObjectNameArg2"),
            new("X-Axis Radius", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasXAxisRadius ? request.XAxisRadius : 5), "SetDoubleArg"),
            new("Y-Axis Radius", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasYAxisRadius ? request.YAxisRadius : 4), "SetDoubleArg"),
            new("Z-Axis Radius", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasZAxisRadius ? request.ZAxisRadius : 3), "SetDoubleArg"),
            new("Magnification", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasMagnification ? request.Magnification : 1), "SetDoubleArg"),
            new("Uncertainty Ellipsoid?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasUncertaintyEllipsoid && request.UncertaintyEllipsoid), "SetBoolArg"),
            new("Transform in Working Coordinates", WorkerMpValueKind.Transform,
                TransformMapper.Required(request.TransformInWorkingCoordinates, "transform_in_working_coordinates"), "SetTransformArg"),
            new("Ellipse Color", WorkerMpValueKind.RgbColor,
                color is null ? new WorkerRgbColorValue(255, 0, 0) : new WorkerRgbColorValue((byte)color.Red, (byte)color.Green, (byte)color.Blue), "SetColorArg")
        ], []);
    }

    public static Api.ConstructEllipsoidResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
