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

public sealed class TypedPointUncertaintyOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.create_point_uncertainty_cloud_point_sets",
        "analysis_operations.create_point_uncertainty_fields",
        "analysis_operations.set_point_weights_from_uncertainties"
    ];

    private static Api.PointName Point(string target) => new()
        { CollectionName = "collection", GroupName = "group", TargetName = target };

    [Fact]
    public void EachUncertaintyOperationHasOneTypedRegistrationAndNoCatalogEntry()
    {
        foreach (var id in MigratedIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    [Fact]
    public void PointListsAreRequiredAndReviewedDefaultsAreApplied()
    {
        Assert.Throws<ArgumentException>(() => CreatePointUncertaintyCloudPointSetsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => CreatePointUncertaintyFieldsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetPointWeightsFromUncertaintiesOperation.CreateCommand(new()));

        var point = Point("P1");
        var cloudSets = CreatePointUncertaintyCloudPointSetsOperation.CreateCommand(new()
            { PointNameList = { point } });
        Assert.Equal(5, cloudSets.InputArguments.Count);
        Assert.Equal("SetPointNameRefListArg", cloudSets.InputArguments[0].SdkBinding);
        Assert.Single(cloudSets.InputArguments[0].RequireValue<WorkerPointNameListValue>().Values);
        Assert.Equal(1000, cloudSets.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal("With respect to WORLD", cloudSets.InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("Group per point", cloudSets.InputArguments[3].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("Point clouds", cloudSets.InputArguments[4].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(3, cloudSets.OutputArguments.Count);

        var fields = CreatePointUncertaintyFieldsOperation.CreateCommand(new()
            { PointNameList = { point } });
        Assert.Equal(1000, fields.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);

        var weights = SetPointWeightsFromUncertaintiesOperation.CreateCommand(new()
        {
            PointNameList = { point },
            ReportingFrame = new Api.CollectionObjectName { ObjectName = "frame" },
            OutputWeightedPointGroup = new Api.CollectionObjectName { ObjectName = "weighted" }
        });
        Assert.Equal("With respect to WORLD", weights.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("Set to fixed value", weights.InputArguments[3].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(1, weights.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(["SetPointNameRefListArg", "SetStringArg", "SetCollectionObjectNameArg2", "SetStringArg", "SetDoubleArg", "SetCollectionObjectNameArg2"],
            weights.InputArguments.Select(value => value.SdkBinding));
    }

    [Fact]
    public void UncertaintyResultsMapObjectAndPointReferenceLists()
    {
        var cloudSets = CreatePointUncertaintyCloudPointSetsOperation.CreateResult(Completed(
            new WorkerRetrievedOutput("Point Groups", WorkerMpValueKind.CollectionObjectNameList,
                new WorkerCollectionObjectNameListValue([new("c", "group", WorkerObjectTypeValue.PointGroup)])),
            new WorkerRetrievedOutput("Point Sets", WorkerMpValueKind.CollectionObjectNameList,
                new WorkerCollectionObjectNameListValue([new("c", "set", WorkerObjectTypeValue.PointSet)])),
            new WorkerRetrievedOutput("Point Clouds", WorkerMpValueKind.CollectionObjectNameList,
                new WorkerCollectionObjectNameListValue([new("c", "cloud", WorkerObjectTypeValue.Cloud)]))));
        var weights = SetPointWeightsFromUncertaintiesOperation.CreateResult(Completed(
            new WorkerRetrievedOutput("Output Weighted Point List", WorkerMpValueKind.PointNameList,
                new WorkerPointNameListValue([new("c", "g", "weighted-1")]))));
        var fields = CreatePointUncertaintyFieldsOperation.CreateResult(Completed());

        Assert.Equal("group", Assert.Single(cloudSets.PointGroups).ObjectName);
        Assert.Equal(Api.ObjectType.PointSet, Assert.Single(cloudSets.PointSets).ObjectType);
        Assert.Equal(Api.ObjectType.Cloud, Assert.Single(cloudSets.PointClouds).ObjectType);
        Assert.Equal("weighted-1", Assert.Single(weights.OutputWeightedPointList).TargetName);
        Assert.NotNull(fields.Execution);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedPointUncertaintyFieldRoute()
    {
        var worker = new UncertaintyFieldsWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var result = await new Api.AnalysisOperations.AnalysisOperationsClient(channel)
            .CreatePointUncertaintyFieldsAsync(new()
            {
                PointNameList = { Point("P1") }
            }, new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        var command = Assert.Single(worker.Commands);
        Assert.Equal(MigratedIds[1], command.OperationId);
        Assert.Equal(1000, command.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
    }

    private static SuccessfulOperationExecution Completed(params WorkerMpOutputValue[] outputs) => new(
        WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed"),
        new Api.MpExecutionDetails());

    private sealed class UncertaintyFieldsWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            Assert.Equal("Create Point Uncertainty Fields", command.StepName);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
