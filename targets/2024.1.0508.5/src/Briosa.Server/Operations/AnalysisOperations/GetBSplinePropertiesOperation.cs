using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetBSplinePropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_bspline_properties", "Get B-Spline Properties",
        "briosa.AnalysisOperations", "GetBSplineProperties", "/briosa.AnalysisOperations/GetBSplineProperties",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("degree", "Degree", WorkerMpValueKind.WholeNumber),
        new("knots", "Knots", WorkerMpValueKind.WholeNumber),
        new("control_points", "Control Points", WorkerMpValueKind.WholeNumber),
        new("range_min", "Range Min", WorkerMpValueKind.FloatingPoint),
        new("range_max", "Range Max", WorkerMpValueKind.FloatingPoint),
        new("length", "Length", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetBSplinePropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("B-Spline Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.BSplineName, "b_spline_name"), "SetCollectionObjectNameArg2")],
            [
                new("Degree", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Knots", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Control Points", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Range Min", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Range Max", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Length", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetBSplinePropertiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Degree = values[0].RequireValue<WorkerIntegerValue>().Value,
            Knots = values[1].RequireValue<WorkerIntegerValue>().Value,
            ControlPoints = values[2].RequireValue<WorkerIntegerValue>().Value,
            RangeMin = values[3].RequireValue<WorkerDoubleValue>().Value,
            RangeMax = values[4].RequireValue<WorkerDoubleValue>().Value,
            Length = values[5].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
