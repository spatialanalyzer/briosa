using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedInstrumentWatchMigrationTests
{
    [Fact]
    public void ClosestPointMapsWindowDefaultsAndRequiresGroups()
    {
        var request = new Api.WatchClosestPointRequest
        {
            Instrument = Instrument(), WatchWindowProperties = Window()
        };
        request.GroupsToConsider.Add(Object("Group"));
        var command = WatchClosestPointOperation.CreateCommand(request);
        Assert.Equal(9, command.InputArguments.Count);
        Assert.Equal(["SetColInstIdArg", "SetCollectionObjectNameRefListArg",
            "SetCollectionObjectNameArg2", "SetStringArg", "SetBoolArg", "SetIntegerArg",
            "SetIntegerArg", "SetIntegerArg", "SetIntegerArg"],
            command.InputArguments.Select(argument => argument.SdkBinding));
        Assert.Equal(string.Empty, command.InputArguments[3].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0, command.InputArguments[8].RequireValue<WorkerIntegerValue>().Value);
        request.GroupsToConsider.Clear();
        Assert.Throws<ArgumentException>(() => WatchClosestPointOperation.CreateCommand(request));
    }

    [Fact]
    public void EdgeWatchKeepsReferenceListsAndRequiresProjection()
    {
        var request = new Api.WatchPointToEdgeRequest
        {
            Instrument = Instrument(), ProjectionOptions = new(), WatchWindowProperties = Window()
        };
        request.ProjectionReferenceObjects.Add(Object("Projection"));
        request.MeasurementReferenceObjects.Add(Object("Measurement"));
        var command = WatchPointToEdgeOperation.CreateCommand(request);
        Assert.Equal(11, command.InputArguments.Count);
        Assert.Equal("Measurement Reference Objects ", command.InputArguments[2].Name);
        Assert.Equal("SetProjectionOptionsArg", command.InputArguments[3].SdkBinding);
        request.ProjectionOptions = null;
        Assert.Throws<ArgumentException>(() => WatchPointToEdgeOperation.CreateCommand(request));
    }

    [Fact]
    public void ObjectWatchUsesTypedProjectionAndRequiresObjects()
    {
        var request = new Api.WatchPointToObjectsRequest
        {
            Instrument = Instrument(), ProjectionOptions = new(), WatchWindowProperties = Window()
        };
        request.ObjectsToConsider.Add(Object("Nominal"));
        var command = WatchPointToObjectsOperation.CreateCommand(request);
        Assert.Equal(10, command.InputArguments.Count);
        Assert.Equal("SetProjectionOptionsArg", command.InputArguments[2].SdkBinding);
        request.ObjectsToConsider.Clear();
        Assert.Throws<ArgumentException>(() => WatchPointToObjectsOperation.CreateCommand(request));
    }

    [Fact]
    public void PointWatchPreservesReferenceAndWindowDefaults()
    {
        var request = new Api.WatchPointToPointRequest
        {
            Instrument = Instrument(), ReferencePoint = Point(), WatchWindowProperties = Window()
        };
        var command = WatchPointToPointOperation.CreateCommand(request);
        Assert.Equal(9, command.InputArguments.Count);
        Assert.False(command.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        request.ReferencePoint = null;
        Assert.Throws<ArgumentException>(() => WatchPointToPointOperation.CreateCommand(request));
    }

    [Fact]
    public void ZoomingWatchDefaultsToUpdateAndRejectsMissingPoint()
    {
        var request = new Api.WatchPointToPointWithViewZoomingRequest
        {
            Instrument = Instrument(), ReferencePoint = Point()
        };
        var command = WatchPointToPointWithViewZoomingOperation.CreateCommand(request);
        Assert.Equal(["SetColInstIdArg", "SetPointNameArg", "SetBoolArg"],
            command.InputArguments.Select(argument => argument.SdkBinding));
        Assert.True(command.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        request.ReferencePoint = null;
        Assert.Throws<ArgumentException>(() =>
            WatchPointToPointWithViewZoomingOperation.CreateCommand(request));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedWatch()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(host.Channel);
        var result = await client.WatchPointToPointWithViewZoomingAsync(new()
        {
            Instrument = Instrument(), ReferencePoint = Point()
        });
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal("instrument_operations.watch_point_to_point_with_view_zooming",
            Assert.Single(worker.Commands).OperationId);
        var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.WatchPointToPointWithViewZoomingAsync(new()));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
    }

    private static Api.CollectionInstrumentId Instrument() =>
        new() { CollectionName = "Instruments", InstrumentId = 1 };

    private static Api.CollectionObjectName Object(string name) =>
        new() { CollectionName = "Objects", ObjectName = name };

    private static Api.CollectionObjectName Window() =>
        new() { CollectionName = "Windows", ObjectName = "ThreeDof" };

    private static Api.PointName Point() =>
        new() { CollectionName = "Points", GroupName = "G", TargetName = "P" };

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
