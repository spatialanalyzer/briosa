using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ScaleBarOperations;

internal static class ScaleBarCheckOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "scale_bar_operations.scale_bar_check", "Scale Bar Check",
        "briosa.ScaleBarOperations", "ScaleBarCheck", "/briosa.ScaleBarOperations/ScaleBarCheck",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe,
        ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("deviation_at_68f", "Deviation at 68F", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.ScaleBarCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("ScaleBar Point A", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.ScaleBarPointA, "scale_bar_point_a"), "SetPointNameArg"),
                new("ScaleBar Point B", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.ScaleBarPointB, "scale_bar_point_b"), "SetPointNameArg"),
                new("Current Temperature (F)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.CurrentTemperature), "SetDoubleArg"),
                new("Length of Bar at 68F", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.LengthOfBarAt68F), "SetDoubleArg"),
                new("Material CTE (PPM/F)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.MaterialCte), "SetDoubleArg"),
                new("Tolerance", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.Tolerance), "SetDoubleArg")
            ],
            [new("Deviation at 68F", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.ScaleBarCheckResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        DeviationAt68F = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
