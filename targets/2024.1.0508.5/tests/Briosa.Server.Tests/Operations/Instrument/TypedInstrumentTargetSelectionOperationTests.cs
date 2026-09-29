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

public sealed class TypedInstrumentTargetSelectionOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.get_instrument_target_status",
        "instrument_operations.set_instrument_group_and_target",
        "instrument_operations.set_instrument_targeting"
    ];

    [Fact]
    public void CommandsPreserveBindingsDefaultsAndTypedStatusShape()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 3 };
        var status = GetInstrumentTargetStatusOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal("SetColInstIdArg", Assert.Single(status.InputArguments).SdkBinding);
        Assert.Equal(["GetBoolArg", "GetStringArg", "GetIntegerArg", "GetIntegerArg"],
            status.OutputArguments.Select(argument => argument.SdkBinding));

        var point = new Api.PointName { CollectionName = "Points", GroupName = "Nominal", TargetName = "P1" };
        var setPoint = SetInstrumentGroupAndTargetOperation.CreateCommand(new()
        {
            Instrument = instrument,
            Point = point
        });
        Assert.Equal(["SetColInstIdArg", "SetPointNameArg"],
            setPoint.InputArguments.Select(argument => argument.SdkBinding));

        var targeting = SetInstrumentTargetingOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(string.Empty, targeting.InputArguments[1].RequireValue<WorkerTextValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedTargetSelectionRoutes()
    {
        var worker = new TargetSelectionWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 3 };
        var point = new Api.PointName { CollectionName = "Points", GroupName = "Nominal", TargetName = "P1" };

        var status = await client.GetInstrumentTargetStatusAsync(new() { Instrument = instrument }, options);
        await client.SetInstrumentGroupAndTargetAsync(new() { Instrument = instrument, Point = point }, options);
        await client.SetInstrumentTargetingAsync(new() { Instrument = instrument, TargetingName = "Reflector" }, options);

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.True(status.Status.IsLocked);
        Assert.Equal("Reflector", status.Status.Name);
        Assert.Equal(6, status.Status.NumberOfFaces);
        Assert.Equal(2, status.Status.LockedFace);
        Assert.Equal("Reflector", worker.Commands[2].InputArguments[1].RequireValue<WorkerTextValue>().Value);
    }

    private sealed class TargetSelectionWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId == "instrument_operations.get_instrument_target_status"
                ?
                [
                    new WorkerRetrievedOutput("Is Locked?", WorkerMpValueKind.Logical,
                        new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("Name", WorkerMpValueKind.Text, new WorkerTextValue("Reflector")),
                    new WorkerRetrievedOutput("Number of Faces", WorkerMpValueKind.WholeNumber,
                        new WorkerIntegerValue(6)),
                    new WorkerRetrievedOutput("Locked Face", WorkerMpValueKind.WholeNumber,
                        new WorkerIntegerValue(2))
                ]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
