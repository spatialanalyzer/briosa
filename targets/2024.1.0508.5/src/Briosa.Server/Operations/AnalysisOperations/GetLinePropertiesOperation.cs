using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetLinePropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_line_properties", "Get Line Properties",
        "briosa.AnalysisOperations", "GetLineProperties", "/briosa.AnalysisOperations/GetLineProperties",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("begin_coordinate", "Begin Coordinate", WorkerMpValueKind.Vector),
        new("end_coordinate", "End Coordinate", WorkerMpValueKind.Vector),
        new("delta_components", "Delta Components", WorkerMpValueKind.Vector),
        new("length", "Length", WorkerMpValueKind.FloatingPoint),
        new("angle_about_x_from_y_in_yz_plane", "Angle about +X from +Y in YZ plane", WorkerMpValueKind.FloatingPoint),
        new("angle_about_y_from_z_in_xz_plane", "Angle about +Y from +Z in XZ plane", WorkerMpValueKind.FloatingPoint),
        new("angle_about_z_from_x_in_xy_plane", "Angle about +Z from +X in XY plane", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetLinePropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Line Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.LineName, "line_name"), "SetCollectionObjectNameArg2")],
            [
                new("Begin Coordinate", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("End Coordinate", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Delta Components", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Length", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Angle about +X from +Y in YZ plane", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Angle about +Y from +Z in XZ plane", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Angle about +Z from +X in XY plane", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetLinePropertiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            BeginCoordinate = VectorMapper.ToProtocol(values[0].RequireValue<WorkerVectorValue>()),
            EndCoordinate = VectorMapper.ToProtocol(values[1].RequireValue<WorkerVectorValue>()),
            DeltaComponents = VectorMapper.ToProtocol(values[2].RequireValue<WorkerVectorValue>()),
            Length = values[3].RequireValue<WorkerDoubleValue>().Value,
            AngleAboutXFromYInYzPlane = values[4].RequireValue<WorkerDoubleValue>().Value,
            AngleAboutYFromZInXzPlane = values[5].RequireValue<WorkerDoubleValue>().Value,
            AngleAboutZFromXInXyPlane = values[6].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
