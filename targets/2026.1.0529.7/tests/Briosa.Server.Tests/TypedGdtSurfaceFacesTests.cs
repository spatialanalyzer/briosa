using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGdtSurfaceFacesTests
{
    [Fact]
    public async Task GeneratedClientUsesTypedSurfaceFaceRoutes()
    {
        var worker = new SurfaceFaceWorker();
        var grpcHost = await GrpcTestHost.StartAsync<GdtOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.GdtOperations.GdtOperationsClient(channel);

        var fromSurface = await client.MakeSurfaceFaceListFromSurfaceAsync(new()
        {
            Surface = new Api.CollectionObjectName { CollectionName = "C", ObjectName = "S" }
        }, deadline: DateTime.UtcNow.AddSeconds(20));
        Assert.Equal("faces", fromSurface.SurfaceFaces.Value);
        Assert.Equal(WorkerObjectTypeValue.Surface,
            worker.Commands[0].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal("SetCollectionObjectNameArg2", worker.Commands[0].InputArguments[0].SdkBinding);

        var runtime = await client.MakeSurfaceFaceListRuntimeSelectAsync(new(),
            deadline: DateTime.UtcNow.AddSeconds(20));
        Assert.Equal("faces", runtime.SurfaceFaces.Value);
        Assert.Empty(worker.Commands[1].InputArguments);

        var refresh = await client.RefreshDatumsFeatureChecksFromAnnotationsAsync(new()
        {
            Collection = new Api.CollectionName { Name = "C" }
        }, deadline: DateTime.UtcNow.AddSeconds(20));
        Assert.Equal(Api.MpExecutionState.Succeeded, refresh.Execution.State);
        Assert.Equal("SetCollectionNameArg", worker.Commands[2].InputArguments[0].SdkBinding);
    }

    [Fact]
    public void RequiredInputsAndRegistryAreEnforced()
    {
        Assert.Throws<ArgumentException>(() => MakeSurfaceFaceListFromSurfaceOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => RefreshDatumsFeatureChecksFromAnnotationsOperation.CreateCommand(new()));
        foreach (var id in new[]
                 {
                     MakeSurfaceFaceListFromSurfaceOperation.Descriptor.OperationId,
                     MakeSurfaceFaceListRuntimeSelectOperation.Descriptor.OperationId,
                     RefreshDatumsFeatureChecksFromAnnotationsOperation.Descriptor.OperationId
                 })
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    private sealed class SurfaceFaceWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerRetrievedOutput[] outputs = command.OutputArguments.Count == 0
                ? []
                : [new("Selected Surface Faces", WorkerMpValueKind.Text, new WorkerTextValue("faces"))];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(
                WorkerExecutionStatus.Completed, WorkerExecutionDisposition.Completed,
                execution, null, "completed", 1));
        }
    }
}
