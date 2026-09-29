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

public sealed class TypedFitProfileOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.make_circle_fit_profile",
        "analysis_operations.make_cone_fit_profile",
        "analysis_operations.make_cylinder_fit_profile",
        "analysis_operations.make_ellipse_fit_profile",
        "analysis_operations.make_line_fit_profile",
        "analysis_operations.make_paraboloid_fit_profile",
        "analysis_operations.make_plane_fit_profile",
        "analysis_operations.make_slot_fit_profile",
        "analysis_operations.make_sphere_fit_profile"
    ];

    [Fact]
    public void EveryFitProfileCreatorHasOneTypedRegistrationAndNoCatalogEntry()
    {
        foreach (var id in MigratedIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    [Fact]
    public void FitProfileDefaultsAndChoiceOrdinalsMatchTheExactBindings()
    {
        var circle = MakeCircleFitProfileOperation.CreateCommand(new());
        Assert.Equal("SetStringArg", circle.InputArguments[0].SdkBinding);
        Assert.Equal(string.Empty, circle.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(WorkerMeasuredSideForRadialOffsetValue.Outside,
            circle.InputArguments[1].RequireValue<WorkerChoiceValue<WorkerMeasuredSideForRadialOffsetValue>>().Value);
        Assert.Equal(-1d, circle.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(WorkerMeasuredSideForPlanarOffsetValue.AbovePlane,
            circle.InputArguments[3].RequireValue<WorkerChoiceValue<WorkerMeasuredSideForPlanarOffsetValue>>().Value);
        Assert.Equal(WorkerNormalDirectionValue.ProbingDirection,
            circle.InputArguments[5].RequireValue<WorkerChoiceValue<WorkerNormalDirectionValue>>().Value);
        Assert.Equal(WorkerCompTechniqueValue.Standard,
            circle.InputArguments[7].RequireValue<WorkerChoiceValue<WorkerCompTechniqueValue>>().Value);
        Assert.False(circle.InputArguments[8].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(circle.InputArguments[9].RequireValue<WorkerBooleanValue>().Value);

        var sphere = MakeSphereFitProfileOperation.CreateCommand(new());
        Assert.Equal(7, sphere.InputArguments.Count);
        Assert.Equal(-1d, sphere.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(WorkerSphereFitComputationModeValue.Standard,
            sphere.InputArguments[6].RequireValue<WorkerChoiceValue<WorkerSphereFitComputationModeValue>>().Value);

        Assert.Throws<ArgumentOutOfRangeException>(() => MakeCircleFitProfileOperation.CreateCommand(new()
        {
            CircleComputationTechnique = Api.CompTechnique.Unspecified
        }));
    }

    [Fact]
    public void DistinctFitProfilesRetainTheirReviewedArgumentCountsAndOrder()
    {
        Assert.Equal(9, MakeConeFitProfileOperation.CreateCommand(new()).InputArguments.Count);
        Assert.Equal(12, MakeEllipseFitProfileOperation.CreateCommand(new()).InputArguments.Count);

        var line = MakeLineFitProfileOperation.CreateCommand(new());
        Assert.Equal(6, line.InputArguments.Count);
        Assert.Equal("Fit Profile Name", line.InputArguments[0].Name);
        Assert.Equal("Reverse Normal Vector after fit?", line.InputArguments[1].Name);
        Assert.Equal("SetBoolArg", line.InputArguments[5].SdkBinding);

        Assert.Equal(8, MakeParaboloidFitProfileOperation.CreateCommand(new()).InputArguments.Count);
        Assert.Equal(8, MakePlaneFitProfileOperation.CreateCommand(new()).InputArguments.Count);
        Assert.Equal(14, MakeSlotFitProfileOperation.CreateCommand(new()).InputArguments.Count);
        Assert.Empty(MakeSphereFitProfileOperation.CreateCommand(new()).OutputArguments);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedSphereFitProfileRoute()
    {
        var worker = new FitProfileWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var result = await new Api.AnalysisOperations.AnalysisOperationsClient(channel)
            .MakeSphereFitProfileAsync(new(), new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(MigratedIds[8], Assert.Single(worker.Commands).OperationId);
    }

    private sealed class FitProfileWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            Assert.Equal("Make Sphere Fit Profile", command.StepName);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
