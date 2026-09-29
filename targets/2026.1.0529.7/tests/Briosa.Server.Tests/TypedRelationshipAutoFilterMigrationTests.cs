using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedRelationshipAutoFilterMigrationTests
{
    [Fact]
    public void Cloud2DMapsExactDefaultsAndRejectsMissingProximity()
    {
        var request = Cloud2D();
        var command = AutoFilterCloudsToNominalGeometry2DOperation.CreateCommand(request);
        Assert.Equal("Auto Filter Clouds to Nominal Geometry 2D", command.StepName);
        Assert.Equal(6, command.InputArguments.Count);
        Assert.Equal(["SetCollectionObjectNameRefListArg", "SetCollectionObjectNameRefListArg",
            "SetCloudThinningOptionsArg", "SetAutoFilterProximitySettingsArg", "SetDoubleArg", "SetBoolArg"],
            command.InputArguments.Select(argument => argument.SdkBinding));
        Assert.Equal(0.01, command.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(command.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        request.FilterProximitySettings2D = null;
        Assert.Throws<ArgumentException>(() => AutoFilterCloudsToNominalGeometry2DOperation.CreateCommand(request));
    }

    [Fact]
    public void Cloud3DMapsTargetArgumentCountAndRejectsEmptyClouds()
    {
        var request = Cloud3D();
        var command = AutoFilterCloudsToNominalGeometry3DOperation.CreateCommand(request);
        Assert.Equal(5, command.InputArguments.Count);
        Assert.Equal("SetAutoFilterProximitySettingsArg", command.InputArguments[3].SdkBinding);
        request.Clouds.Clear();
        Assert.Throws<ArgumentException>(() => AutoFilterCloudsToNominalGeometry3DOperation.CreateCommand(request));
    }

    [Fact]
    public void SurfaceFacesPreservesDefaultsAndRequiresDirection()
    {
        var request = new Api.AutoFilterPointsGroupsCloudsToSurfaceFacesRequest
        {
            Points = new(), Groups = new(), Clouds = new(),
            OffsetDirection = Api.OffsetDirectionType.Both
        };
        request.Points.Values.Add(Point("P"));
        request.Groups.Values.Add(Object("Group"));
        request.Clouds.Values.Add(Object("Cloud"));
        request.Surfaces.Add(Object("Surface"));
        var command = AutoFilterPointsGroupsCloudsToSurfaceFacesOperation.CreateCommand(request);
        Assert.Equal(12, command.InputArguments.Count);
        Assert.Equal(0.1, command.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("SetOffsetDirectionTypeArg", command.InputArguments[5].SdkBinding);
        Assert.Equal("InspAutoFilteredCloud", command.InputArguments[10].RequireValue<WorkerTextValue>().Value);
        Assert.True(command.InputArguments[11].RequireValue<WorkerBooleanValue>().Value);
        request.OffsetDirection = (Api.OffsetDirectionType)999;
        Assert.Throws<ArgumentException>(() =>
            AutoFilterPointsGroupsCloudsToSurfaceFacesOperation.CreateCommand(request));
    }

    [Fact]
    public void NominalPointsRequiresRelationshipAndPoints()
    {
        var request = new Api.AutoFilterPointsToNominalGeometry3DRequest
        {
            FilterProximitySettings3D = Proximity()
        };
        request.AutoFilterTargetRelationships.Add(Item());
        request.Points.Add(Point("P"));
        var command = AutoFilterPointsToNominalGeometry3DOperation.CreateCommand(request);
        Assert.Equal(["SetCollectionObjectNameRefListArg", "SetPointNameRefListArg",
            "SetAutoFilterProximitySettingsArg"], command.InputArguments.Select(argument => argument.SdkBinding));
        request.Points.Clear();
        Assert.Throws<ArgumentException>(() =>
            AutoFilterPointsToNominalGeometry3DOperation.CreateCommand(request));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedCloudFilter()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(host.Channel);
        var result = await client.AutoFilterCloudsToNominalGeometry3DAsync(Cloud3D());
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal("relationship_operations.auto_filter_clouds_to_nominal_geometry_3d",
            Assert.Single(worker.Commands).OperationId);
        var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.AutoFilterCloudsToNominalGeometry3DAsync(new()));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
    }

    private static Api.AutoFilterCloudsToNominalGeometry2DRequest Cloud2D()
    {
        var request = new Api.AutoFilterCloudsToNominalGeometry2DRequest
        {
            FilterProximitySettings2D = Proximity()
        };
        request.AutoFilterTargetRelationships.Add(Item());
        request.Clouds.Add(Object("Cloud"));
        return request;
    }

    private static Api.AutoFilterCloudsToNominalGeometry3DRequest Cloud3D()
    {
        var request = new Api.AutoFilterCloudsToNominalGeometry3DRequest
        {
            FilterProximitySettings3D = Proximity()
        };
        request.AutoFilterTargetRelationships.Add(Item());
        request.Clouds.Add(Object("Cloud"));
        return request;
    }

    private static Api.FilterProximitySettings Proximity() => new()
    {
        SurfaceProximityMode = Api.OffsetDirectionType.Both,
        PlanarProximityMode = Api.OffsetDirectionType.PositiveOnly,
        RadialProximityMode = Api.OffsetDirectionType.NegativeOnly
    };

    private static Api.CollectionItemName Item() =>
        new() { CollectionName = "Relationships", ItemName = "R" };

    private static Api.CollectionObjectName Object(string name) =>
        new() { CollectionName = "Objects", ObjectName = name };

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
