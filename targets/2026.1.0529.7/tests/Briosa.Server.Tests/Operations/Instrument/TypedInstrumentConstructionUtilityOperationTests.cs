using Briosa.Server.Operations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedInstrumentConstructionUtilityOperationTests
{
    private static readonly string[] Ids =
    [
        "instrument_operations.build_target",
        "instrument_operations.combine_point_groups",
        "instrument_operations.construct_mirror_from_plane",
        "instrument_operations.construct_mirror_from_two_points",
        "instrument_operations.construct_perimeters_from_surface_face_list",
        "instrument_operations.create_new_dynamic_reference",
        "instrument_operations.create_templated_instrument_usmn",
        "instrument_operations.dissect_point_group"
    ];

    [Fact]
    public void RoutesAndInputBindingsPreserveExactTargetContract()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, descriptor => descriptor.OperationId == id);
        }
        var instrument = Instrument();
        var point = Point();
        var group = Group();
        var build = BuildTargetOperation.CreateCommand(new()
        {
            Instrument = instrument, OutputTargetName = point, NominalPoint = point, Tolerance = new()
        });
        Assert.Equal("SetColInstIdArg,SetPointNameArg,SetPointNameArg,SetToleranceVectorOptionsArg",
            string.Join(',', build.InputArguments.Select(input => input.SdkBinding)));
        Assert.Equal(4, build.InputArguments.Count);
        Assert.Throws<ArgumentException>(() => BuildTargetOperation.CreateCommand(new()
            { Instrument = instrument, OutputTargetName = point, NominalPoint = point }));
        var withPrompt = BuildTargetOperation.CreateCommand(new()
        {
            Instrument = instrument, OutputTargetName = point, NominalPoint = point,
            Tolerance = new(), HtmlPromptFile = new Api.FileReference { Path = "prompt.html" }
        });
        Assert.Equal("SetFilePathArg", withPrompt.InputArguments[4].SdkBinding);

        var combine = CombinePointGroupsOperation.CreateCommand(new()
            { GroupsToCombine = { group }, CombinedPointGroup = group });
        Assert.Equal("SetCollectionObjectNameRefListArg", combine.InputArguments[0].SdkBinding);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            combine.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Throws<ArgumentException>(() => CombinePointGroupsOperation.CreateCommand(new()
            { CombinedPointGroup = group }));

        var plane = ConstructMirrorFromPlaneOperation.CreateCommand(new()
            { Instrument = instrument, Plane = group });
        Assert.Equal(string.Empty, plane.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Throws<ArgumentException>(() => ConstructMirrorFromPlaneOperation.CreateCommand(new()
            { Instrument = instrument }));
        var twoPoints = ConstructMirrorFromTwoPointsOperation.CreateCommand(new()
        {
            Instrument = instrument, PointMeasuredDirectly = point, PointMeasuredThroughMirror = point
        });
        Assert.True(twoPoints.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("GetCollectionObjectNameArg", Assert.Single(twoPoints.OutputArguments).SdkBinding);
        Assert.Throws<ArgumentException>(() => ConstructMirrorFromTwoPointsOperation.CreateCommand(new()
            { Instrument = instrument, PointMeasuredDirectly = point }));

        var perimeters = ConstructPerimetersFromSurfaceFaceListOperation.CreateCommand(new());
        Assert.Equal(string.Empty, Assert.Single(perimeters.InputArguments).RequireValue<WorkerTextValue>().Value);
        Assert.Equal(2, perimeters.OutputArguments.Count);
        var dynamicReference = CreateNewDynamicReferenceOperation.CreateCommand(new()
            { Instrument = instrument, PointsDefiningDynamicReference = { point } });
        Assert.Equal("SetPointNameRefListArg", dynamicReference.InputArguments[1].SdkBinding);
        Assert.Throws<ArgumentException>(() => CreateNewDynamicReferenceOperation.CreateCommand(new()
            { Instrument = instrument }));

        var usmn = CreateTemplatedInstrumentUsmnOperation.CreateCommand(new()
            { InstrumentTemplateName = group, Instrument = instrument });
        Assert.Equal(15, usmn.InputArguments.Count);
        Assert.Equal(1d, usmn.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.True(usmn.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(usmn.InputArguments[10].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(1d, usmn.InputArguments[14].RequireValue<WorkerDoubleValue>().Value);
        Assert.Throws<ArgumentException>(() => CreateTemplatedInstrumentUsmnOperation.CreateCommand(new()
            { Instrument = instrument }));
        var dissect = DissectPointGroupOperation.CreateCommand(new() { GroupToDissect = group });
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            dissect.InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(string.Empty, dissect.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Throws<ArgumentException>(() => DissectPointGroupOperation.CreateCommand(new()));
    }

    [Fact]
    public void MirrorAndPerimeterResultsKeepOutputOrder()
    {
        var mirror = ConstructMirrorFromTwoPointsOperation.CreateResult(Success(
        [
            new WorkerRetrievedOutput("Mirror Plane", WorkerMpValueKind.CollectionObjectName,
                new WorkerCollectionObjectNameValue("C", "Mirror", WorkerObjectTypeValue.Plane))
        ]));
        Assert.Equal("Mirror", mirror.MirrorPlane.ObjectName);
        var perimeters = ConstructPerimetersFromSurfaceFaceListOperation.CreateResult(Success(
        [
            new WorkerRetrievedOutput("Scan perimeter list", WorkerMpValueKind.CollectionObjectNameList,
                new WorkerCollectionObjectNameListValue([new("C", "Scan", WorkerObjectTypeValue.Perimeter)])),
            new WorkerRetrievedOutput("Exclusion perimeter list", WorkerMpValueKind.CollectionObjectNameList,
                new WorkerCollectionObjectNameListValue([new("C", "Exclude", WorkerObjectTypeValue.Perimeter)]))
        ]));
        Assert.Equal("Scan", Assert.Single(perimeters.Perimeters.ScanPerimeters).ObjectName);
        Assert.Equal("Exclude", Assert.Single(perimeters.Perimeters.ExclusionPerimeters).ObjectName);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedBuildTargetRoute()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(host.Channel);
        var result = await client.BuildTargetAsync(new()
        {
            Instrument = Instrument(), OutputTargetName = Point(), NominalPoint = Point(), Tolerance = new()
        }, new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal("instrument_operations.build_target", Assert.Single(worker.Commands).OperationId);
    }

    private static Api.CollectionInstrumentId Instrument() => new() { CollectionName = "C", InstrumentId = 1 };
    private static Api.PointName Point() => new() { CollectionName = "C", GroupName = "G", TargetName = "P" };
    private static Api.CollectionObjectName Group() => new() { CollectionName = "C", ObjectName = "G" };

    private static SuccessfulOperationExecution Success(IReadOnlyList<WorkerMpOutputValue> outputs) =>
        new(WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed"),
            new Api.MpExecutionDetails());

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
