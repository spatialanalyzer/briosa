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

public sealed class TypedInstrumentGuidanceOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.initiate_servo_guide",
        "instrument_operations.issue_instrument_actuator_command",
        "instrument_operations.jump_instrument_to_new_location",
        "instrument_operations.move_objects_in_6d_using_instrument_updates",
        "instrument_operations.point_at_target"
    ];

    [Fact]
    public void InstrumentGuidanceCommandsPreserveOptionalValuesAndDefaults()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var point = new Api.PointName { CollectionName = "Work", GroupName = "Nominal", TargetName = "P1" };
        var servo = InitiateServoGuideOperation.CreateCommand(new()
        {
            Instrument = instrument,
            NominalPoints = { point }
        });
        Assert.Equal(string.Empty, servo.InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(string.Empty, servo.InputArguments[3].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0, servo.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(string.Empty, IssueInstrumentActuatorCommandOperation.CreateCommand(new() { Instrument = instrument })
            .InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.False(JumpInstrumentToNewLocationOperation.CreateCommand(new() { LiveInstrument = instrument })
            .InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        var moved = MoveObjectsIn6dUsingInstrumentUpdatesOperation.CreateCommand(new()
        {
            Instrument = instrument,
            ObjectsToMove = { new Api.CollectionObjectName { CollectionName = "Work", ObjectName = "Part" } }
        });
        Assert.Equal(string.Empty, moved.InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(2, PointAtTargetOperation.CreateCommand(new() { Instrument = instrument, TargetId = point }).InputArguments.Count);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedInstrumentGuidanceRoutes()
    {
        var worker = new InstrumentWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var point = new Api.PointName { CollectionName = "Work", GroupName = "Nominal", TargetName = "P1" };

        await client.InitiateServoGuideAsync(new()
        {
            Instrument = instrument,
            NominalPoints = { point },
            GroupNameSuffix = "Nominal",
            TargetNameSuffix = "Servo",
            Tolerance = 0.25
        }, options);
        await client.IssueInstrumentActuatorCommandAsync(new() { Instrument = instrument, Command = "Home" }, options);
        await client.JumpInstrumentToNewLocationAsync(new() { LiveInstrument = instrument, HidePreviousInstrument = true }, options);
        await client.MoveObjectsIn6dUsingInstrumentUpdatesAsync(new()
        {
            Instrument = instrument,
            ObjectsToMove = { new Api.CollectionObjectName { CollectionName = "Work", ObjectName = "Part" } },
            MeasurementMode = "Continuous"
        }, options);
        await client.PointAtTargetAsync(new()
        {
            Instrument = instrument,
            TargetId = point,
            HtmlPromptFile = new Api.FileReference { Path = "prompt.html", EmbeddedFile = true }
        }, options);

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(0.25, worker.Commands[0].InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("Home", worker.Commands[1].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.True(worker.Commands[2].InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("Continuous", worker.Commands[3].InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(("prompt.html", true),
            (worker.Commands[4].InputArguments[2].RequireValue<WorkerFileReferenceValue>().Path,
                worker.Commands[4].InputArguments[2].RequireValue<WorkerFileReferenceValue>().EmbeddedFile));
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
