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

public sealed class TypedInstrumentPositionAndProjectorOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.get_current_instrument_position_update",
        "instrument_operations.set_alignment_projector"
    ];

    [Fact]
    public void PositionAndProjectorCommandsPreserveEnumAndEmptyDefaults()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var defaultPosition = GetCurrentInstrumentPositionUpdateOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal("Instrument Base", defaultPosition.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.False(defaultPosition.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        var unspecifiedPosition = GetCurrentInstrumentPositionUpdateOperation.CreateCommand(new()
        {
            Instrument = instrument,
            ReportingFrame = Api.InstrumentPositionReportingFrame.Unspecified
        });
        Assert.Equal("Instrument Base", unspecifiedPosition.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(["GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetStringArg"],
            defaultPosition.OutputArguments.Select(argument => argument.SdkBinding));

        var worldPosition = GetCurrentInstrumentPositionUpdateOperation.CreateCommand(new()
        {
            Instrument = instrument,
            ReportingFrame = Api.InstrumentPositionReportingFrame.World,
            PolarCoordinates = true
        });
        Assert.Equal("World", worldPosition.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.True(worldPosition.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => GetCurrentInstrumentPositionUpdateOperation.CreateCommand(new()
        {
            Instrument = instrument,
            ReportingFrame = (Api.InstrumentPositionReportingFrame)99
        }));

        var projector = SetAlignmentProjectorOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal("", projector.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("", projector.InputArguments[2].RequireValue<WorkerTextValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedPositionAndProjectorRoutes()
    {
        var worker = new InstrumentWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };

        var result = await client.GetCurrentInstrumentPositionUpdateAsync(new()
        {
            Instrument = instrument,
            ReportingFrame = Api.InstrumentPositionReportingFrame.Working,
            PolarCoordinates = true
        }, options);
        await client.SetAlignmentProjectorAsync(new() { Instrument = instrument }, options);

        Assert.Equal(1.25, result.XOrR);
        Assert.Equal(2.5, result.YOrTheta);
        Assert.Equal(3.75, result.ZOrPhi);
        Assert.Equal(4.5, result.TimeSinceUpdate);
        Assert.Equal("Approximate", result.Timestamp);
        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal("Working", worker.Commands[0].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("", worker.Commands[1].InputArguments[1].RequireValue<WorkerTextValue>().Value);
    }

    private sealed class InstrumentWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId == "instrument_operations.get_current_instrument_position_update"
                ?
                [
                    new WorkerRetrievedOutput("X / R", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1.25)),
                    new WorkerRetrievedOutput("Y / Theta (Degrees)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2.5)),
                    new WorkerRetrievedOutput("Z / Phi (Degrees)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(3.75)),
                    new WorkerRetrievedOutput("Time Since Update (sec)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(4.5)),
                    new WorkerRetrievedOutput("Timestamp (Approximate)", WorkerMpValueKind.Text, new WorkerTextValue("Approximate"))
                ]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
