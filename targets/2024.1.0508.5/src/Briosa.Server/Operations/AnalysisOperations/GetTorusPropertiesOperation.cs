using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetTorusPropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_torus_properties", "Get Torus Properties",
        "briosa.AnalysisOperations", "GetTorusProperties", "/briosa.AnalysisOperations/GetTorusProperties",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("center_coordinate", "Center Coordinate", WorkerMpValueKind.Vector),
        new("normal_direction", "Normal Direction", WorkerMpValueKind.Vector),
        new("major_radius", "Major Radius", WorkerMpValueKind.FloatingPoint),
        new("minor_radius", "Minor Radius", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetTorusPropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Torus Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.TorusName, "torus_name"), "SetCollectionObjectNameArg2")],
            [
                new("Center Coordinate", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Normal Direction", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Major Radius", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Minor Radius", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetTorusPropertiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            CenterCoordinate = VectorMapper.ToProtocol(values[0].RequireValue<WorkerVectorValue>()),
            NormalDirection = VectorMapper.ToProtocol(values[1].RequireValue<WorkerVectorValue>()),
            MajorRadius = values[2].RequireValue<WorkerDoubleValue>().Value,
            MinorRadius = values[3].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
