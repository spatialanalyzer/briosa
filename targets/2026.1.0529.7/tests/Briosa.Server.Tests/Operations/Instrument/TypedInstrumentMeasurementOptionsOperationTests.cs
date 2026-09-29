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

public sealed class TypedInstrumentMeasurementOptionsOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.fabricate_observations",
        "instrument_operations.get_estimated_scan_time",
        "instrument_operations.get_instrument_targets_and_mode_profiles"
    ];

    [Fact]
    public void CommandsPreserveBindingsDefaultsAndListOutputs()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 3 };
        var group = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Nominal" };
        var fabricate = FabricateObservationsOperation.CreateCommand(new() { Instrument = instrument, PointGroup = group });
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            fabricate.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.False(fabricate.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(fabricate.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0d, fabricate.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(1_000_000d, fabricate.InputArguments[5].RequireValue<WorkerDoubleValue>().Value);

        var scanTime = GetEstimatedScanTimeOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(string.Empty, scanTime.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("GetDoubleArg", Assert.Single(scanTime.OutputArguments).SdkBinding);

        var options = GetInstrumentTargetsAndModeProfilesOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(["GetStringRefListArg", "GetStringRefListArg"],
            options.OutputArguments.Select(argument => argument.SdkBinding));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedMeasurementOptionsRoutes()
    {
        var worker = new MeasurementOptionsWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 3 };

        await client.FabricateObservationsAsync(new()
        {
            Instrument = instrument,
            PointGroup = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Nominal" }
        }, options);
        var scanTime = await client.GetEstimatedScanTimeAsync(new() { Instrument = instrument }, options);
        var targetOptions = await client.GetInstrumentTargetsAndModeProfilesAsync(new() { Instrument = instrument }, options);

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(17.25, scanTime.EstimatedScanTime);
        Assert.Equal(["Prism", "Reflector"], targetOptions.ModeProfiles);
        Assert.Equal(["P1", "P2"], targetOptions.TargetNames);
        Assert.Equal(string.Empty, worker.Commands[1].InputArguments[1].RequireValue<WorkerTextValue>().Value);
    }

    private sealed class MeasurementOptionsWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "instrument_operations.get_estimated_scan_time" =>
                    [new WorkerRetrievedOutput("Estimated Scan Time", WorkerMpValueKind.FloatingPoint,
                        new WorkerDoubleValue(17.25))],
                "instrument_operations.get_instrument_targets_and_mode_profiles" =>
                [
                    new WorkerRetrievedOutput("Mode/Profile", WorkerMpValueKind.StringList,
                        new WorkerStringListValue(["Prism", "Reflector"])),
                    new WorkerRetrievedOutput("Target Names", WorkerMpValueKind.StringList,
                        new WorkerStringListValue(["P1", "P2"]))
                ],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
