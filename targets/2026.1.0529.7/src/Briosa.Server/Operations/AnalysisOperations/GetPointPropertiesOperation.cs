using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetPointPropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_point_properties", "Get Point Properties",
        "briosa.AnalysisOperations", "GetPointProperties", "/briosa.AnalysisOperations/GetPointProperties",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("planar_offset", "Planar Offset", WorkerMpValueKind.FloatingPoint),
        new("radial_offset", "Radial Offset", WorkerMpValueKind.FloatingPoint),
        new("ux", "Ux", WorkerMpValueKind.FloatingPoint),
        new("uy", "Uy", WorkerMpValueKind.FloatingPoint),
        new("uz", "Uz", WorkerMpValueKind.FloatingPoint),
        new("umag", "Umag", WorkerMpValueKind.FloatingPoint),
        new("position_tolerance", "Position Tolerance", WorkerMpValueKind.ToleranceVectorOptions),
        new("component_weights", "Component Weights", WorkerMpValueKind.Vector)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetPointPropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg")],
            [
                new("Planar Offset", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Radial Offset", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Ux", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Uy", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Uz", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Umag", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Position Tolerance", WorkerMpValueKind.ToleranceVectorOptions, "GetToleranceVectorOptionsArg"),
                new("Component Weights", WorkerMpValueKind.Vector, "GetVectorArg")
            ]);
    }

    public static Api.GetPointPropertiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            PlanarOffset = values[0].RequireValue<WorkerDoubleValue>().Value,
            RadialOffset = values[1].RequireValue<WorkerDoubleValue>().Value,
            Ux = values[2].RequireValue<WorkerDoubleValue>().Value,
            Uy = values[3].RequireValue<WorkerDoubleValue>().Value,
            Uz = values[4].RequireValue<WorkerDoubleValue>().Value,
            Umag = values[5].RequireValue<WorkerDoubleValue>().Value,
            PositionTolerance = ToleranceVectorOptionsMapper.ToProtocol(
                values[6].RequireValue<WorkerToleranceVectorOptionsValue>()),
            ComponentWeights = VectorMapper.ToProtocol(values[7].RequireValue<WorkerVectorValue>()),
            Execution = completed.Details
        };
    }
}
