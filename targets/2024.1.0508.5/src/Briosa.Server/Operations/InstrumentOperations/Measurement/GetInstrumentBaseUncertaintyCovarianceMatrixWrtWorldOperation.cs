using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_instrument_base_uncertainty_covariance_matrix_wrt_world",
        "Get Instrument Base Uncertainty Covariance Matrix WRT WORLD", "briosa.InstrumentOperations",
        "GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorld",
        "/briosa.InstrumentOperations/GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorld",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("covariance_matrix", "Covar Row 1", WorkerMpValueKind.DoubleArray),
        new("covariance_matrix", "Covar Row 2", WorkerMpValueKind.DoubleArray),
        new("covariance_matrix", "Covar Row 3", WorkerMpValueKind.DoubleArray),
        new("covariance_matrix", "Covar Row 4", WorkerMpValueKind.DoubleArray),
        new("covariance_matrix", "Covar Row 5", WorkerMpValueKind.DoubleArray),
        new("covariance_matrix", "Covar Row 6", WorkerMpValueKind.DoubleArray)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [
                new("Covar Row 1", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg"),
                new("Covar Row 2", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg"),
                new("Covar Row 3", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg"),
                new("Covar Row 4", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg"),
                new("Covar Row 5", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg"),
                new("Covar Row 6", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg")
            ]);
    }

    public static Api.GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldResult CreateResult(
        SuccessfulOperationExecution completed) => new()
    {
        CovarianceMatrix = UncertaintyCovarianceMatrixMapper.FromOutputRows(completed.Execution.OutputValues),
        Execution = completed.Details
    };
}
