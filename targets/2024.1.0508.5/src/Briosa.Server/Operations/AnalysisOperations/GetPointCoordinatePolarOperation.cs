using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetPointCoordinatePolarOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_point_coordinate_polar", "Get Point Coordinate (Polar)",
        "briosa.AnalysisOperations", "GetPointCoordinatePolar", "/briosa.AnalysisOperations/GetPointCoordinatePolar",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("radius_value", "Radius Value", WorkerMpValueKind.FloatingPoint),
        new("theta_value", "Theta Value", WorkerMpValueKind.FloatingPoint),
        new("phi_value", "Phi Value", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetPointCoordinatePolarRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg")],
            [
                new("Radius Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Theta Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Phi Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetPointCoordinatePolarResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            RadiusValue = values[0].RequireValue<WorkerDoubleValue>().Value,
            ThetaValue = values[1].RequireValue<WorkerDoubleValue>().Value,
            PhiValue = values[2].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
