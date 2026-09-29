using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class ComputeCteScaleFactorOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.compute_cte_scale_factor", "Compute CTE Scale Factor", "ComputeCteScaleFactor");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("scale_factor", "Scale Factor", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.ComputeCteScaleFactorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Material CTE (1/Deg F)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.MaterialCte), "SetDoubleArg"),
                new("Initial Temperature (F)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.InitialTemperature), "SetDoubleArg"),
                new("Final Temperature (F)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.FinalTemperature), "SetDoubleArg")
            ],
            [new("Scale Factor", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.ComputeCteScaleFactorResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        ScaleFactor = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
