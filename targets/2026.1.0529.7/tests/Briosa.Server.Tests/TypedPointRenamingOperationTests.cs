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

public sealed class TypedPointRenamingOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.rename_points_based_on_inter_point_distance_to_reference_points",
        "analysis_operations.rename_points_based_on_proximity_to_reference_points"
    ];

    [Fact]
    public void BothRenamersHaveTypedRegistrationAndKeepTheAtRiskFlagOnlyWhereRecorded()
    {
        foreach (var id in MigratedIds)
        {
            var descriptor = Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, descriptor.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, descriptor.ReplaySafety);
            string[] expectedFlags = id == MigratedIds[0] ? ["fixture_validation_pending"] : [];
            Assert.Equal(expectedFlags, descriptor.RiskFlags);
        }
    }

    [Fact]
    public void ThresholdAndBooleanInputsPreserveTheReviewedDefaultsAndBindings()
    {
        var reference = Object("reference");
        var group = Object("group");
        var interPoint = RenamePointsBasedOnInterPointDistanceToReferencePointsOperation.CreateCommand(new()
        {
            ReferenceGroupName = reference,
            GroupToRenamePoints = group
        });
        var proximity = RenamePointsBasedOnProximityToReferencePointsOperation.CreateCommand(new()
        {
            ReferenceGroupName = reference,
            GroupToRenamePoints = group
        });

        AssertInputs(interPoint, "Rename points based on inter-point distance to reference points",
            ("Reference Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Group To Rename Points", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Distance Threshold", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Verify Results?", WorkerMpValueKind.Logical, "SetBoolArg"));
        AssertInputs(proximity, "Rename points based on proximity to reference points",
            ("Reference Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Group To Rename Points", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Proximity Threshold", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Verify Results?", WorkerMpValueKind.Logical, "SetBoolArg"),
            ("Rename All Proximate Points?", WorkerMpValueKind.Logical, "SetBoolArg"));

        Assert.Equal(0d, interPoint.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(interPoint.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0d, proximity.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(proximity.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(proximity.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);

        var configuredInterPoint = RenamePointsBasedOnInterPointDistanceToReferencePointsOperation.CreateCommand(new()
        {
            ReferenceGroupName = reference, GroupToRenamePoints = group, DistanceThreshold = 1.25, VerifyResults = true
        });
        var configuredProximity = RenamePointsBasedOnProximityToReferencePointsOperation.CreateCommand(new()
        {
            ReferenceGroupName = reference, GroupToRenamePoints = group, ProximityThreshold = 2.5,
            VerifyResults = true, RenameAllProximatePoints = true
        });
        Assert.Equal(1.25, configuredInterPoint.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.True(configuredInterPoint.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(2.5, configuredProximity.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.True(configuredProximity.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(configuredProximity.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => RenamePointsBasedOnInterPointDistanceToReferencePointsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => RenamePointsBasedOnProximityToReferencePointsOperation.CreateCommand(new()));
        Assert.Empty(interPoint.OutputArguments);
        Assert.Empty(proximity.OutputArguments);
    }

    [Fact]
    public async Task GeneratedClientRoutesBothPointRenamers()
    {
        var worker = new RecordingWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.AnalysisOperations.AnalysisOperationsClient(channel);
        var deadline = DateTime.UtcNow.AddSeconds(20);
        var interPoint = await client.RenamePointsBasedOnInterPointDistanceToReferencePointsAsync(new()
        {
            ReferenceGroupName = Object("reference"), GroupToRenamePoints = Object("group"), DistanceThreshold = 1
        }, new CallOptions(deadline: deadline));
        var proximity = await client.RenamePointsBasedOnProximityToReferencePointsAsync(new()
        {
            ReferenceGroupName = Object("reference"), GroupToRenamePoints = Object("group"), ProximityThreshold = 2
        }, new CallOptions(deadline: deadline));

        Assert.Equal(MigratedIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(Api.MpExecutionState.Succeeded, interPoint.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, proximity.Execution.State);
    }

    private static Api.CollectionObjectName Object(string name) => new() { ObjectName = name };

    private static void AssertInputs(WorkerMpCommand command, string step,
        params (string Name, WorkerMpValueKind Kind, string? Binding)[] expected)
    {
        Assert.Equal(step, command.StepName);
        Assert.Equal(expected,
            command.InputArguments.Select(argument => (argument.Name, argument.Kind, argument.SdkBinding)).ToArray());
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
