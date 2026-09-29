using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetPointToLineDistanceOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_point_to_line_distance", "Get Point To Line Distance",
        "briosa.AnalysisOperations", "GetPointToLineDistance", "/briosa.AnalysisOperations/GetPointToLineDistance",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("vector_representation", "Vector Representation", WorkerMpValueKind.Vector),
        new("x_value", "X Value", WorkerMpValueKind.FloatingPoint),
        new("y_value", "Y Value", WorkerMpValueKind.FloatingPoint),
        new("z_value", "Z Value", WorkerMpValueKind.FloatingPoint),
        new("magnitude", "Magnitude", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetPointToLineDistanceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Point", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.Point, "point"), "SetPointNameArg"),
                new("Line", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.Line, "line"), "SetCollectionObjectNameArg2")
            ],
            [
                new("Vector Representation", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("X Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Y Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Z Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Magnitude", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetPointToLineDistanceResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            VectorRepresentation = VectorMapper.ToProtocol(values[0].RequireValue<WorkerVectorValue>()),
            XValue = values[1].RequireValue<WorkerDoubleValue>().Value,
            YValue = values[2].RequireValue<WorkerDoubleValue>().Value,
            ZValue = values[3].RequireValue<WorkerDoubleValue>().Value,
            Magnitude = values[4].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
