using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetPlanePropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_plane_properties", "Get Plane Properties",
        "briosa.AnalysisOperations", "GetPlaneProperties", "/briosa.AnalysisOperations/GetPlaneProperties",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("normal_direction", "Normal Direction", WorkerMpValueKind.Vector),
        new("point_on_plane", "Point on Plane", WorkerMpValueKind.Vector),
        new("d_parameter", "D Parameter", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetPlanePropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Plane Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.PlaneName, "plane_name"), "SetCollectionObjectNameArg2")],
            [
                new("Normal Direction", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Point on Plane", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("D Parameter", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetPlanePropertiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            NormalDirection = VectorMapper.ToProtocol(values[0].RequireValue<WorkerVectorValue>()),
            PointOnPlane = VectorMapper.ToProtocol(values[1].RequireValue<WorkerVectorValue>()),
            DParameter = values[2].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
