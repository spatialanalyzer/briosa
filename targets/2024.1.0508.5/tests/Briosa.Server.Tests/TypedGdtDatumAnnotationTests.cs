using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGdtDatumAnnotationTests
{
    [Fact]
    public async Task GeneratedClientMapsDatumAnnotationDefaultsAndRelationshipLists()
    {
        var worker = new DatumAnnotationWorker();
        var grpcHost = await GrpcTestHost.StartAsync<GdtOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.GdtOperations.GdtOperationsClient(channel);
        var request = new Api.MakeGdtDatumAnnotationRequest
        {
            AuxiliaryObject = new() { CollectionName = "C", ObjectName = "Aux" },
            AuxiliaryGeometryRelationship = new() { CollectionName = "C", ItemName = "AuxRel" }
        };
        request.Objects.Add(new Api.CollectionObjectName { CollectionName = "C", ObjectName = "Part" });
        request.GeometryRelationships.Add(new Api.CollectionItemName
            { CollectionName = "C", ItemName = "Rel", ItemType = Api.ItemType.Relationship });

        var result = await client.MakeGdtDatumAnnotationAsync(request, deadline: DateTime.UtcNow.AddSeconds(20));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        var command = Assert.Single(worker.Commands);
        Assert.Equal(8, command.InputArguments.Count);
        Assert.Equal("", command.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("Part", command.InputArguments[1]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values.Single().ObjectName);
        var geometryRelationships = command.InputArguments[2]
            .RequireValue<WorkerCollectionItemNameListValue>().Values;
        Assert.Equal("Rel", Assert.Single(geometryRelationships).ItemName);
        Assert.Equal(WorkerItemTypeValue.Relationship, geometryRelationships[0].ItemType);
        Assert.Equal("", command.InputArguments[3].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(WorkerItemTypeValue.Relationship,
            command.InputArguments[5].RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.False(command.InputArguments[6].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(command.InputArguments[7].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(
            ["SetStringArg", "SetCollectionObjectNameRefListArg", "SetCollectionObjectNameRefListArg",
                "SetStringArg", "SetCollectionObjectNameArg2", "SetCollectionObjectNameArg2", "SetBoolArg", "SetBoolArg"],
            command.InputArguments.Select(input => input.SdkBinding));
    }

    [Fact]
    public void RequiredReferencesAndRegistryAreEnforced()
    {
        Assert.Throws<ArgumentException>(() => MakeGdtDatumAnnotationOperation.CreateCommand(new()));
        var id = MakeGdtDatumAnnotationOperation.Descriptor.OperationId;
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
    }

    private sealed class DatumAnnotationWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1,
                Array.Empty<WorkerRetrievedOutput>(), "completed");
            return Task.FromResult(new WorkerExecutionOutcome(
                WorkerExecutionStatus.Completed, WorkerExecutionDisposition.Completed,
                execution, null, "completed", 1));
        }
    }
}
