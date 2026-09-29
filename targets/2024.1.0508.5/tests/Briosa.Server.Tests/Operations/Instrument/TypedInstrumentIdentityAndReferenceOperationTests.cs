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

public sealed class TypedInstrumentIdentityAndReferenceOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.add_new_instrument",
        "instrument_operations.delete_instrument",
        "instrument_operations.get_instrument_id_from_name",
        "instrument_operations.get_last_instrument_index",
        "instrument_operations.move_instrument_to_another_collection",
        "instrument_operations.rename_instrument",
        "instrument_operations.associate_objects_with_instrument",
        "instrument_operations.disassociate_objects_from_instrument",
        "instrument_operations.make_collection_object_name_ref_list_from_objects_associated_with_instruments",
        "instrument_operations.get_instruments_with_observations_on_target",
        "instrument_operations.get_targets_measured_by_instrument"
    ];

    [Fact]
    public void InstrumentIdentityAndReferenceCommandsAreTypedAndKeepDefaults()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var add = AddNewInstrumentOperation.CreateCommand(new()
        {
            InstrumentType = new Api.InstrumentTypeName { Value = "PMT Arm 4m 7 dof" }
        });
        Assert.Equal("PMT Arm 4m 7 dof", add.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Single(add.OutputArguments);

        var delete = DeleteInstrumentOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal([false, true], delete.InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerBooleanValue>().Value));
        Assert.Contains("destructive", DeleteInstrumentOperation.Descriptor.RiskFlags);
        Assert.Throws<ArgumentException>(() => DeleteInstrumentOperation.CreateCommand(new()));

        var lookup = GetInstrumentIdFromNameOperation.CreateCommand(new() { Name = "Tracker A" });
        Assert.Equal("Tracker A", lookup.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Empty(GetLastInstrumentIndexOperation.CreateCommand(new()).InputArguments);
        var move = MoveInstrumentToAnotherCollectionOperation.CreateCommand(new()
        {
            Instrument = instrument,
            CollectionName = new() { Name = "Inspection" }
        });
        Assert.Equal(new WorkerTextValue("Inspection"), move.InputArguments[1].RequireValue<WorkerTextValue>());
        var rename = RenameInstrumentOperation.CreateCommand(new() { Instrument = instrument, NewName = "Tracker B" });
        Assert.Equal("Tracker B", rename.InputArguments[1].RequireValue<WorkerTextValue>().Value);

        var objectName = new Api.CollectionObjectName { CollectionName = "Models", ObjectName = "Part" };
        var associate = AssociateObjectsWithInstrumentOperation.CreateCommand(new()
        {
            Instrument = instrument,
            Objects = { objectName }
        });
        Assert.Single(associate.InputArguments[1].RequireValue<WorkerCollectionObjectNameListValue>().Values);
        var disassociate = DisassociateObjectsFromInstrumentOperation.CreateCommand(new() { Objects = { objectName } });
        Assert.Single(disassociate.InputArguments[0].RequireValue<WorkerCollectionObjectNameListValue>().Values);

        var makeList = MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstrumentsOperation.CreateCommand(new()
        {
            Instruments = { instrument }
        });
        Assert.Single(makeList.InputArguments[0].RequireValue<WorkerCollectionInstrumentIdListValue>().Values);
        var point = new Api.PointName { CollectionName = "Points", GroupName = "Nominal", TargetName = "P1" };
        var observed = GetInstrumentsWithObservationsOnTargetOperation.CreateCommand(new() { Point = point });
        Assert.Equal("P1", observed.InputArguments[0].RequireValue<WorkerPointNameValue>().TargetName);
        var targets = GetTargetsMeasuredByInstrumentOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal(instrument.InstrumentId,
            targets.InputArguments[0].RequireValue<WorkerCollectionInstrumentIdValue>().InstrumentId);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedInstrumentIdentityAndReferenceRoutes()
    {
        var worker = new InstrumentWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var objectName = new Api.CollectionObjectName { CollectionName = "Models", ObjectName = "Part" };
        var point = new Api.PointName { CollectionName = "Points", GroupName = "Nominal", TargetName = "P1" };

        var added = await client.AddNewInstrumentAsync(new()
        {
            InstrumentType = new Api.InstrumentTypeName { Value = "PMT Arm 4m 7 dof" }
        }, options);
        await client.DeleteInstrumentAsync(new() { Instrument = instrument, PromptUserToConfirm = true, KeepResultingPoints = false }, options);
        var found = await client.GetInstrumentIdFromNameAsync(new() { Name = "Tracker A" }, options);
        var last = await client.GetLastInstrumentIndexAsync(new(), options);
        await client.MoveInstrumentToAnotherCollectionAsync(new()
        {
            Instrument = instrument,
            CollectionName = new() { Name = "Inspection" }
        }, options);
        await client.RenameInstrumentAsync(new() { Instrument = instrument, NewName = "Tracker B" }, options);
        await client.AssociateObjectsWithInstrumentAsync(new() { Instrument = instrument, Objects = { objectName } }, options);
        await client.DisassociateObjectsFromInstrumentAsync(new() { Objects = { objectName } }, options);
        var associated = await client.MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstrumentsAsync(new()
        {
            Instruments = { instrument }
        }, options);
        var observers = await client.GetInstrumentsWithObservationsOnTargetAsync(new() { Point = point }, options);
        var targets = await client.GetTargetsMeasuredByInstrumentAsync(new() { Instrument = instrument }, options);

        Assert.Equal(1, added.InstrumentAdded.InstrumentId);
        Assert.Equal(2, found.Instrument.InstrumentId);
        Assert.Equal(7, last.InstrumentIndex);
        Assert.Equal(3, last.Instrument.InstrumentId);
        Assert.Equal("Part", Assert.Single(associated.Objects).ObjectName);
        Assert.Equal([11, 12], observers.Instruments.Select(value => value.InstrumentId));
        Assert.Equal("P2", Assert.Single(targets.Targets).TargetName);
        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal([true, false], worker.Commands[1].InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerBooleanValue>().Value));
        Assert.Equal("SetColInstIdRefListArg", worker.Commands[8].InputArguments[0].SdkBinding);
    }

    private sealed class InstrumentWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "instrument_operations.add_new_instrument" =>
                    [new WorkerRetrievedOutput("Instrument Added (result)", WorkerMpValueKind.CollectionInstrumentId,
                        new WorkerCollectionInstrumentIdValue("Trackers", 1))],
                "instrument_operations.get_instrument_id_from_name" =>
                    [new WorkerRetrievedOutput("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                        new WorkerCollectionInstrumentIdValue("Trackers", 2))],
                "instrument_operations.get_last_instrument_index" =>
                    [
                        new WorkerRetrievedOutput("Instrument ID", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(7)),
                        new WorkerRetrievedOutput("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                            new WorkerCollectionInstrumentIdValue("Trackers", 3))
                    ],
                "instrument_operations.make_collection_object_name_ref_list_from_objects_associated_with_instruments" =>
                    [new WorkerRetrievedOutput("Resultant Collection Object Name List", WorkerMpValueKind.CollectionObjectNameList,
                        new WorkerCollectionObjectNameListValue([
                            new WorkerCollectionObjectNameValue("Models", "Part", WorkerObjectTypeValue.Any)
                        ]))],
                "instrument_operations.get_instruments_with_observations_on_target" =>
                    [new WorkerRetrievedOutput("Resultant Collection Instrument Reference List", WorkerMpValueKind.CollectionInstrumentIdList,
                        new WorkerCollectionInstrumentIdListValue([
                            new WorkerCollectionInstrumentIdValue("Trackers", 11),
                            new WorkerCollectionInstrumentIdValue("Trackers", 12)
                        ]))],
                "instrument_operations.get_targets_measured_by_instrument" =>
                    [new WorkerRetrievedOutput("Points Measured by Instrument", WorkerMpValueKind.PointNameList,
                        new WorkerPointNameListValue([
                            new WorkerPointNameValue("Points", "Measured", "P2")
                        ]))],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}