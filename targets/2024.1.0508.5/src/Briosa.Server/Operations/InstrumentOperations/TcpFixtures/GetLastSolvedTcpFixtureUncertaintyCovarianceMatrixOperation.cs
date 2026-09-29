using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.ReadOnly(
        "instrument_operations.get_last_solved_tcp_fixture_uncertainty_covariance_matrix",
        "Get Last Solved TCP Fixture Uncertainty Covariance Matrix", "GetLastSolvedTcpFixtureUncertaintyCovarianceMatrix");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("covariance_matrix", "Covar Row 1", WorkerMpValueKind.DoubleArray),
        new("covariance_matrix", "Covar Row 2", WorkerMpValueKind.DoubleArray),
        new("covariance_matrix", "Covar Row 3", WorkerMpValueKind.DoubleArray),
        new("covariance_matrix", "Covar Row 4", WorkerMpValueKind.DoubleArray),
        new("covariance_matrix", "Covar Row 5", WorkerMpValueKind.DoubleArray),
        new("covariance_matrix", "Covar Row 6", WorkerMpValueKind.DoubleArray)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("TCP Fixture", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.TcpFixture, "tcp_fixture"), "SetCollectionObjectNameArg2")],
            [
                new("Covar Row 1", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg"),
                new("Covar Row 2", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg"),
                new("Covar Row 3", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg"),
                new("Covar Row 4", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg"),
                new("Covar Row 5", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg"),
                new("Covar Row 6", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg")
            ]);
    }

    public static Api.GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixResult CreateResult(
        SuccessfulOperationExecution completed) => new()
    {
        CovarianceMatrix = UncertaintyCovarianceMatrixMapper.FromOutputRows(completed.Execution.OutputValues),
        Execution = completed.Details
    };
}
