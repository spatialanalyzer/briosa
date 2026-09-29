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

public sealed class TypedInstrumentObservationConfigurationOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.set_observation_collimation_shot_options",
        "instrument_operations.set_observation_mirror_cube_shot_face",
        "instrument_operations.set_observation_status",
        "instrument_operations.set_remeasure_failed_checks_only"
    ];

    [Fact]
    public void ObservationAndRemeasureCommandsPreserveBindingsAndDefaults()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var point = new Api.PointName { CollectionName = "Points", GroupName = "Nominal", TargetName = "P1" };
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 3 };
        var collimation = SetObservationCollimationShotOptionsOperation.CreateCommand(new()
        {
            Point = point,
            TargetedInstrument = instrument
        });
        Assert.Equal(0, collimation.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(collimation.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);

        var mirror = SetObservationMirrorCubeShotFaceOperation.CreateCommand(new() { Point = point });
        Assert.Equal(0, mirror.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(mirror.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(1, mirror.InputArguments[3].RequireValue<WorkerIntegerValue>().Value);

        var status = SetObservationStatusOperation.CreateCommand(new() { Point = point });
        Assert.Equal(0, status.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(status.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);

        var remeasure = SetRemeasureFailedChecksOnlyOperation.CreateCommand(new()
        {
            Collection = new Api.CollectionName { Name = "Checks" }
        });
        Assert.Equal("SetCollectionNameArg", Assert.Single(remeasure.InputArguments).SdkBinding);
        Assert.Equal("Checks", remeasure.InputArguments[0].RequireValue<WorkerTextValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedObservationConfigurationRoutes()
    {
        var worker = new ObservationConfigurationWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var point = new Api.PointName { CollectionName = "Points", GroupName = "Nominal", TargetName = "P1" };
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 3 };

        await client.SetObservationCollimationShotOptionsAsync(new()
        {
            Point = point, ObservationIndex = 3, IsCollimationShot = true, TargetedInstrument = instrument
        }, options);
        await client.SetObservationMirrorCubeShotFaceAsync(new()
        {
            Point = point, ObservationIndex = 2, IsMirrorCubeShot = true, MirrorCubeShotFace = 6
        }, options);
        await client.SetObservationStatusAsync(new() { Point = point, ObservationIndex = 4, Active = true }, options);
        await client.SetRemeasureFailedChecksOnlyAsync(new()
        {
            Collection = new Api.CollectionName { Name = "Checks" }
        }, options);

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(3, worker.Commands[0].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.True(worker.Commands[0].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(6, worker.Commands[1].InputArguments[3].RequireValue<WorkerIntegerValue>().Value);
        Assert.True(worker.Commands[2].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
    }

    private sealed class ObservationConfigurationWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
