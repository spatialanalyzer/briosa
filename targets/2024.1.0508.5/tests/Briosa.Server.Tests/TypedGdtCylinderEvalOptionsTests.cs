using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGdtCylinderEvalOptionsTests
{
    [Fact]
    public async Task GeneratedClientUsesTypedOptionsRoutesAndDefaults()
    {
        var worker = new CylinderOptionsWorker();
        var grpcHost = await GrpcTestHost.StartAsync<GdtOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.GdtOperations.GdtOperationsClient(channel);
        var featureCheck = new Api.CollectionItemName { CollectionName = "C", ItemName = "FC" };

        var get = await client.GetFeatureCheckCylinderEvalOptionsAsync(new() { FeatureCheck = featureCheck },
            deadline: DateTime.UtcNow.AddSeconds(20));
        Assert.True(get.Options.EnableActualDiameterOverride);
        Assert.Equal(2.5, get.Options.ActualDiameterOverride);
        Assert.Equal(["GetBoolArg", "GetDoubleArg"], worker.Commands[0].OutputArguments.Select(x => x.SdkBinding));

        var set = await client.SetFeatureCheckCylinderEvalOptionsAsync(new() { FeatureCheck = featureCheck },
            deadline: DateTime.UtcNow.AddSeconds(20));
        Assert.Equal(Api.MpExecutionState.Succeeded, set.Execution.State);
        Assert.False(worker.Commands[1].InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0, worker.Commands[1].InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
    }

    [Fact]
    public void RequiredFeatureCheckAndRegistryAreEnforced()
    {
        Assert.Throws<ArgumentException>(() => GetFeatureCheckCylinderEvalOptionsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetFeatureCheckCylinderEvalOptionsOperation.CreateCommand(new()));
        foreach (var id in new[] { GetFeatureCheckCylinderEvalOptionsOperation.Descriptor.OperationId,
                     SetFeatureCheckCylinderEvalOptionsOperation.Descriptor.OperationId })
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    private sealed class CylinderOptionsWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerRetrievedOutput[] outputs = command.OperationId.EndsWith("get_feature_check_cylinder_eval_options", StringComparison.Ordinal)
                ? [
                    new("Enable Actual Diameter Override", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                    new("Actual Diameter Override", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2.5))
                  ]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(
                WorkerExecutionStatus.Completed, WorkerExecutionDisposition.Completed,
                execution, null, "completed", 1));
        }
    }
}
