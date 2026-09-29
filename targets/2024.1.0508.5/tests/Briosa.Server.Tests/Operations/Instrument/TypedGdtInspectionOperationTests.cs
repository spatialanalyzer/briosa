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

public sealed class TypedGdtInspectionOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.start_gdt_inspection",
        "instrument_operations.start_gdt_inspection_design",
        "instrument_operations.start_gdt_inspection_rehearse"
    ];

    [Fact]
    public void InspectionFiltersUseTheDocumentedMpLiteralsAndDefaults()
    {
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 2 };
        var collection = new Api.CollectionName { Name = "Checks" };
        var defaultFilter = StartGdtInspectionOperation.CreateCommand(new() { Instrument = instrument, Collection = collection });
        Assert.Equal("ALL", defaultFilter.InputArguments[2].RequireValue<WorkerTextValue>().Value);

        Assert.Equal("CHECKS", StartGdtInspectionDesignOperation.CreateCommand(new()
        {
            Collection = collection,
            Filter = Api.InspectionFilter.Checks
        }).InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("DATUMS", StartGdtInspectionRehearseOperation.CreateCommand(new()
        {
            Collection = collection,
            Filter = Api.InspectionFilter.Datums
        }).InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => StartGdtInspectionDesignOperation.CreateCommand(new()
        {
            Collection = collection,
            Filter = (Api.InspectionFilter)99
        }));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedGdtInspectionRoutes()
    {
        var worker = new InspectionWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 2 };
        var collection = new Api.CollectionName { Name = "Checks" };

        await client.StartGdtInspectionAsync(new() { Instrument = instrument, Collection = collection }, options);
        await client.StartGdtInspectionDesignAsync(new() { Collection = collection, Filter = Api.InspectionFilter.Checks }, options);
        await client.StartGdtInspectionRehearseAsync(new() { Collection = collection, Filter = Api.InspectionFilter.Datums }, options);

        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal("ALL", worker.Commands[0].InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("CHECKS", worker.Commands[1].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("DATUMS", worker.Commands[2].InputArguments[1].RequireValue<WorkerTextValue>().Value);
    }

    private sealed class InspectionWorker : IWorkerCommandExecutor
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
