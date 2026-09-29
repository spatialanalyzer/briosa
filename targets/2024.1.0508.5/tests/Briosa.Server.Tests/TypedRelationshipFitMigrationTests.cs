using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedRelationshipFitMigrationTests
{
    [Fact]
    public void UncertaintyAndSummaryMapDistinctReferenceKinds()
    {
        var uncertainty = ComputeGeometryRelationshipUncertaintiesOperation.CreateCommand(
            new() { RelationshipName = Item() });
        Assert.Equal("SetCollectionObjectNameArg2", uncertainty.InputArguments[0].SdkBinding);
        Assert.False(uncertainty.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => ComputeGeometryRelationshipUncertaintiesOperation.CreateCommand(new()));

        var summary = new Api.GenerateGeometryRelationshipSummaryRequest();
        summary.RelationshipRefList.Add(Item());
        var command = GenerateGeometryRelationshipSummaryOperation.CreateCommand(summary);
        Assert.Equal(WorkerMpValueKind.CollectionObjectNameList, command.InputArguments[0].Kind);
        Assert.Equal("Geometry Relationship Summary", command.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Throws<ArgumentException>(() => GenerateGeometryRelationshipSummaryOperation.CreateCommand(new()));
    }

    [Fact]
    public void FitPreservesBindingsOutputsAndRequiredLists()
    {
        var request = new Api.DoRelationshipFitRequest
        {
            CollectionContainingRelationships = "Relationships", MotionToAllow = new()
        };
        request.ObjectsToMove.Add(Object("Cloud"));
        request.InstrumentsToMove.Add(new Api.CollectionInstrumentId { CollectionName = "Instruments", InstrumentId = 1 });
        var command = DoRelationshipFitOperation.CreateCommand(request);
        Assert.Equal("Do Relationship Fit", command.StepName);
        Assert.Equal(["SetCollectionNameArg", "SetCollectionObjectNameRefListArg", "SetColInstIdRefListArg",
            "SetStringArg", "SetFitDofOptionsArg",  "SetBoolArg"],
            command.InputArguments.Select(argument => argument.SdkBinding));
        Assert.Equal("Gauss-Newton", command.InputArguments[3].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(["GetTransformArg", "GetWorldTransformArg", "GetWorldTransformArg", "GetDoubleArg"],
            command.OutputArguments.Select(argument => argument.SdkBinding));
        request.InstrumentsToMove.Clear();
        Assert.Throws<ArgumentException>(() => DoRelationshipFitOperation.CreateCommand(request));
    }

    [Fact]
    public void OutlierFilterKeepsTenTypedMetricsAndSigmaDefault()
    {
        var command = FilterGeometryRelationshipOutlierCloudPointsOperation.CreateCommand(
            new() { RelationshipName = Item() });
        Assert.Equal(3d, command.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(10, command.OutputArguments.Count);
        Assert.Equal("GetIntegerArg", command.OutputArguments[9].SdkBinding);
        Assert.Throws<ArgumentException>(() =>
            FilterGeometryRelationshipOutlierCloudPointsOperation.CreateCommand(new()));
    }

    [Fact]
    public void CollectionFitMapsOptionalEmptyListAndSolverChoice()
    {
        var request = new Api.MoveCollectionsByMinimizingRelationshipsRequest
        {
            MotionToAllow = new(), SolverMode = Api.SolverMode.DirectSearch
        };
        request.RelationshipsToMinimize.Add(Item());
        var command = MoveCollectionsByMinimizingRelationshipsOperation.CreateCommand(request);
        Assert.Empty(command.InputArguments[0].RequireValue<WorkerStringListValue>().Values);
        Assert.Equal("Direct Search", command.InputArguments[2].RequireValue<WorkerTextValue>().Value);
        request.SolverMode = (Api.SolverMode)999;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MoveCollectionsByMinimizingRelationshipsOperation.CreateCommand(request));
        request.SolverMode = Api.SolverMode.GaussNewton;
        request.RelationshipsToMinimize.Clear();
        Assert.Throws<ArgumentException>(() =>
            MoveCollectionsByMinimizingRelationshipsOperation.CreateCommand(request));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedUncertaintyRoute()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(host.Channel);
        var result = await client.ComputeGeometryRelationshipUncertaintiesAsync(
            new() { RelationshipName = Item() });
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal("relationship_operations.compute_geometry_relationship_uncertainties",
            Assert.Single(worker.Commands).OperationId);
        var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ComputeGeometryRelationshipUncertaintiesAsync(new()));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
    }

    private static Api.CollectionObjectName Object(string name) =>
        new() { CollectionName = "Objects", ObjectName = name };

    private static Api.CollectionItemName Item() =>
        new() { CollectionName = "Relationships", ItemName = "Fit" };

    private sealed class RecordingWorker : IWorkerCommandExecutor
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
