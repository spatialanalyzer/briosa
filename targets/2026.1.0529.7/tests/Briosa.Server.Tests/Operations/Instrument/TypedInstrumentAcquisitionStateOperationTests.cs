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

public sealed class TypedInstrumentAcquisitionStateOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.get_current_trapping_status",
        "instrument_operations.get_instrument_measurement_mode_profile",
        "instrument_operations.set_instrument_measurement_mode_profile",
        "instrument_operations.stop_active_measurement_mode",
        "instrument_operations.wait_for_trapping_to_complete"
    ];

    [Fact]
    public void AcquisitionCommandsPreserveBindingsDefaultsAndCatalogRegistration()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var getProfile = GetInstrumentMeasurementModeProfileOperation.CreateCommand(
            new() { Instrument = instrument });
        Assert.Equal("SetColInstIdArg", Assert.Single(getProfile.InputArguments).SdkBinding);
        Assert.Equal("GetStringArg", Assert.Single(getProfile.OutputArguments).SdkBinding);
        var status = GetCurrentTrappingStatusOperation.CreateCommand(new());
        Assert.Empty(status.InputArguments);
        Assert.Equal(
            ["GetBoolArg", "GetCollectionObjectNameArg", "GetColInstIdArg"],
            status.OutputArguments.Select(argument => argument.SdkBinding));
        Assert.Equal(["status", "status", "status"],
            GetCurrentTrappingStatusOperation.OutputContracts.Select(output => output.FieldName));

        var setProfile = SetInstrumentMeasurementModeProfileOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(["SetColInstIdArg", "SetStringArg"],
            setProfile.InputArguments.Select(argument => argument.SdkBinding));
        Assert.Equal(string.Empty, setProfile.InputArguments[1].RequireValue<WorkerTextValue>().Value);

        var stop = StopActiveMeasurementModeOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal("SetColInstIdArg", Assert.Single(stop.InputArguments).SdkBinding);
        Assert.Empty(WaitForTrappingToCompleteOperation.CreateCommand(new()).InputArguments);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedAcquisitionStateRoutes()
    {
        var worker = new AcquisitionStateWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };

        var status = await client.GetCurrentTrappingStatusAsync(new(), options);
        var profile = await client.GetInstrumentMeasurementModeProfileAsync(
            new() { Instrument = instrument }, options);
        await client.SetInstrumentMeasurementModeProfileAsync(new() { Instrument = instrument }, options);
        await client.StopActiveMeasurementModeAsync(new() { Instrument = instrument }, options);
        await client.WaitForTrappingToCompleteAsync(new(), options);

        Assert.True(status.Status.Active);
        Assert.Equal("Checks", status.Status.FocusedItem.CollectionName);
        Assert.Equal("C1", status.Status.FocusedItem.ItemName);
        Assert.Equal(4, status.Status.Instrument.InstrumentId);
        Assert.Equal("Reflector", profile.ModeProfile);
        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(string.Empty, worker.Commands[2].InputArguments[1].RequireValue<WorkerTextValue>().Value);
    }

    private sealed class AcquisitionStateWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "instrument_operations.get_instrument_measurement_mode_profile" =>
                    [new WorkerRetrievedOutput("Mode/Profile", WorkerMpValueKind.Text,
                        new WorkerTextValue("Reflector"))],
                "instrument_operations.get_current_trapping_status" =>
                [
                    new WorkerRetrievedOutput("Trapping Active?", WorkerMpValueKind.Logical,
                        new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("Relationship / Feature Check Name", WorkerMpValueKind.CollectionItemName,
                        new WorkerCollectionItemNameValue("Checks", "C1", WorkerItemTypeValue.Any)),
                    new WorkerRetrievedOutput("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                        new WorkerCollectionInstrumentIdValue("Trackers", 4))
                ],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
