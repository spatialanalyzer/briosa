using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedCrossSectionCloudTests
{
    [Fact]
    public async Task GeneratedClientConstructsCrossSectionCloudsWithDefaults()
    {
        var worker = new CrossSectionCloudWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var standard = await client.ConstructCrossSectionCloudAsync(new()
        {
            CrossSectionCloudName = Object("Results", "CrossSection"),
            ReferenceObject = Object("Frames", "Main"),
            InputClouds = { Object("Scans", "A") }
        }, options);
        var userSelect = await client.ConstructCrossSectionCloudUserSelectAsync(new()
        {
            CrossSectionCloudName = Object("Results", "SelectedCrossSection"),
            ReferencePlanes = { Object("Geometry", "PlaneA") },
            InputClouds = { Object("Scans", "B") }
        }, options);

        Assert.Equal(Api.MpExecutionState.Succeeded, standard.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, userSelect.Execution.State);
        Assert.Equal(2, worker.Commands.Count);

        var standardCommand = worker.Commands[0];
        Assert.Equal(13, standardCommand.InputArguments.Count);
        Assert.Equal(WorkerObjectTypeValue.CrossSectionCloud, ObjectArgument(standardCommand, 0).ObjectType);
        Assert.False(standardCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0, standardCommand.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, standardCommand.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, standardCommand.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, standardCommand.InputArguments[5].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(standardCommand.InputArguments[6].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0, standardCommand.InputArguments[7].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(standardCommand.InputArguments[8].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Any, ObjectArgument(standardCommand, 9).ObjectType);
        Assert.Equal(new[] { new WorkerCollectionObjectNameValue("Scans", "A", WorkerObjectTypeValue.Any) },
            standardCommand.InputArguments[10].RequireValue<WorkerCollectionObjectNameListValue>().Values);
        Assert.Equal(new WorkerCloudThinningOptionsValue(0, 1, 0, 0),
            standardCommand.InputArguments[11].RequireValue<WorkerCloudThinningOptionsValue>());
        Assert.False(standardCommand.InputArguments[12].RequireValue<WorkerBooleanValue>().Value);

        var selectCommand = worker.Commands[1];
        Assert.Equal(9, selectCommand.InputArguments.Count);
        Assert.Equal(WorkerObjectTypeValue.CrossSectionCloud, ObjectArgument(selectCommand, 0).ObjectType);
        Assert.Equal(new[] { new WorkerCollectionObjectNameValue("Geometry", "PlaneA", WorkerObjectTypeValue.Any) },
            selectCommand.InputArguments[5].RequireValue<WorkerCollectionObjectNameListValue>().Values);
        Assert.Equal(new[] { new WorkerCollectionObjectNameValue("Scans", "B", WorkerObjectTypeValue.Any) },
            selectCommand.InputArguments[6].RequireValue<WorkerCollectionObjectNameListValue>().Values);

        var missingReferencePlanes = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructCrossSectionCloudUserSelectAsync(new()
            {
                CrossSectionCloudName = Object("Results", "Invalid"),
                InputClouds = { Object("Scans", "A") }
            }, options));
        Assert.Equal(StatusCode.InvalidArgument, missingReferencePlanes.StatusCode);
        Assert.Equal(2, worker.Commands.Count);
    }

    [Fact]
    public void CrossSectionCloudOperationsAreUnsafeGlobalMutationsOutsideTheGenericCatalog()
    {
        foreach (var operation in new[]
        {
            ConstructCrossSectionCloudOperation.Descriptor,
            ConstructCrossSectionCloudUserSelectOperation.Descriptor
        })
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
            Assert.Empty(operation.RiskFlags);
        }
    }

    private static Api.CollectionObjectName Object(string collection, string name) => new()
    {
        CollectionName = collection,
        ObjectName = name
    };

    private static WorkerCollectionObjectNameValue ObjectArgument(WorkerMpCommand command, int index) =>
        command.InputArguments[index].RequireValue<WorkerCollectionObjectNameValue>();

    private sealed class CrossSectionCloudWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
