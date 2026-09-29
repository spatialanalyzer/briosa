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

public sealed class TypedPointSetQueryOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.get_coordinate_for_ith_point_in_point_set",
        "analysis_operations.get_ith_point_from_group",
        "analysis_operations.get_number_of_points_in_group",
        "analysis_operations.get_number_of_points_in_point_set",
        "analysis_operations.get_timestamp_for_ith_point_in_point_set"
    ];

    [Fact]
    public void EachPointSetQueryHasOneTypedRegistrationAndNoCatalogEntry()
    {
        foreach (var id in MigratedIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    [Fact]
    public void RequiredNamesAndIndexedDefaultsMatchReviewedBindings()
    {
        Assert.Throws<ArgumentException>(() => GetCoordinateForIthPointInPointSetOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetIthPointFromGroupOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetNumberOfPointsInGroupOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetNumberOfPointsInPointSetOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetTimestampForIthPointInPointSetOperation.CreateCommand(new()));

        var pointSet = new Api.CollectionObjectName { ObjectName = "set" };
        var coordinate = GetCoordinateForIthPointInPointSetOperation.CreateCommand(new() { PointSet = pointSet });
        var timestamp = GetTimestampForIthPointInPointSetOperation.CreateCommand(new() { PointSet = pointSet });
        var group = GetIthPointFromGroupOperation.CreateCommand(new()
            { GroupName = new Api.CollectionObjectName { ObjectName = "group" } });
        foreach (var command in new[] { coordinate, timestamp, group })
        {
            Assert.Equal("SetCollectionObjectNameArg2", command.InputArguments[0].SdkBinding);
            Assert.Equal("SetIntegerArg", command.InputArguments[1].SdkBinding);
            Assert.Equal(0, command.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        }
        Assert.Equal("GetStringArg", coordinate.OutputArguments[0].SdkBinding);
        Assert.Equal("GetPointNameArg", group.OutputArguments[0].SdkBinding);
    }

    [Fact]
    public void MixedPointAndVectorResultsKeepOutputOrderAndPresence()
    {
        var coordinate = GetCoordinateForIthPointInPointSetOperation.CreateResult(Completed(
            new WorkerRetrievedOutput("Point Name", WorkerMpValueKind.Text, new WorkerTextValue("P1")),
            new WorkerRetrievedOutput("Point Coordinates", WorkerMpValueKind.Vector, new WorkerVectorValue(1, 2, 3))));
        Assert.Equal("P1", coordinate.PointName);
        Assert.Equal(3, coordinate.PointCoordinates.Z);

        var point = GetIthPointFromGroupOperation.CreateResult(Completed(
            new WorkerRetrievedOutput("Complete Point Name", WorkerMpValueKind.PointName,
                new WorkerPointNameValue("c", "g", "p")),
            new WorkerRetrievedOutput("Point Name Only", WorkerMpValueKind.Text, new WorkerTextValue("p")),
            new WorkerRetrievedOutput("Vector in Working", WorkerMpValueKind.Vector, new WorkerVectorValue(4, 5, 6))));
        Assert.Equal("p", point.CompletePointName.TargetName);
        Assert.Equal("p", point.PointNameOnly);
        Assert.Equal(6, point.VectorInWorking.Z);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedPointSetCountRoute()
    {
        var worker = new CountWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var result = await new Api.AnalysisOperations.AnalysisOperationsClient(channel)
            .GetNumberOfPointsInPointSetAsync(new()
            {
                PointSetContainer = new Api.CollectionObjectName { ObjectName = "set" }
            }, new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));

        Assert.Equal(7, result.TotalCount);
        Assert.True(result.HasTotalCount);
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(MigratedIds[3], Assert.Single(worker.Commands).OperationId);
    }

    private static SuccessfulOperationExecution Completed(params WorkerMpOutputValue[] outputs) => new(
        WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed"),
        new Api.MpExecutionDetails());

    private sealed class CountWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            Assert.Equal("Get Number of Points In Point Set", command.StepName);
            var outputs = new WorkerMpOutputValue[]
            {
                new WorkerRetrievedOutput("Total Count", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(7))
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
