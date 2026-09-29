using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGdtOptionsTests
{
    [Fact]
    public async Task GeneratedClientUsesTypedGdtOptionsRoute()
    {
        var worker = new GdtOptionsWorker();
        var grpcHost = await GrpcTestHost.StartAsync<GdtOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.GdtOperations.GdtOperationsClient(channel);

        var result = await client.GetGdtOptionsAsync(new(), deadline: DateTime.UtcNow.AddSeconds(20));

        Assert.True(result.Options.UseHighPoints);
        Assert.False(result.Options.ExtrapolateAxialExtent);
        Assert.True(result.Options.ExcludeFromAutoEvaluation);
        Assert.False(result.Options.CreateActualFeatures);
        Assert.True(result.Options.CreateSolvedPoints);
        Assert.Equal(0.25, result.Options.CrossSectionCriteria);
        Assert.False(result.Options.EnableAutoFeatureDetection);
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(7, worker.Commands[0].OutputArguments.Count);
        Assert.Equal(
            ["GetBoolArg", "GetBoolArg", "GetBoolArg", "GetBoolArg", "GetBoolArg", "GetDoubleArg", "GetBoolArg"],
            worker.Commands[0].OutputArguments.Select(output => output.SdkBinding));
    }

    [Fact]
    public void TypedRouteIsRegisteredAndRemovedFromTheDynamicCatalog()
    {
        var id = GetGdtOptionsOperation.Descriptor.OperationId;
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
    }

    private sealed class GdtOptionsWorker : IWorkerCommandExecutor
    {
        private static readonly WorkerRetrievedOutput[] OptionValues =
        [
            new("Use High Points", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
            new("Extrapolate Axial Extent", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
            new("Exclude From Auto Evaluation", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
            new("Create Actual Features", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
            new("Create Solved Points", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
            new("Cross Section Criteria", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.25)),
            new("Enable Auto Feature Detection?", WorkerMpValueKind.Logical, new WorkerBooleanValue(false))
        ];

        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1,
                OptionValues, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(
                WorkerExecutionStatus.Completed, WorkerExecutionDisposition.Completed,
                execution, null, "completed", 1));
        }
    }
}
