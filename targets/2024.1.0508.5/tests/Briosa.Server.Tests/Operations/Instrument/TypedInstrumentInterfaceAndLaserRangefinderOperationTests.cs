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

public sealed class TypedInstrumentInterfaceAndLaserRangefinderOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.get_instrument_interface_response_timeout",
        "instrument_operations.get_instrument_model",
        "instrument_operations.lr_get_most_recent_snr_info",
        "instrument_operations.lr_hardware_connect",
        "instrument_operations.lr_hardware_disconnect",
        "instrument_operations.lr_verify_hardware_connection",
        "instrument_operations.set_instrument_interface_response_timeout"
    ];

    [Fact]
    public void InterfaceAndLaserRangefinderCommandsPreserveDefaultsAndRegistration()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        Assert.Equal(1.25, GetInstrumentInterfaceResponseTimeoutOperation.CreateResult(Completed(
            new WorkerRetrievedOutput("Resulting Timeout Value (secs)", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(1.25)))).Timeout);
        var model = GetInstrumentModelOperation.CreateResult(Completed(
        [
            new WorkerRetrievedOutput("Name", WorkerMpValueKind.Text, new WorkerTextValue("Tracker A")),
            new WorkerRetrievedOutput("Model", WorkerMpValueKind.Text, new WorkerTextValue("AT960"))
        ]));
        Assert.Equal(("Tracker A", "AT960"), (model.Name, model.Model));

        var connect = LrHardwareConnectOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(string.Empty, connect.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0, connect.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(0, SetInstrumentInterfaceResponseTimeoutOperation.CreateCommand(new() { Instrument = instrument })
            .InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(5, LrGetMostRecentSnrInfoOperation.OutputContracts.Count);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedInterfaceAndLaserRangefinderRoutes()
    {
        var worker = new InstrumentWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };

        var timeout = await client.GetInstrumentInterfaceResponseTimeoutAsync(new() { Instrument = instrument }, options);
        var model = await client.GetInstrumentModelAsync(new() { Instrument = instrument }, options);
        var snr = await client.LrGetMostRecentSnrInfoAsync(new() { Instrument = instrument }, options);
        await client.LrHardwareConnectAsync(new() { Instrument = instrument, Host = "laser.example", Port = 8123 }, options);
        await client.LrHardwareDisconnectAsync(new() { Instrument = instrument }, options);
        var connection = await client.LrVerifyHardwareConnectionAsync(new() { Instrument = instrument }, options);
        await client.SetInstrumentInterfaceResponseTimeoutAsync(new() { Instrument = instrument, Timeout = 2.5 }, options);

        Assert.Equal(1.25, timeout.Timeout);
        Assert.Equal(("Tracker A", "AT960"), (model.Name, model.Model));
        Assert.Equal((18.5, 128, 3, 12.25, 4.75),
            (snr.Info.Snr, snr.Info.SizeOfDataArray, snr.Info.PeakValueIndex, snr.Info.PeakValue, snr.Info.MeasuredRange));
        Assert.True(connection.ConnectedToHardware);
        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal("laser.example", worker.Commands[3].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(8123, worker.Commands[3].InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(2.5, worker.Commands[6].InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
    }

    private static SuccessfulOperationExecution Completed(params WorkerMpOutputValue[] outputs) => new(
        WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed"),
        new Api.MpExecutionDetails());

    private sealed class InstrumentWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "instrument_operations.get_instrument_interface_response_timeout" =>
                    [new WorkerRetrievedOutput("Resulting Timeout Value (secs)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1.25))],
                "instrument_operations.get_instrument_model" =>
                [
                    new WorkerRetrievedOutput("Name", WorkerMpValueKind.Text, new WorkerTextValue("Tracker A")),
                    new WorkerRetrievedOutput("Model", WorkerMpValueKind.Text, new WorkerTextValue("AT960"))
                ],
                "instrument_operations.lr_get_most_recent_snr_info" =>
                [
                    new WorkerRetrievedOutput("SNR", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(18.5)),
                    new WorkerRetrievedOutput("Size of Data Array", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(128)),
                    new WorkerRetrievedOutput("Peak Value Index", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(3)),
                    new WorkerRetrievedOutput("Peak Value (dB)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(12.25)),
                    new WorkerRetrievedOutput("Measured Range (m)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(4.75))
                ],
                "instrument_operations.lr_verify_hardware_connection" =>
                    [new WorkerRetrievedOutput("Connected to Hardware?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true))],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
