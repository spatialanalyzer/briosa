using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedRelationshipCreationFamilyTests
{
    [Fact]
    public void FrameAndGeometryRoutesPreserveRequiredAndOptionalBindings()
    {
        var frame = MakeFrameToFrameRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), FirstFrameName = Object("F1"), SecondFrameName = Object("F2"),
            OrientationTolerance = new(), PositionTolerance = new()
        });
        Assert.Equal("Make Frame to Frame Relationship", frame.StepName);
        Assert.Equal(WorkerObjectTypeValue.Frame,
            frame.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(["SetCollectionObjectNameArg2", "SetCollectionObjectNameArg2", "SetCollectionObjectNameArg2",
            "SetToleranceScalarOptionsArg", "SetToleranceVectorOptionsArg"],
            frame.InputArguments.Select(argument => argument.SdkBinding));
        Assert.Throws<ArgumentException>(() => MakeFrameToFrameRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), FirstFrameName = Object("F1"), SecondFrameName = Object("F2"),
            PositionTolerance = new()
        }));

        var compare = MakeGeometryCompareOnlyRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), NominalGeometry = Object("nominal"), MeasuredGeometry = Object("measured")
        });
        Assert.Equal(["Relationship Name", "Nominal Geometry", "Measured Geometry"],
            compare.InputArguments.Select(argument => argument.Name));
        Assert.Throws<ArgumentException>(() => MakeGeometryCompareOnlyRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), NominalGeometry = Object("nominal")
        }));

        var fitCompare = new Api.MakeGeometryFitAndCompareToNominalRelationshipRequest
        {
            RelationshipName = Relationship(), NominalGeometry = Object("nominal")
        };
        fitCompare.PointGroupsToFit.Add(Object("group"));
        var withoutOptionals = MakeGeometryFitAndCompareToNominalRelationshipOperation.CreateCommand(fitCompare);
        Assert.Equal(3, withoutOptionals.InputArguments.Count);
        fitCompare.ResultingObjectName = Object("fit");
        fitCompare.FitProfileName = "profile";
        var withOptionals = MakeGeometryFitAndCompareToNominalRelationshipOperation.CreateCommand(fitCompare);
        Assert.Equal(["Resulting Object Name (Optional)", "Fit Profile Name (Optional)"],
            withOptionals.InputArguments.Skip(3).Select(argument => argument.Name));
        Assert.Equal("profile", withOptionals.InputArguments[4].RequireValue<WorkerTextValue>().Value);
        Assert.Throws<ArgumentException>(() => MakeGeometryFitAndCompareToNominalRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), NominalGeometry = Object("nominal")
        }));

        var fitOnly = new Api.MakeGeometryFitOnlyRelationshipRequest
        {
            RelationshipName = Relationship(), GeometryType = Api.GeometryType.Plane
        };
        fitOnly.PointGroupsToFit.Add(Object("group"));
        Assert.Equal("SetGeometryTypeArg",
            MakeGeometryFitOnlyRelationshipOperation.CreateCommand(fitOnly).InputArguments[2].SdkBinding);
        fitOnly.ClearGeometryType();
        Assert.Throws<ArgumentException>(() => MakeGeometryFitOnlyRelationshipOperation.CreateCommand(fitOnly));
    }

    [Fact]
    public void GroupAndObjectRoutesPreserveDefaultsAndDomains()
    {
        var group = MakeGroupToGroupRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), FirstGroupName = Object("G1"), SecondGroupName = Object("G2"),
            Tolerance = new(), Constraint = new()
        });
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            group.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.False(group.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => MakeGroupToGroupRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), FirstGroupName = Object("G1"), SecondGroupName = Object("G2"),
            Constraint = new()
        }));

        var nominal = MakeGroupToNominalGroupRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), NominalGroupName = Object("nominal"), MeasuredGroupName = Object("measured"),
            Tolerance = new(), Constraint = new()
        });
        Assert.Equal(12, nominal.InputArguments.Count);
        Assert.True(nominal.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0.01, nominal.InputArguments[8].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(1d, nominal.InputArguments[11].RequireValue<WorkerDoubleValue>().Value);
        Assert.Throws<ArgumentException>(() => MakeGroupToNominalGroupRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), NominalGroupName = Object("nominal"),
            Tolerance = new(), Constraint = new()
        }));

        var direction = MakeObjectToObjectDirectionRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), FirstObjectInRelationship = Object("A"),
            SecondObjectInRelationship = Object("B")
        });
        Assert.Equal(0d, direction.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("SetDoubleArg", direction.InputArguments[3].SdkBinding);
        Assert.Throws<ArgumentException>(() => MakeObjectToObjectDirectionRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), FirstObjectInRelationship = Object("A")
        }));
    }

    [Fact]
    public void ListRelationshipRoutesUseCorrectNameDomains()
    {
        var groups = new Api.MakeGroupsToObjectsRelationshipRequest
        {
            RelationshipName = Relationship(), ProjectionOptions = new()
        };
        groups.PointGroupsInRelationship.Add(Object("G"));
        groups.ObjectsInRelationship.Add(Object("O"));
        Assert.Equal("SetProjectionOptionsArg",
            MakeGroupsToObjectsRelationshipOperation.CreateCommand(groups).InputArguments[3].SdkBinding);
        groups.ProjectionOptions = null;
        Assert.Throws<ArgumentException>(() => MakeGroupsToObjectsRelationshipOperation.CreateCommand(groups));

        var clouds = new Api.MakePointCloudsToObjectsRelationshipRequest
        {
            RelationshipName = Relationship(), ProjectionOptions = new()
        };
        clouds.PointCloudsInRelationship.Add(Object("C"));
        clouds.ObjectsInRelationship.Add(Object("O"));
        Assert.Equal("SetCollectionObjectNameRefListArg",
            MakePointCloudsToObjectsRelationshipOperation.CreateCommand(clouds).InputArguments[1].SdkBinding);
        clouds.PointCloudsInRelationship.Clear();
        Assert.Throws<ArgumentException>(() => MakePointCloudsToObjectsRelationshipOperation.CreateCommand(clouds));

        var points = PointsToObjects();
        var pointsCommand = MakePointsToObjectsRelationshipOperation.CreateCommand(points);
        Assert.Equal("SetPointNameRefListArg", pointsCommand.InputArguments[1].SdkBinding);
        Assert.Equal("P1", Assert.Single(pointsCommand.InputArguments[1]
            .RequireValue<WorkerPointNameListValue>().Values).TargetName);
        points.ProjectionOptions = null;
        Assert.Throws<ArgumentException>(() => MakePointsToObjectsRelationshipOperation.CreateCommand(points));

        var pointPairs = new Api.MakePointsToPointsRelationshipRequest
        {
            RelationshipName = Relationship(), Tolerance = new(), Constraint = new()
        };
        pointPairs.NominalPoints.Add(Point("nominal"));
        pointPairs.MeasuredPoints.Add(Point("measured"));
        Assert.Equal(["SetCollectionObjectNameArg2", "SetPointNameRefListArg", "SetPointNameRefListArg",
            "SetBoolArg", "SetToleranceVectorOptionsArg", "SetToleranceVectorOptionsArg"],
            MakePointsToPointsRelationshipOperation.CreateCommand(pointPairs).InputArguments.Select(argument => argument.SdkBinding));
        pointPairs.MeasuredPoints.Clear();
        Assert.Throws<ArgumentException>(() => MakePointsToPointsRelationshipOperation.CreateCommand(pointPairs));

        var vectors = MakeVectorGroupToVectorGroupRelationshipOperation.CreateCommand(new()
        {
            NewVgToVgRelationship = Relationship(), ReferenceVectorGroup = Object("V1"),
            CorrespondingVectorGroup = Object("V2")
        });
        Assert.Equal(WorkerObjectTypeValue.VectorGroup,
            vectors.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.True(vectors.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => MakeVectorGroupToVectorGroupRelationshipOperation.CreateCommand(new()
        {
            NewVgToVgRelationship = Relationship(), ReferenceVectorGroup = Object("V1")
        }));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedPointsToObjectsRoute()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(host.Channel);
        var result = await client.MakePointsToObjectsRelationshipAsync(PointsToObjects());
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal("relationship_operations.make_points_to_objects_relationship", Assert.Single(worker.Commands).OperationId);
        var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.MakePointsToObjectsRelationshipAsync(new() { RelationshipName = Relationship() }));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
        Assert.Single(worker.Commands);
    }

    private static Api.MakePointsToObjectsRelationshipRequest PointsToObjects()
    {
        var request = new Api.MakePointsToObjectsRelationshipRequest
        {
            RelationshipName = Relationship(), ProjectionOptions = new()
        };
        request.PointsInRelationship.Add(Point("P1"));
        request.ObjectsInRelationship.Add(Object("O1"));
        return request;
    }

    private static Api.CollectionItemName Relationship() => new() { CollectionName = "Relationships", ItemName = "R1" };
    private static Api.CollectionObjectName Object(string name) => new() { CollectionName = "Objects", ObjectName = name };
    private static Api.PointName Point(string name) => new() { CollectionName = "Points", GroupName = "G", TargetName = name };

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
