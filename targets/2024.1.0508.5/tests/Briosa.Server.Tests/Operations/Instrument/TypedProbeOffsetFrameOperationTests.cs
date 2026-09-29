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

public sealed class TypedProbeOffsetFrameOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.set_probe_offset_frame_offline",
        "instrument_operations.set_probe_offset_frame_online"
    ];

    [Fact]
    public void ProbeOffsetOperationsAreRegisteredAsTypedOperations()
    {
        foreach (var operationId in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == operationId);
        }
    }

    [Fact]
    public void CommandsPreserveRequiredFramesAndDocumentedDefaults()
    {
        var offline = SetProbeOffsetFrameOfflineOperation.CreateCommand(new()
        {
            Instrument = Instrument(),
            RawMeasuredFrame = Frame("Raw"),
            OffsetFrame = Frame("Offset")
        });
        Assert.Equal(string.Empty, offline.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0, offline.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal("Raw", offline.InputArguments[3].RequireValue<WorkerCollectionObjectNameValue>().ObjectName);
        Assert.Equal(WorkerObjectTypeValue.Frame,
            offline.InputArguments[3].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);

        var online = SetProbeOffsetFrameOnlineOperation.CreateCommand(new()
        {
            Instrument = Instrument(),
            OffsetFrame = Frame("Offset")
        });
        Assert.Equal(string.Empty, online.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0, online.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(string.Empty, online.InputArguments[3].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(15, online.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);

        var timeoutOverride = SetProbeOffsetFrameOnlineOperation.CreateCommand(new()
        {
            Instrument = Instrument(),
            ProbeName = "Probe A",
            FaceId = 2,
            MeasureProfileName = "Profile A",
            TimeoutSeconds = 3.5,
            OffsetFrame = Frame("Offset")
        });
        Assert.Equal("Probe A", timeoutOverride.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(2, timeoutOverride.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal("Profile A", timeoutOverride.InputArguments[3].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(3.5, timeoutOverride.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientRoutesBothFrameOperationsThroughFakeWorker()
    {
        var worker = new ProbeOffsetWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);

        await client.SetProbeOffsetFrameOfflineAsync(new()
        {
            Instrument = Instrument(),
            RawMeasuredFrame = Frame("Raw"),
            OffsetFrame = Frame("Offline Offset")
        });
        await client.SetProbeOffsetFrameOnlineAsync(new()
        {
            Instrument = Instrument(),
            ProbeName = "Probe A",
            FaceId = 2,
            MeasureProfileName = "Profile A",
            TimeoutSeconds = 3.5,
            OffsetFrame = Frame("Online Offset")
        });

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal("Raw", worker.Commands[0].InputArguments[3]
            .RequireValue<WorkerCollectionObjectNameValue>().ObjectName);
        Assert.Equal("Offline Offset", worker.Commands[0].InputArguments[4]
            .RequireValue<WorkerCollectionObjectNameValue>().ObjectName);
        Assert.Equal("Online Offset", worker.Commands[1].InputArguments[5]
            .RequireValue<WorkerCollectionObjectNameValue>().ObjectName);
    }

    private static Api.CollectionInstrumentId Instrument() => new()
    {
        CollectionName = "Trackers",
        InstrumentId = 4
    };

    private static Api.CollectionObjectName Frame(string name) => new()
    {
        CollectionName = "Frames",
        ObjectName = name,
        ObjectType = Api.ObjectType.Frame
    };

    private sealed class ProbeOffsetWorker : IWorkerCommandExecutor
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
