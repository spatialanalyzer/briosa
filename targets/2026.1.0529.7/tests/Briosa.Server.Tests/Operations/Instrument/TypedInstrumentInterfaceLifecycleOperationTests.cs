using Briosa.Server.Operations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedInstrumentInterfaceLifecycleOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.activate_deactivate_instrument_toolbar",
        "instrument_operations.dock_instrument_interface",
        "instrument_operations.start_instrument_interface",
        "instrument_operations.start_theodolite_interface",
        "instrument_operations.stop_instrument_interface",
        "instrument_operations.verify_instrument_connection",
        "instrument_operations.watch_instrument",
        "instrument_operations.instrument_operational_check"
    ];

    [Fact]
    public void InstrumentInterfaceCommandsKeepOptionalFileAndBooleanDefaults()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var activate = ActivateDeactivateInstrumentToolbarOperation.CreateCommand(new() { Instrument = instrument });
        Assert.False(activate.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        var dock = DockInstrumentInterfaceOperation.CreateCommand(new() { Instrument = instrument });
        Assert.False(dock.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        var start = StartInstrumentInterfaceOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(5, start.InputArguments.Count);
        Assert.DoesNotContain(start.InputArguments, argument => argument.Name == "Device IP Address (optional)");
        Assert.Equal(0, start.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.All(start.InputArguments.Skip(1).Where(argument => argument.Kind == WorkerMpValueKind.Logical), argument =>
            Assert.False(argument.RequireValue<WorkerBooleanValue>().Value));
        var startWithAddress = StartInstrumentInterfaceOperation.CreateCommand(new()
        {
            Instrument = instrument,
            DeviceIpAddress = "127.0.0.1"
        });
        Assert.Equal("127.0.0.1", startWithAddress.InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("SetStringArg", startWithAddress.InputArguments[2].SdkBinding);

        var theodolite = StartTheodoliteInterfaceOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(4, theodolite.InputArguments.Count);
        Assert.Equal(0, theodolite.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.DoesNotContain(theodolite.InputArguments, argument => argument.Name == "Device IP Address (optional)");
        Assert.Single(StopInstrumentInterfaceOperation.CreateCommand(new() { Instrument = instrument }).InputArguments);

        var watch = WatchInstrumentOperation.CreateCommand(new()
        {
            Instrument = instrument,
            WatchWindowProperties = new() { CollectionName = "Windows", ObjectName = "Properties" }
        });
        Assert.Equal(7, watch.InputArguments.Count);
        Assert.Throws<ArgumentException>(() => WatchInstrumentOperation.CreateCommand(new() { Instrument = instrument }));
        Assert.Equal(WorkerObjectTypeValue.Any,
            watch.InputArguments[2].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        var check = InstrumentOperationalCheckOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal("", check.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Single(VerifyInstrumentConnectionOperation.OutputContracts);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedInstrumentInterfaceLifecycleRoutes()
    {
        var worker = new InstrumentWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };

        await client.ActivateDeactivateInstrumentToolbarAsync(new() { Instrument = instrument, DeactivateToolbar = true }, options);
        await client.DockInstrumentInterfaceAsync(new() { Instrument = instrument, DockInterface = true }, options);
        await client.StartInstrumentInterfaceAsync(new()
        {
            Instrument = instrument,
            InitializeAtStartup = true,
            DeviceIpAddress = "127.0.0.1",
            InterfaceType = 2,
            RunInSimulation = true,
            AllowStartWithoutInitializationRequirements = true
        }, options);
        await client.StartTheodoliteInterfaceAsync(new()
        {
            Instrument = instrument,
            TheodoliteType = "Tracker",
            CommPort = 4,
            Simulation = true
        }, options);
        await client.StopInstrumentInterfaceAsync(new() { Instrument = instrument }, options);
        var connection = await client.VerifyInstrumentConnectionAsync(new() { Instrument = instrument }, options);
        await client.WatchInstrumentAsync(new()
        {
            Instrument = instrument,
            WatchWindowProperties = new() { CollectionName = "Windows", ObjectName = "Properties" },
            WindowTopLeftX = 10,
            WindowTopLeftY = 20,
            WindowWidth = 800,
            WindowHeight = 600
        }, options);
        await client.InstrumentOperationalCheckAsync(new() { Instrument = instrument, CheckType = "Interface" }, options);

        Assert.True(connection.Connected);
        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal("127.0.0.1", worker.Commands[2].InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(4, worker.Commands[3].InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal("SetCollectionObjectNameArg2", worker.Commands[6].InputArguments[2].SdkBinding);
    }

    private sealed class InstrumentWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId == "instrument_operations.verify_instrument_connection"
                ? [new WorkerRetrievedOutput("Connected?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true))]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}