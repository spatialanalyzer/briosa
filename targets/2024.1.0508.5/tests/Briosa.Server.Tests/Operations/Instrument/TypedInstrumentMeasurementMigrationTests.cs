using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedInstrumentMeasurementMigrationTests
{
    [Fact]
    public void ClosestCorrespondenceDefaultsToWaitingAndRequiresGroups()
    {
        var request = new Api.AutoCorrespondClosestPointRequest
        {
            Instrument = Instrument(), ReferenceGroup = Group("Reference"), ActualsGroup = Group("Actual")
        };
        var command = AutoCorrespondClosestPointOperation.CreateCommand(request);
        Assert.Equal("Auto-Correspond Closest Point", command.StepName);
        Assert.Equal(["SetColInstIdArg", "SetCollectionObjectNameArg2",
            "SetCollectionObjectNameArg2", "SetBoolArg"],
            command.InputArguments.Select(argument => argument.SdkBinding));
        Assert.True(command.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        request.ActualsGroup = null;
        Assert.Throws<ArgumentException>(() => AutoCorrespondClosestPointOperation.CreateCommand(request));
    }

    [Fact]
    public void ProximityCorrespondencePreservesThresholdsAndRequiredVectorName()
    {
        var request = new Api.AutoCorrespondWithProximityTriggerRequest
        {
            Instrument = Instrument(), NominalGroup = Group("Nominal"), ResultsGroup = Group("Results"),
            DeviationVectorGroupName = string.Empty
        };
        var command = AutoCorrespondWithProximityTriggerOperation.CreateCommand(request);
        Assert.Equal(11, command.InputArguments.Count);
        Assert.Equal(0.5, command.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.25, command.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(12d, command.InputArguments[6].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("SetVectorGroupNameArg", command.InputArguments[8].SdkBinding);
        request.ClearDeviationVectorGroupName();
        Assert.Throws<ArgumentException>(() =>
            AutoCorrespondWithProximityTriggerOperation.CreateCommand(request));
    }

    [Fact]
    public void CollimationMapsChoicesAndRejectsUnknownTilt()
    {
        var request = new Api.CollimationRequest
        {
            StationaryInstrument = Instrument(), MovingInstrument = Instrument(),
            CollimationPoint = Point("C"), TiltMode = Api.CollimationTiltMode.FullCollimation,
            BaselineMethod = Api.CollimationBaselineMethod.DeterminedByValue,
            ScalePoint1 = Point("S1"), ScalePoint2 = Point("S2"),
            NotMeasuredByMovingInstrument = Point("N"),
            AsMeasuredByMovingInstrument = Point("M")
        };
        var command = CollimationOperation.CreateCommand(request);
        Assert.Equal(11, command.InputArguments.Count);
        Assert.Equal("SetCollimationTypeArg", command.InputArguments[4].SdkBinding);
        Assert.Equal("SetCollimationBaselineTypeArg", command.InputArguments[5].SdkBinding);
        Assert.Equal(0d, command.InputArguments[6].RequireValue<WorkerDoubleValue>().Value);
        request.TiltMode = (Api.CollimationTiltMode)999;
        Assert.Throws<ArgumentException>(() => CollimationOperation.CreateCommand(request));
    }

    [Fact]
    public void DriftKeepsFourOutputsAndClosestDefault()
    {
        var request = new Api.DriftCheckRequest
        {
            Instrument = Instrument(), ReferenceGroup = Group("Reference"), ActualsGroup = Group("Actual")
        };
        var command = DriftCheckOperation.CreateCommand(request);
        Assert.True(command.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(["GetDoubleArg", "GetDoubleArg", "GetBoolArg", "GetColInstIdArg"],
            command.OutputArguments.Select(argument => argument.SdkBinding));
        request.ReferenceGroup = null;
        Assert.Throws<ArgumentException>(() => DriftCheckOperation.CreateCommand(request));
    }

    [Fact]
    public void EdgeScanKeepsEmptyTextDefaultsAndRequiresPoint()
    {
        var request = new Api.EdgeScanMeasurementRequest
        {
            Instrument = Instrument(), PointNearEdge = Point("N"),
            EdgeSearchDirectionPoint = Point("D"), PointGroup = Group("G")
        };
        var command = EdgeScanMeasurementOperation.CreateCommand(request);
        Assert.Equal(["SetColInstIdArg", "SetPointNameArg", "SetPointNameArg", "SetStringArg",
            "SetCollectionObjectNameArg2", "SetStringArg"],
            command.InputArguments.Select(argument => argument.SdkBinding));
        Assert.Equal(string.Empty, command.InputArguments[3].RequireValue<WorkerTextValue>().Value);
        request.PointNearEdge = null;
        Assert.Throws<ArgumentException>(() => EdgeScanMeasurementOperation.CreateCommand(request));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedCorrespondence()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(host.Channel);
        var result = await client.AutoCorrespondClosestPointAsync(new()
        {
            Instrument = Instrument(), ReferenceGroup = Group("Reference"), ActualsGroup = Group("Actual")
        });
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal("instrument_operations.auto_correspond_closest_point",
            Assert.Single(worker.Commands).OperationId);
        var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.AutoCorrespondClosestPointAsync(new()));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
    }

    private static Api.CollectionInstrumentId Instrument() =>
        new() { CollectionName = "Instruments", InstrumentId = 1 };

    private static Api.CollectionObjectName Group(string name) =>
        new() { CollectionName = "Points", ObjectName = name };

    private static Api.PointName Point(string name) =>
        new() { CollectionName = "Points", GroupName = "G", TargetName = name };

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
