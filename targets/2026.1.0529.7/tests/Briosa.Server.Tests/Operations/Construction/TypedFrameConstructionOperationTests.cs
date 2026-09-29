using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedFrameConstructionOperationTests
{
    private static readonly string[] Ids =
    [
        "construction_operations.construct_frame",
        "construction_operations.construct_frame_at_point_with_working_z_and_clocked_axis",
        "construction_operations.construct_frame_at_robot_link",
        "construction_operations.construct_frame_average_of_other_object_frames",
        "construction_operations.construct_frame_copy_and_make_left_handed",
        "construction_operations.construct_frame_from_point_measurement_probing_frames",
        "construction_operations.construct_frame_from_transform_in_world",
        "construction_operations.construct_frame_known_origin_object_direction_object_direction",
        "construction_operations.construct_frame_on_instrument_base",
        "construction_operations.construct_frame_on_object",
        "construction_operations.construct_frame_pick_origin_and_point_on_x_axis_clock_z_along_working_z",
        "construction_operations.construct_frame_three_planes",
        "construction_operations.construct_frame_three_points",
        "construction_operations.construct_frame_with_wizard",
        "construction_operations.construct_mirror_cube_frame"
    ];

    [Fact]
    public void FrameRoutesAreTypedAndRegistered()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, descriptor => descriptor.OperationId == id);
        }
    }

    [Fact]
    public void FrameTransformsNamesAndFlagsPreserveBindings()
    {
        Assert.Throws<ArgumentException>(() => ConstructFrameOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => ConstructFrameOperation.CreateCommand(new()
            { NewFrameName = Frame() }));
        var frame = ConstructFrameOperation.CreateCommand(new()
            { NewFrameName = Frame(), TransformInWorkingCoordinates = Transform() });
        Assert.Equal(["SetCollectionObjectNameArg2", "SetTransformArg"],
            frame.InputArguments.Select(input => input.SdkBinding));
        Assert.Equal(WorkerObjectTypeValue.Frame, frame.InputArguments[0]
            .RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal("SetTransformArg", ConstructFrameFromTransformInWorldOperation.CreateCommand(new()
            { NewFrameName = Frame(), TransformInWorldCoordinates = Transform() }).InputArguments[1].SdkBinding);

        var wizard = ConstructFrameWithWizardOperation.CreateCommand(new() { NewFrameName = Frame() });
        Assert.True(wizard.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(ConstructFrameWithWizardOperation.CreateCommand(new()
            { NewFrameName = Frame(), WaitForCompletion = false }).InputArguments[1]
            .RequireValue<WorkerBooleanValue>().Value);
        var onObject = ConstructFrameOnObjectOperation.CreateCommand(new() { ReferenceObject = Frame() });
        Assert.Single(onObject.InputArguments);
        Assert.Equal(2, ConstructFrameOnObjectOperation.CreateCommand(new()
            { ReferenceObject = Frame(), FrameName = Frame("New") }).InputArguments.Count);
        Assert.Throws<ArgumentException>(() => ConstructFrameAverageOfOtherObjectFramesOperation.CreateCommand(new()));
        var average = ConstructFrameAverageOfOtherObjectFramesOperation.CreateCommand(new()
            { Objects = { Frame("A") } });
        Assert.Single(average.InputArguments);
        Assert.Equal("SetCollectionObjectNameRefListArg", average.InputArguments[0].SdkBinding);
        Assert.Throws<ArgumentException>(() => ConstructFrameFromPointMeasurementProbingFramesOperation.CreateCommand(new()));
        var probes = ConstructFrameFromPointMeasurementProbingFramesOperation.CreateCommand(new()
            { PointList = { Point("P") } });
        Assert.Equal(["SetPointNameRefListArg", "SetBoolArg"],
            probes.InputArguments.Select(input => input.SdkBinding));
        Assert.False(probes.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public void FrameAxisPointAndPlaneChoicesPreserveExactOrder()
    {
        Assert.Throws<ArgumentException>(() => FrameConstructionChoiceMapper.Axis(Api.AxisIdentifier.Unspecified));
        Assert.Throws<ArgumentException>(() => FrameConstructionChoiceMapper.Method(Api.FrameConstructionMethod.Unspecified));
        Assert.Equal(WorkerAxisIdentifierValue.PositiveX,
            FrameConstructionChoiceMapper.FrameAxis((Api.FrameAxis)1).Value);
        var clocked = ConstructFrameAtPointWithWorkingZAndClockedAxisOperation.CreateCommand(new()
        {
            OriginPoint = Point("O"), ClockedAxis = (Api.AxisIdentifier)2, ClockingPoint = Point("C")
        });
        Assert.Equal(["SetPointNameArg", "SetAxisNameArg", "SetPointNameArg"],
            clocked.InputArguments.Select(input => input.SdkBinding));
        Assert.Equal(WorkerAxisIdentifierValue.NegativeX, clocked.InputArguments[1]
            .RequireValue<WorkerChoiceValue<WorkerAxisIdentifierValue>>().Value);
        Assert.Equal(4, ConstructFrameAtPointWithWorkingZAndClockedAxisOperation.CreateCommand(new()
        {
            OriginPoint = Point("O"), ClockedAxis = (Api.AxisIdentifier)1,
            ClockingPoint = Point("C"), FrameName = "F"
        }).InputArguments.Count);

        var copy = ConstructFrameCopyAndMakeLeftHandedOperation.CreateCommand(new()
            { ReferenceFrame = Frame(), AxisToReverse = (Api.FrameAxis)3 });
        Assert.Equal(["SetCollectionObjectNameArg2", "SetAxisNameArg"],
            copy.InputArguments.Select(input => input.SdkBinding));
        Assert.Equal(WorkerAxisIdentifierValue.PositiveZ, copy.InputArguments[1]
            .RequireValue<WorkerChoiceValue<WorkerAxisIdentifierValue>>().Value);
        Assert.Equal(3, ConstructFrameCopyAndMakeLeftHandedOperation.CreateCommand(new()
            { ReferenceFrame = Frame(), FrameName = Frame("New"), AxisToReverse = (Api.FrameAxis)1 })
            .InputArguments.Count);

        var instrument = ConstructFrameOnInstrumentBaseOperation.CreateCommand(new()
            { InstrumentId = new Api.CollectionInstrumentId { CollectionName = "C", InstrumentId = 7 } });
        Assert.Equal("SetColInstIdArg", Assert.Single(instrument.InputArguments).SdkBinding);
        Assert.Equal(2, ConstructFrameOnInstrumentBaseOperation.CreateCommand(new()
            { InstrumentId = new Api.CollectionInstrumentId { CollectionName = "C", InstrumentId = 7 }, FrameName = "F" })
            .InputArguments.Count);
        var robot = ConstructFrameAtRobotLinkOperation.CreateCommand(new()
        {
            MachineId = new Api.CollectionMachineId { CollectionName = "C", MachineId = 3 }, ResultingFrame = Frame()
        });
        Assert.Equal(["SetColMachineIdArg", "SetStringArg", "SetCollectionObjectNameArg2"],
            robot.InputArguments.Select(input => input.SdkBinding));

        var pick = ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZOperation.CreateCommand(new()
            { OriginPoint = Point("O"), PointOnXAxis = Point("X") });
        Assert.Equal(["SetPointNameArg", "SetPointNameArg"], pick.InputArguments.Select(input => input.SdkBinding));
        var planes = ConstructFrameThreePlanesOperation.CreateCommand(new()
            { XPlane = Frame("X"), YPlane = Frame("Y"), ZPlane = Frame("Z") });
        Assert.Equal(["SetCollectionObjectNameArg2", "SetDoubleArg", "SetCollectionObjectNameArg2",
            "SetDoubleArg", "SetCollectionObjectNameArg2", "SetDoubleArg"],
            planes.InputArguments.Select(input => input.SdkBinding));
        Assert.Equal(WorkerObjectTypeValue.Plane, planes.InputArguments[0]
            .RequireValue<WorkerCollectionObjectNameValue>().ObjectType);

        var points = ConstructFrameThreePointsOperation.CreateCommand(new()
        {
            ConstructionMethod = (Api.FrameConstructionMethod)3,
            OriginPoint = Point("O"), PrimaryAxisPoint = Point("P"), SecondaryAxisPoint = Point("S")
        });
        Assert.Equal("Origin Y Yx", points.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(["SetStringArg", "SetPointNameArg", "SetPointNameArg", "SetPointNameArg"],
            points.InputArguments.Select(input => input.SdkBinding));
    }

    [Fact]
    public void KnownOriginAndMirrorCubePreserveDefaultsAndOutput()
    {
        var known = ConstructFrameKnownOriginObjectDirectionObjectDirectionOperation.CreateCommand(new()
        {
            KnownPoint = Point("P"), KnownPointValueInNewFrame = new Api.Vector { X = 1 },
            PrimaryAxisObject = Frame("X"), PrimaryAxisDefinesWhichAxis = (Api.AxisIdentifier)1,
            SecondaryAxisObject = Frame("Y"), SecondaryAxisDefinesWhichAxis = (Api.AxisIdentifier)3
        });
        Assert.Equal(["SetPointNameArg", "SetVectorArg", "SetCollectionObjectNameArg2",
            "SetAxisNameArg", "SetCollectionObjectNameArg2", "SetAxisNameArg"],
            known.InputArguments.Select(input => input.SdkBinding));
        var mirror = ConstructMirrorCubeFrameOperation.CreateCommand(new()
            { MirrorCubeFrameName = Frame(), PointName = Point("P") });
        Assert.Equal(["SetCollectionObjectNameArg2", "SetPointNameArg", "SetBoolArg", "SetDoubleArg"],
            mirror.InputArguments.Select(input => input.SdkBinding));
        Assert.True(mirror.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(90d, mirror.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("GetDoubleArg", Assert.Single(mirror.OutputArguments).SdkBinding);
        var output = new WorkerRetrievedOutput("Total Angular Error", WorkerMpValueKind.FloatingPoint,
            new WorkerDoubleValue(0.25));
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [output], "completed");
        Assert.Equal(0.25, ConstructMirrorCubeFrameOperation.CreateResult(new(execution,
            new Api.MpExecutionDetails())).TotalAngularError);
    }

    [Fact]
    public async Task GeneratedClientUsesTypedFrameRoute()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(host.Channel);
        var result = await client.ConstructFrameWithWizardAsync(new() { NewFrameName = Frame() },
            new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal("construction_operations.construct_frame_with_wizard", Assert.Single(worker.Commands).OperationId);
        Assert.True(worker.Commands[0].InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
    }

    private static Api.CollectionObjectName Frame(string name = "F") => new() { CollectionName = "C", ObjectName = name };
    private static Api.PointName Point(string name) => new() { CollectionName = "C", GroupName = "G", TargetName = name };
    private static Api.Transform Transform() => new() { Values = { Enumerable.Range(0, 16).Select(value => (double)value) } };

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
