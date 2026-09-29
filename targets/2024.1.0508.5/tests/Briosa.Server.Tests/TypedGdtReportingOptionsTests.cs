using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGdtReportingOptionsTests
{
    [Fact]
    public async Task GeneratedClientUsesTargetSpecificTypedReportingOptions()
    {
        var worker = new ReportingOptionsWorker();
        var grpcHost = await GrpcTestHost.StartAsync<GdtOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.GdtOperations.GdtOperationsClient(channel);
        var featureCheck = new Api.CollectionItemName { CollectionName = "C", ItemName = "FC" };

        var get = await client.GetFeatureCheckReportingOptionsAsync(new() { FeatureCheck = featureCheck },
            deadline: DateTime.UtcNow.AddSeconds(20));
        Assert.True(get.Options.ShowFeatureControlFrameSummary);
        Assert.False(get.Options.IncludeTitle);
        Assert.True(get.Options.ShowDatumAndToleranceSummary);
        Assert.False(get.Options.ShowFeatureSummary);
        Assert.False(get.Options.ShowPointDetails);
        Assert.True(get.Options.ShowLowerTierTables);
        Assert.Equal(6, worker.Commands[0].OutputArguments.Count);

        var set = await client.SetFeatureCheckReportingOptionsAsync(new() { FeatureCheck = featureCheck },
            deadline: DateTime.UtcNow.AddSeconds(20));
        Assert.Equal(Api.MpExecutionState.Succeeded, set.Execution.State);
        Assert.Equal(7, worker.Commands[1].InputArguments.Count);
        Assert.Equal([true, false, false, false, false, false],
            worker.Commands[1].InputArguments.Skip(1).Select(x => x.RequireValue<WorkerBooleanValue>().Value));
    }

    [Fact]
    public void RequiredFeatureCheckAndRegistryAreEnforced()
    {
        Assert.Throws<ArgumentException>(() => GetFeatureCheckReportingOptionsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetFeatureCheckReportingOptionsOperation.CreateCommand(new()));
        foreach (var id in new[] { GetFeatureCheckReportingOptionsOperation.Descriptor.OperationId,
                     SetFeatureCheckReportingOptionsOperation.Descriptor.OperationId })
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    private sealed class ReportingOptionsWorker : IWorkerCommandExecutor
    {
        private static readonly bool[] ReportingValues = [true, false, true, false, false, true];
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerRetrievedOutput[] outputs = command.OperationId.EndsWith("get_feature_check_reporting_options", StringComparison.Ordinal)
                ? ReportingValues
                    .Select((value, index) => new WorkerRetrievedOutput(command.OutputArguments[index].Name,
                        WorkerMpValueKind.Logical, new WorkerBooleanValue(value))).ToArray()
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(
                WorkerExecutionStatus.Completed, WorkerExecutionDisposition.Completed,
                execution, null, "completed", 1));
        }
    }
}
