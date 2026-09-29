using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGdtReportingFrameTests
{
    [Fact]
    public async Task GeneratedClientUsesTypedReportingFrameRoutes()
    {
        var worker = new ReportingFrameWorker();
        var grpcHost = await GrpcTestHost.StartAsync<GdtOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.GdtOperations.GdtOperationsClient(channel);
        var featureCheck = new Api.CollectionItemName { CollectionName = "C", ItemName = "FC" };

        var get = await client.GetFeatureCheckReportingFrameAsync(new() { FeatureCheck = featureCheck },
            deadline: DateTime.UtcNow.AddSeconds(20));
        Assert.Equal(Api.ObjectType.Frame, get.ReportingFrame.ObjectType);
        Assert.Equal("GetCollectionObjectNameArg", worker.Commands[0].OutputArguments[0].SdkBinding);

        var set = await client.SetFeatureCheckReportingFrameAsync(new()
        {
            FeatureCheck = featureCheck,
            ReportingFrame = new Api.CollectionObjectName { CollectionName = "C", ObjectName = "F" }
        }, deadline: DateTime.UtcNow.AddSeconds(20));
        Assert.Equal(Api.MpExecutionState.Succeeded, set.Execution.State);
        Assert.Equal(WorkerItemTypeValue.FeatureCheck,
            worker.Commands[1].InputArguments[0].RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.Equal(WorkerObjectTypeValue.Frame,
            worker.Commands[1].InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(["SetCollectionObjectNameArg2", "SetCollectionObjectNameArg2"],
            worker.Commands[1].InputArguments.Select(x => x.SdkBinding));
    }

    [Fact]
    public void RequiredNamesAndRegistryAreEnforced()
    {
        Assert.Throws<ArgumentException>(() => GetFeatureCheckReportingFrameOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetFeatureCheckReportingFrameOperation.CreateCommand(new()
            { FeatureCheck = new Api.CollectionItemName { ItemName = "FC" } }));
        foreach (var id in new[] { GetFeatureCheckReportingFrameOperation.Descriptor.OperationId,
                     SetFeatureCheckReportingFrameOperation.Descriptor.OperationId })
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    private sealed class ReportingFrameWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerRetrievedOutput[] outputs = command.OperationId.EndsWith("get_feature_check_reporting_frame", StringComparison.Ordinal)
                ? [new("Reporting Frame", WorkerMpValueKind.CollectionObjectName,
                    new WorkerCollectionObjectNameValue("C", "F", WorkerObjectTypeValue.Frame))]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(
                WorkerExecutionStatus.Completed, WorkerExecutionDisposition.Completed,
                execution, null, "completed", 1));
        }
    }
}
