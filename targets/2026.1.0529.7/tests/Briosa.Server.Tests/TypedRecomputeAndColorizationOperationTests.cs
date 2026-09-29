using Briosa.Server.Operations;
using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedRecomputeAndColorizationOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.re_compute_calculated_items",
        "analysis_operations.set_default_colorization_options"
    ];

    [Fact]
    public void BothOperationsHaveTypedRegistrationsAndPreserveDefaultValues()
    {
        foreach (var id in MigratedIds)
        {
            var descriptor = Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, descriptor.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, descriptor.ReplaySafety);
            Assert.Empty(descriptor.RiskFlags);
        }

        var recompute = ReComputeCalculatedItemsOperation.CreateCommand(new());
        Assert.Equal("Re-Compute Calculated Items", recompute.StepName);
        Assert.Equal(
            ["Targets from Shots", "Hidden Points", "Relationships"],
            recompute.InputArguments.Take(3).Select(argument => argument.Name));
        Assert.All(recompute.InputArguments.Take(3), argument =>
        {
            Assert.Equal(WorkerMpValueKind.Logical, argument.Kind);
            Assert.Equal("SetBoolArg", argument.SdkBinding);
            Assert.False(argument.RequireValue<WorkerBooleanValue>().Value);
        });
        Assert.Empty(recompute.OutputArguments);

        var colorization = SetDefaultColorizationOptionsOperation.CreateCommand(new());
        Assert.Equal("Set Default Colorization Options", colorization.StepName);
        var input = Assert.Single(colorization.InputArguments);
        Assert.Equal("Colorization Options", input.Name);
        Assert.Equal(WorkerMpValueKind.ColorizationOptions, input.Kind);
        Assert.Equal("SetColorizationOptionsArg", input.SdkBinding);
        Assert.Equal(new WorkerColorizationOptionsValue(
            1, 2, 1, 0, false, true, false, 100, 1, false, 0.1, false, false, true, false,
            0.5, -0.5, 0.03, -0.03), input.RequireValue<WorkerColorizationOptionsValue>());
        Assert.Empty(colorization.OutputArguments);
    }

    [Fact]
    public void ExplicitColorizationOptionsMapAllChoiceAndScalarValues()
    {
        var command = SetDefaultColorizationOptionsOperation.CreateCommand(new()
        {
            ColorizationOptions = new()
            {
                ColorRangeMethod = Api.ColorRangeMethod.Continuous,
                BaseHighColor = Api.BaseColorType.Red,
                BaseMidColor = Api.BaseMidColorType.Gray,
                BaseLowColor = Api.BaseColorType.Blue,
                DrawTubes = true,
                DrawArrowheads = false,
                IndicateValues = true,
                VectorMagnification = 250,
                VectorWidth = 4,
                DrawBlotches = true,
                BlotchSize = 0.75,
                ShowOutOfToleranceOnly = true,
                ShowColorBarInView = true,
                ShowColorBarPercentages = false,
                ShowColorBarFractions = true,
                HighSaturationLimit = 0.8,
                LowSaturationLimit = -0.8,
                HighTolerance = 0.04,
                LowTolerance = -0.04
            }
        });

        Assert.Equal(new WorkerColorizationOptionsValue(
            1, 0, 2, 2, true, false, true, 250, 4, true, 0.75, true, true, false, true,
            0.8, -0.8, 0.04, -0.04), command.InputArguments[0].RequireValue<WorkerColorizationOptionsValue>());
    }

    [Fact]
    public async Task GeneratedClientRoutesRecomputeAndSetDefaultColorizationOptions()
    {
        var worker = new RecordingWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.AnalysisOperations.AnalysisOperationsClient(channel);
        var deadline = DateTime.UtcNow.AddSeconds(20);
        var recompute = await client.ReComputeCalculatedItemsAsync(new(), new CallOptions(deadline: deadline));
        var colorization = await client.SetDefaultColorizationOptionsAsync(new(), new CallOptions(deadline: deadline));

        Assert.Equal(MigratedIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(Api.MpExecutionState.Succeeded, recompute.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, colorization.Execution.State);
    }

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
