using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.set_instrument_base_uncertainty_covariance_matrix_wrt_world",
        "Set Instrument Base Uncertainty Covariance Matrix WRT WORLD", "briosa.InstrumentOperations",
        "SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorld",
        "/briosa.InstrumentOperations/SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorld",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rows = UncertaintyCovarianceMatrixMapper.RequiredRows(request.CovarianceMatrix, "covariance_matrix");
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Covar Row 1", WorkerMpValueKind.DoubleArray, rows[0], "SetDoubleArrayArg"),
                new("Covar Row 2", WorkerMpValueKind.DoubleArray, rows[1], "SetDoubleArrayArg"),
                new("Covar Row 3", WorkerMpValueKind.DoubleArray, rows[2], "SetDoubleArrayArg"),
                new("Covar Row 4", WorkerMpValueKind.DoubleArray, rows[3], "SetDoubleArrayArg"),
                new("Covar Row 5", WorkerMpValueKind.DoubleArray, rows[4], "SetDoubleArrayArg"),
                new("Covar Row 6", WorkerMpValueKind.DoubleArray, rows[5], "SetDoubleArrayArg")
            ], []);
    }

    public static Api.SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
