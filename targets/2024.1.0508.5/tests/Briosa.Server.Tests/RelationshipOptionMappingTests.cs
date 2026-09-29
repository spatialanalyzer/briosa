using Briosa.Server.Operations.WaveA;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class RelationshipOptionMappingTests
{
    [Fact]
    public void FilterProximitySettingsReachTheWorkerWithAllFields()
    {
        var request = new Api.AutoFilterPointsToNominalGeometry3DRequest
        {
            AutoFilterTargetRelationships = { new Api.CollectionItemName { ItemName = "relationship" } },
            Points = { new Api.PointName { TargetName = "point" } },
            FilterProximitySettings3D = new()
            {
                SurfaceInclusionProximity = 1,
                EdgeExclusionProximity = 2,
                PlanarInclusionProximity = 3,
                PlanarExclusionProximity = 4,
                RadialInclusionProximity = 5,
                GeometryExtractionTolerance = 6,
                SurfaceProximityMode = (Api.OffsetDirectionType)1,
                PlanarProximityMode = (Api.OffsetDirectionType)2,
                RadialProximityMode = (Api.OffsetDirectionType)3,
                ProjectToPlane = true,
                AssertPlaneBoundaries = false
            }
        };
        var command = MpOperationCatalog.Get("relationship_operations.auto_filter_points_to_nominal_geometry_3d")
            .CreateCommand(request);
        var decoded = RoundTrip(command);
        var argument = Assert.Single(decoded.InputArguments, input => input.Name == "Filter Proximity Settings 3D");
        Assert.Equal("SetAutoFilterProximitySettingsArg", argument.SdkBinding);
        Assert.Equal(new WorkerAutoFilterProximitySettingsValue(1, 2, 3, 4, 5, 6, 0, 1, 2, true, false),
            argument.RequireValue<WorkerAutoFilterProximitySettingsValue>());

        request.FilterProximitySettings3D.SurfaceProximityMode = (Api.OffsetDirectionType)9999;
        var invalid = MpOperationCatalog.Get(command.OperationId).CreateCommand(request);
        Assert.Throws<WorkerMessageRejectedException>(() => RoundTrip(invalid));
    }

    [Fact]
    public void FitMotionOptionsPreserveEachDegreeOfFreedom()
    {
        var request = new Api.MoveCollectionsByMinimizingRelationshipsRequest
        {
            RelationshipsToMinimize = { new Api.CollectionItemName { ItemName = "relationship" } },
            MotionToAllow = new()
            {
                AllowX = true,
                AllowY = false,
                AllowZ = true,
                AllowRx = false,
                AllowRy = true,
                AllowRz = false,
                RotateAboutCentroid = true
            }
        };
        var command = MpOperationCatalog.Get("relationship_operations.move_collections_by_minimizing_relationships")
            .CreateCommand(request);
        var argument = Assert.Single(RoundTrip(command).InputArguments, input => input.Name == "Motion to allow");
        Assert.Equal("SetFitDofOptionsArg", argument.SdkBinding);
        Assert.Equal(new WorkerFitDegreeOfFreedomOptionsValue(true, false, true, false, true, false, true),
            argument.RequireValue<WorkerFitDegreeOfFreedomOptionsValue>());
    }

    private static WorkerMpCommand RoundTrip(WorkerMpCommand command)
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream);
        channel.Send(WorkerControlMessage.Execute(Guid.NewGuid(), command));
        stream.Position = 0;
        return channel.Receive().Command!;
    }
}
