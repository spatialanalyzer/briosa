using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedDynamicRelationshipCreationTests
{
    [Fact]
    public void ConstructionModesUseReviewedBindingsAndRejectUnspecifiedChoices()
    {
        var circle = MakeDynamicCircleRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), ConstructionMode = Api.DynamicCircleMode.SphereAndPlaneIntersection,
            FirstReferenceGeometry = Geometry("sphere"), SecondReferenceGeometry = Geometry("plane")
        });
        Assert.Equal(["SetCollectionObjectNameArg2", "SetDynamicCircleModeArg", "SetCollectionObjectNameArg2", "SetCollectionObjectNameArg2"],
            circle.InputArguments.Select(argument => argument.SdkBinding));
        Assert.Equal(WorkerDynamicCircleModeValue.SpherePlaneIntersection,
            circle.InputArguments[1].RequireValue<WorkerChoiceValue<WorkerDynamicCircleModeValue>>().Value);

        var ellipse = MakeDynamicEllipseRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), ConstructionMode = Api.DynamicEllipseMode.ConeAndPlaneIntersection,
            FirstReferenceGeometry = Geometry("cone"), SecondReferenceGeometry = Geometry("plane")
        });
        Assert.Equal(WorkerDynamicEllipseModeValue.ConePlaneIntersection,
            ellipse.InputArguments[1].RequireValue<WorkerChoiceValue<WorkerDynamicEllipseModeValue>>().Value);

        var line = MakeDynamicLineRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), ConstructionMode = Api.DynamicLineMode.SlotCenterlineAlongLength,
            FirstReferenceGeometry = Geometry("slot"), SecondReferenceGeometry = Geometry("reference")
        });
        Assert.Equal(WorkerDynamicLineModeValue.SlotCenterlineAlongLength,
            line.InputArguments[1].RequireValue<WorkerChoiceValue<WorkerDynamicLineModeValue>>().Value);

        var plane = MakeDynamicPlaneRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), ConstructionMode = Api.DynamicPlaneMode.OffsetPlaneFromPlane,
            FirstReferenceGeometry = Geometry("plane"), SecondReferenceGeometry = Geometry("reference")
        });
        Assert.Equal(WorkerDynamicPlaneModeValue.OffsetPlaneFromPlane,
            plane.InputArguments[1].RequireValue<WorkerChoiceValue<WorkerDynamicPlaneModeValue>>().Value);
        Assert.Equal(0, plane.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);

        var point = MakeDynamicPointRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), ConstructionMode = Api.DynamicPointMode.IntersectionThreePlanes,
            FirstReferenceGeometry = Geometry("plane1"), SecondReferenceGeometry = Geometry("plane2"),
            ThirdReferenceGeometry = Geometry("plane3")
        });
        Assert.Equal(WorkerDynamicPointModeValue.IntersectionThreePlanes,
            point.InputArguments[1].RequireValue<WorkerChoiceValue<WorkerDynamicPointModeValue>>().Value);
        Assert.Equal("plane3", point.InputArguments[4].RequireValue<WorkerCollectionObjectNameValue>().ObjectName);

        Assert.Throws<ArgumentException>(() => MakeDynamicCircleRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), FirstReferenceGeometry = Geometry("sphere"),
            SecondReferenceGeometry = Geometry("plane")
        }));
        Assert.Throws<ArgumentException>(() => MakeDynamicEllipseRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), ConstructionMode = (Api.DynamicEllipseMode)999,
            FirstReferenceGeometry = Geometry("cone"), SecondReferenceGeometry = Geometry("plane")
        }));
        Assert.Throws<ArgumentException>(() => MakeDynamicEllipseRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), ConstructionMode = Api.DynamicEllipseMode.ConeAndPlaneIntersection,
            FirstReferenceGeometry = Geometry("cone")
        }));
        Assert.Throws<ArgumentException>(() => MakeDynamicLineRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), FirstReferenceGeometry = Geometry("line1"),
            SecondReferenceGeometry = Geometry("line2")
        }));
        Assert.Throws<ArgumentException>(() => MakeDynamicLineRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), ConstructionMode = Api.DynamicLineMode.BisectTwoLines,
            SecondReferenceGeometry = Geometry("line2")
        }));
        Assert.Throws<ArgumentException>(() => MakeDynamicPlaneRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), ConstructionMode = (Api.DynamicPlaneMode)999,
            FirstReferenceGeometry = Geometry("plane1"), SecondReferenceGeometry = Geometry("plane2")
        }));
        Assert.Throws<ArgumentException>(() => MakeDynamicPlaneRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), ConstructionMode = Api.DynamicPlaneMode.BisectTwoPlanes,
            FirstReferenceGeometry = Geometry("plane1")
        }));
        Assert.Throws<ArgumentException>(() => MakeDynamicPointRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), ConstructionMode = Api.DynamicPointMode.IntersectionThreePlanes,
            FirstReferenceGeometry = Geometry("plane1"), SecondReferenceGeometry = Geometry("plane2")
        }));
    }

    [Fact]
    public async Task GeneratedClientUsesTypedDynamicRoute()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(host.Channel);
        var result = await client.MakeDynamicPlaneRelationshipAsync(new()
        {
            RelationshipName = Relationship(), ConstructionMode = Api.DynamicPlaneMode.OffsetPlaneFromPlane,
            FirstReferenceGeometry = Geometry("P1"), SecondReferenceGeometry = Geometry("P2"),
            OffsetPlaneOffset = 2.5
        });
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(MakeDynamicPlaneRelationshipOperation.Descriptor.OperationId, Assert.Single(worker.Commands).OperationId);
        Assert.Equal(2.5, worker.Commands[0].InputArguments[4].RequireValue<WorkerDoubleValue>().Value);

        var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.MakeDynamicPlaneRelationshipAsync(new() { RelationshipName = Relationship() }));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
        Assert.Single(worker.Commands);
    }

    private static Api.CollectionItemName Relationship() => new() { CollectionName = "Relationships", ItemName = "R1" };
    private static Api.CollectionObjectName Geometry(string name) => new() { CollectionName = "Geometry", ObjectName = name };

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
