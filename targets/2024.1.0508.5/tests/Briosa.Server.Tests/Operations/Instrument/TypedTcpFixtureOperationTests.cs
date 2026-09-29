using Briosa.Server.Operations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedTcpFixtureOperationTests
{
    private static readonly double[] TransformValues = [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 1, 2, 3, 1];
    private static readonly double[] VectorValues = [0.1, 0.2, 0.3, 0.4, 0.5, 0.6];
    private static readonly string[] OperationIds =
    [
        "instrument_operations.add_nominal_point_to_tcp_fixture",
        "instrument_operations.calculate_tcp_fixture_uncertainties",
        "instrument_operations.construct_tcp_fixture",
        "instrument_operations.get_last_solved_tcp_fixture_uncertainty_covariance_matrix"
    ];

    [Fact]
    public void TcpFixtureOperationsAreRegisteredAsTypedOperations()
    {
        foreach (var operationId in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == operationId);
        }
    }

    [Fact]
    public void CommandsPreserveTcpFixtureDefaultsAndRequiredStructuredInputs()
    {
        var addPoint = AddNominalPointToTcpFixtureOperation.CreateCommand(new()
        {
            TcpFixture = Fixture(),
            NominalPointLocation = new Api.Vector { X = 1, Y = 2, Z = 3 }
        });
        Assert.Equal(string.Empty, addPoint.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0, addPoint.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, addPoint.InputArguments[8].RequireValue<WorkerDoubleValue>().Value);
        Assert.Throws<ArgumentException>(() => AddNominalPointToTcpFixtureOperation.CreateCommand(new()
        {
            TcpFixture = Fixture()
        }));

        var construct = ConstructTcpFixtureOperation.CreateCommand(new() { RequestedTcpFixture = Fixture() });
        Assert.Equal(0, construct.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(construct.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientRoutesTcpFixtureOperationsAndMapsStructuredResults()
    {
        var worker = new TcpFixtureWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);

        var added = await client.AddNominalPointToTcpFixtureAsync(new()
        {
            TcpFixture = Fixture(),
            NominalPointName = "TCP-1",
            NominalPointLocation = new Api.Vector { X = 1, Y = 2, Z = 3 },
            VarXx = 0.01
        });
        var calculated = await client.CalculateTcpFixtureUncertaintiesAsync(new()
        {
            TcpFixture = Fixture(),
            TcpInWorking = new Api.Transform { Values = { TransformValues } },
            TcpMeasurements = { Point("P1"), Point("P2") }
        });
        var constructed = await client.ConstructTcpFixtureAsync(new()
        {
            RequestedTcpFixture = Fixture(),
            PointMatchThreshold = 0.25
        });
        var covariance = await client.GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixAsync(new()
        {
            TcpFixture = Fixture()
        });

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.NotNull(added.Execution);
        Assert.True(calculated.Uncertainties.SolutionValid);
        Assert.Equal(7, calculated.Uncertainties.RmsError);
        Assert.Equal(10, calculated.Uncertainties.Robustness);
        Assert.Equal("Fit converged", Assert.Single(calculated.Uncertainties.ResultNotes));
        Assert.Equal(6, calculated.Uncertainties.UncertaintiesInTcpFixtureFrame.Values.Count);
        Assert.Equal("TCP Fixture Result", constructed.ResultingTcpFixture.ObjectName);
        Assert.Equal(1, covariance.CovarianceMatrix.Row1.Values[0]);
        Assert.Equal(6, covariance.CovarianceMatrix.Row6.Values[5]);
        Assert.Equal(0.01, worker.Commands[0].InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(2, worker.Commands[1].InputArguments[2].RequireValue<WorkerPointNameListValue>().Values.Count);
        Assert.Equal(0.25, worker.Commands[2].InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
    }

    private static Api.CollectionObjectName Fixture() => new()
    {
        CollectionName = "Fixtures",
        ObjectName = "TCP Fixture"
    };

    private static Api.PointName Point(string name) => new()
    {
        CollectionName = "Measurements",
        GroupName = "TCP",
        TargetName = name
    };

    private sealed class TcpFixtureWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "instrument_operations.calculate_tcp_fixture_uncertainties" =>
                [
                    Boolean("Solution Valid", true),
                    Transform("Refined TCP In Working Frame"),
                    DoubleArray("Uncertainties in TCP Fixture Frame", VectorValues),
                    DoubleArray("Uncertainties in Working Frame", VectorValues),
                    Double("RMS Error", 7),
                    Double("MAX Abs Error", 8),
                    Double("Goodness Of Fit", 9),
                    Double("Robustness", 10),
                    new WorkerRetrievedOutput("Result Notes", WorkerMpValueKind.EditText,
                        new WorkerStringListValue(["Fit converged"]))
                ],
                "instrument_operations.construct_tcp_fixture" =>
                    [new WorkerRetrievedOutput("Resulting TCP Fixture", WorkerMpValueKind.CollectionObjectName,
                        new WorkerCollectionObjectNameValue("Fixtures", "TCP Fixture Result", WorkerObjectTypeValue.Any))],
                "instrument_operations.get_last_solved_tcp_fixture_uncertainty_covariance_matrix" =>
                [
                    DoubleArray("Covar Row 1", [1, 0, 0, 0, 0, 0]),
                    DoubleArray("Covar Row 2", [0, 2, 0, 0, 0, 0]),
                    DoubleArray("Covar Row 3", [0, 0, 3, 0, 0, 0]),
                    DoubleArray("Covar Row 4", [0, 0, 0, 4, 0, 0]),
                    DoubleArray("Covar Row 5", [0, 0, 0, 0, 5, 0]),
                    DoubleArray("Covar Row 6", [0, 0, 0, 0, 0, 6])
                ],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }

        private static WorkerRetrievedOutput Boolean(string name, bool value) =>
            new(name, WorkerMpValueKind.Logical, new WorkerBooleanValue(value));

        private static WorkerRetrievedOutput Double(string name, double value) =>
            new(name, WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(value));

        private static WorkerRetrievedOutput Transform(string name) =>
            new(name, WorkerMpValueKind.Transform, new WorkerTransformValue(TransformValues));

        private static WorkerRetrievedOutput DoubleArray(string name, IReadOnlyList<double> values) =>
            new(name, WorkerMpValueKind.DoubleArray, new WorkerDoubleArrayValue(values));
    }
}
