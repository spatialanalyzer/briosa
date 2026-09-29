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

public sealed class TypedInstrumentAlignmentAndTieInOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.align_laser_projector",
        "instrument_operations.align_two_targets_with_axis_wcf_x",
        "instrument_operations.locate_instrument_ref_tie_in"
    ];

    [Fact]
    public void AlignmentAndTieInCommandsPreserveTypesAndOptionalArguments()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var group = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Measured", ObjectType = Api.ObjectType.PointGroup };
        var laser = AlignLaserProjectorOperation.CreateCommand(new() { Instrument = instrument, Group = group });
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            laser.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);

        var firstPoint = new Api.PointName { CollectionName = "Points", GroupName = "Axis", TargetName = "P1" };
        var secondPoint = new Api.PointName { CollectionName = "Points", GroupName = "Axis", TargetName = "P2" };
        var axisRequest = new Api.AlignTwoTargetsWithAxisWcfXRequest
        {
            Instrument = instrument,
            FirstPointOnAxis = firstPoint,
            SecondPointOnAxis = secondPoint,
            InitialMeasuredGroup = group
        };
        var axis = AlignTwoTargetsWithAxisWcfXOperation.CreateCommand(axisRequest);
        Assert.Equal(4, axis.InputArguments.Count);
        axisRequest.RotationalTolerance = new()
        {
            HighX = new() { Enabled = true, Value = 0.25 }
        };
        axis = AlignTwoTargetsWithAxisWcfXOperation.CreateCommand(axisRequest);
        Assert.Equal(5, axis.InputArguments.Count);
        Assert.Equal(new WorkerToleranceLimit(true, 0.25),
            axis.InputArguments[4].RequireValue<WorkerToleranceVectorOptionsValue>().HighX);

        var tieIn = LocateInstrumentRefTieInOperation.CreateCommand(new()
        {
            Instrument = instrument,
            ReferenceGroup = new() { CollectionName = "Points", ObjectName = "Reference", ObjectType = Api.ObjectType.PointGroup },
            ActualsGroup = group
        });
        Assert.Equal(0d, tieIn.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(tieIn.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedAlignmentAndTieInRoutes()
    {
        var worker = new InstrumentWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var group = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Measured" };

        await client.AlignLaserProjectorAsync(new() { Instrument = instrument, Group = group }, options);
        await client.AlignTwoTargetsWithAxisWcfXAsync(new()
        {
            Instrument = instrument,
            FirstPointOnAxis = new() { CollectionName = "Points", GroupName = "Axis", TargetName = "P1" },
            SecondPointOnAxis = new() { CollectionName = "Points", GroupName = "Axis", TargetName = "P2" },
            InitialMeasuredGroup = group,
            RotationalTolerance = new() { LowZ = new() { Enabled = true, Value = 0.1 } }
        }, options);
        await client.LocateInstrumentRefTieInAsync(new()
        {
            Instrument = instrument,
            ReferenceGroup = new() { CollectionName = "Points", ObjectName = "Reference" },
            ActualsGroup = group
        }, options);

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            worker.Commands[0].InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(new WorkerToleranceLimit(true, 0.1),
            worker.Commands[1].InputArguments[4].RequireValue<WorkerToleranceVectorOptionsValue>().LowZ);
    }

    private sealed class InstrumentWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
