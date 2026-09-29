using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetCirclePropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_circle_properties", "Get Circle Properties",
        "briosa.AnalysisOperations", "GetCircleProperties", "/briosa.AnalysisOperations/GetCircleProperties",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("center_coordinate", "Center Coordinate", WorkerMpValueKind.Vector),
        new("normal_direction", "Normal Direction", WorkerMpValueKind.Vector),
        new("radius", "Radius", WorkerMpValueKind.FloatingPoint),
        new("diameter", "Diameter", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetCirclePropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Circle Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CircleName, "circle_name"), "SetCollectionObjectNameArg2")],
            [
                new("Center Coordinate", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Normal Direction", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Radius", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Diameter", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetCirclePropertiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            CenterCoordinate = VectorMapper.ToProtocol(values[0].RequireValue<WorkerVectorValue>()),
            NormalDirection = VectorMapper.ToProtocol(values[1].RequireValue<WorkerVectorValue>()),
            Radius = values[2].RequireValue<WorkerDoubleValue>().Value,
            Diameter = values[3].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
