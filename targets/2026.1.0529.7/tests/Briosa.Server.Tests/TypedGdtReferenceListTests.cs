using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGdtReferenceListTests
{
    [Fact]
    public async Task GeneratedClientUsesTypedReferenceListRoute()
    {
        var worker = new ReferenceListWorker();
        var grpcHost = await GrpcTestHost.StartAsync<GdtOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.GdtOperations.GdtOperationsClient(channel);

        var result = await client.MakeAnnotationRefListWildcardSelectionAsync(new(),
            deadline: DateTime.UtcNow.AddSeconds(20));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(Api.ItemType.Annotation, Assert.Single(result.Annotations).ItemType);
        Assert.Equal(["*", "*"], worker.Command!.InputArguments
            .Select(item => item.RequireValue<WorkerTextValue>().Value));
        Assert.Equal(["SetStringArg", "SetStringArg"],
            worker.Command.InputArguments.Select(item => item.SdkBinding));
        Assert.Equal("GetCollectionObjectNameRefListArg",
            Assert.Single(worker.Command.OutputArguments).SdkBinding);
    }

    [Fact]
    public void RequiredCollectionAndMigratedRegistryAreEnforced()
    {
        Assert.Throws<ArgumentException>(() =>
            MakeDatumRefListFromCollectionOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() =>
            MakeFeatureChecksOperation.CreateCommand(new()
                { Collection = new Api.CollectionName { Name = " " } }));

        var ids = new[]
        {
            MakeAnnotationRefListFromCollectionOperation.Descriptor.OperationId,
            MakeAnnotationRefListWildcardSelectionOperation.Descriptor.OperationId,
            MakeDatumRefListFromCollectionOperation.Descriptor.OperationId,
            MakeFeatureCheckRefListFromCollectionOperation.Descriptor.OperationId,
            MakeFeatureCheckReferenceListWildcardSelectionOperation.Descriptor.OperationId,
            MakeFeatureChecksOperation.Descriptor.OperationId
        };
        foreach (var id in ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    private sealed class ReferenceListWorker : IWorkerCommandExecutor
    {
        public WorkerMpCommand? Command { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Command = command;
            var output = new WorkerRetrievedOutput(
                "Resultant Annotation Reference List",
                WorkerMpValueKind.CollectionItemNameList,
                new WorkerCollectionItemNameListValue(
                    [new WorkerCollectionItemNameValue("Collection", "A", WorkerItemTypeValue.Annotation)]));
            var execution = WorkerMpExecutionResult.FromEvidence(
                true, true, true, 2, 1, [output], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(
                WorkerExecutionStatus.Completed, WorkerExecutionDisposition.Completed,
                execution, null, "completed", 1));
        }
    }
}
