using Briosa.Server.Operations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedInstrumentBestFitOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.locate_instrument_best_fit_group_to_group",
        "instrument_operations.locate_instrument_best_fit_nominal_geometry"
    ];

    private static readonly double[] TransformValues = Enumerable.Range(0, 16).Select(value => (double)value).ToArray();

    [Fact]
    public void BestFitCommandsPreserveDefaultsAndOptionalCsvReport()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var reference = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Reference" };
        var corresponding = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Corresponding" };
        var groupToGroup = LocateInstrumentBestFitGroupToGroupOperation.CreateCommand(new()
        {
            ReferenceGroup = reference,
            CorrespondingGroup = corresponding,
            AllowX = false
        });
        Assert.Equal(14, groupToGroup.InputArguments.Count);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            groupToGroup.InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.False(groupToGroup.InputArguments[6].RequireValue<WorkerBooleanValue>().Value);
        Assert.All(groupToGroup.InputArguments.Skip(7).Take(5), input =>
            Assert.True(input.RequireValue<WorkerBooleanValue>().Value));
        Assert.False(groupToGroup.InputArguments[12].RequireValue<WorkerBooleanValue>().Value);
        var withCsv = LocateInstrumentBestFitGroupToGroupOperation.CreateCommand(new()
        {
            ReferenceGroup = reference,
            CorrespondingGroup = corresponding,
            CsvReport = new() { Path = @"C:\reports\fit.csv" }
        });
        Assert.Equal(15, withCsv.InputArguments.Count);
        Assert.Equal(@"C:\reports\fit.csv", withCsv.InputArguments[^1].RequireValue<WorkerFileReferenceValue>().Path);

        var nominalGeometry = LocateInstrumentBestFitNominalGeometryOperation.CreateCommand(new()
        {
            Instrument = new() { CollectionName = "Trackers", InstrumentId = 4 },
            GeometryRelationships = { reference }
        });
        Assert.Equal(14, nominalGeometry.InputArguments.Count);
        Assert.Equal(WorkerObjectTypeValue.Any,
            nominalGeometry.InputArguments[1].RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectType);
        Assert.All(nominalGeometry.InputArguments.Skip(8).Take(4), input =>
            Assert.True(input.RequireValue<WorkerBooleanValue>().Value));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedBestFitRoutesAndMapsOutputs()
    {
        var worker = new InstrumentWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var reference = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Reference" };
        var corresponding = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Corresponding" };

        var groupResult = await client.LocateInstrumentBestFitGroupToGroupAsync(new()
        {
            ReferenceGroup = reference,
            CorrespondingGroup = corresponding
        }, options);
        var nominalResult = await client.LocateInstrumentBestFitNominalGeometryAsync(new()
        {
            Instrument = new() { CollectionName = "Trackers", InstrumentId = 4 },
            GeometryRelationships = { reference }
        }, options);

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(TransformValues, groupResult.TransformInWorking.Values);
        Assert.Equal(1.5, groupResult.OptimumTransform.ScaleFactor);
        Assert.Equal(7, groupResult.NumberOfEquations);
        Assert.Equal(9.5, nominalResult.Robustness);
    }

    private sealed class InstrumentWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs =
            [
                new WorkerRetrievedOutput("Transform in Working", WorkerMpValueKind.Transform, new WorkerTransformValue(TransformValues)),
                new WorkerRetrievedOutput("Optimum Transform", WorkerMpValueKind.WorldTransform,
                    new WorkerWorldTransformValue(new WorkerTransformValue(TransformValues), 1.5)),
                new WorkerRetrievedOutput("RMS Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2.5)),
                new WorkerRetrievedOutput("Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(3.5)),
                new WorkerRetrievedOutput("Number of Unknowns", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(6)),
                new WorkerRetrievedOutput("Number of Equations", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(7)),
                new WorkerRetrievedOutput("Robustness", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(9.5))
            ];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
