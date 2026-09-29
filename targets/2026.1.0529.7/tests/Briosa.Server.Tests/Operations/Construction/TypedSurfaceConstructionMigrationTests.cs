using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Security;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedSurfaceConstructionMigrationTests
{
    [Fact]
    public async Task GeneratedClientConstructsSurfacesThroughTypedCommands()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var lifetime = host.ConfigureAwait(true);
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(host.Channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var dissected = await client.ConstructSurfaceByDissectingSurfacesAsync(new()
        {
            DissectionMode = Api.SurfaceDissectionMode.SelectFaces
        }, options);
        var offset = await client.ConstructSurfaceByOffsettingSurfaceAsync(new()
        {
            ReferenceSurface = { Object("nominal") }
        }, options);
        var fitted = await client.ConstructSurfaceFitFromNominalSurfacesAndActualDataAsync(new()
        {
            NominalSurface = Object("nominal"),
            ActualDataPointList = { new Api.PointName { CollectionName = "part", GroupName = "actual", TargetName = "p1" } },
            ResultingSurfaceName = Object("fitted")
        }, options);

        Assert.Equal(Api.ObjectType.Surface, Assert.Single(dissected.ResultantSurfacesList).ObjectType);
        Assert.All(new[] { dissected.Execution, offset.Execution, fitted.Execution },
            detail => Assert.Equal(Api.MpExecutionState.Succeeded, detail.State));
        Assert.Equal(3, worker.Commands.Count);
        Assert.Equal(WorkerSurfaceDissectionModeTypeValue.SelectFaces,
            worker.Commands[0].InputArguments[0]
                .RequireValue<WorkerChoiceValue<WorkerSurfaceDissectionModeTypeValue>>().Value);
        Assert.True(worker.Commands[1].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Surface,
            worker.Commands[2].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal("SetPointNameRefListArg", worker.Commands[2].InputArguments[1].SdkBinding);

        var invalidMode = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructSurfaceByDissectingSurfacesAsync(new(), options));
        var missingSurface = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructSurfaceByOffsettingSurfaceAsync(new(), options));
        Assert.Equal(StatusCode.InvalidArgument, invalidMode.StatusCode);
        Assert.Equal(StatusCode.InvalidArgument, missingSurface.StatusCode);
        Assert.Equal(3, worker.Commands.Count);
    }

    [Fact]
    public void SurfaceDefaultsAndRegistrationsAreExplicit()
    {
        var joined = ConstructSurfaceFromCollectionOfSurfacesOperation.CreateCommand(new()
        {
            SurfacesToCombine = { Object("a"), Object("b") }, ResultingSurfaceName = Object("joined")
        });
        Assert.True(joined.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(joined.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(-1, joined.InputArguments[5].RequireValue<WorkerDoubleValue>().Value);
        var groups = ConstructSurfaceFromPointGroupsOperation.CreateCommand(new()
        {
            GroupNameList = { Object("group") }, ResultingSurfaceName = Object("surface")
        });
        Assert.Equal(WorkerMpValueKind.BSplineFitOptions, groups.InputArguments[1].Kind);

        OperationDescriptor[] descriptors =
        [
            ConstructSurfaceByDissectingSurfacesOperation.Descriptor,
            ConstructSurfaceByOffsettingSurfaceOperation.Descriptor,
            ConstructSurfaceFitFromNominalSurfacesAndActualDataOperation.Descriptor,
            ConstructSurfaceFromAnnotationLinksOperation.Descriptor,
            ConstructSurfaceFromBSplinesOperation.Descriptor,
            ConstructSurfaceFromCollectionOfSurfacesOperation.Descriptor,
            ConstructSurfaceFromPointGroupsOperation.Descriptor,
            ConstructSurfacesByProjectingPointsOperation.Descriptor,
            ConstructSurfacesFromObjectsOperation.Descriptor
        ];
        foreach (var descriptor in descriptors)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == descriptor.OperationId);
            Assert.Equal(Api.ReplaySafety.Unsafe, descriptor.ReplaySafety);
        }
    }

    [Fact]
    public void EachSourceMappingUsesItsDocumentedArgumentOrderAndRejectsMissingLists()
    {
        var annotation = ConstructSurfaceFromAnnotationLinksOperation.CreateCommand(new()
        {
            AnnotationList = { Object("link") }, ResultingSurfaceName = Object("surface")
        });
        Assert.Equal(["Annotation List", "Resulting Surface Name"], annotation.InputArguments.Select(argument => argument.Name));
        Assert.Equal("SetCollectionObjectNameRefListArg", annotation.InputArguments[0].SdkBinding);
        Assert.Equal(WorkerObjectTypeValue.Surface,
            annotation.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Throws<ArgumentException>(() => ConstructSurfaceFromAnnotationLinksOperation.CreateCommand(new()
        {
            ResultingSurfaceName = Object("surface")
        }));

        var splines = ConstructSurfaceFromBSplinesOperation.CreateCommand(new()
        {
            ResultingSurfaceName = Object("surface"), BSplineList = { Object("curve") }
        });
        Assert.Equal(["Resulting Surface Name", "BSpline List"], splines.InputArguments.Select(argument => argument.Name));
        Assert.Equal("SetCollectionObjectNameRefListArg", splines.InputArguments[1].SdkBinding);
        Assert.Throws<ArgumentException>(() => ConstructSurfaceFromBSplinesOperation.CreateCommand(new()
        {
            ResultingSurfaceName = Object("surface")
        }));

        var projected = ConstructSurfacesByProjectingPointsOperation.CreateCommand(new()
        {
            ProjectionTargetNameList = { Object("plane") },
            PointList = { new Api.PointName { CollectionName = "part", GroupName = "points", TargetName = "p1" } },
            ResultingSurfaceName = Object("projected")
        });
        Assert.Equal(["Projection Target Name List", "Point List", "Resulting Surface Name"],
            projected.InputArguments.Select(argument => argument.Name));
        Assert.Equal("SetPointNameRefListArg", projected.InputArguments[1].SdkBinding);
        Assert.Throws<ArgumentException>(() => ConstructSurfacesByProjectingPointsOperation.CreateCommand(new()
        {
            ProjectionTargetNameList = { Object("plane") }, ResultingSurfaceName = Object("projected")
        }));

        var fromObjects = ConstructSurfacesFromObjectsOperation.CreateCommand(new()
        {
            Objects = { Object("sphere") }
        });
        Assert.Equal("Construct Surfaces From Objects", fromObjects.StepName);
        Assert.Equal("Objects", Assert.Single(fromObjects.InputArguments).Name);
        Assert.Equal("SetCollectionObjectNameRefListArg", fromObjects.InputArguments[0].SdkBinding);
        Assert.Throws<ArgumentException>(() => ConstructSurfacesFromObjectsOperation.CreateCommand(new()));

        Assert.Throws<ArgumentException>(() => ConstructSurfaceFromPointGroupsOperation.CreateCommand(new()
        {
            ResultingSurfaceName = Object("surface")
        }));
        Assert.Throws<ArgumentException>(() => ConstructSurfaceFromCollectionOfSurfacesOperation.CreateCommand(new()
        {
            ResultingSurfaceName = Object("surface")
        }));
    }

    private static Api.CollectionObjectName Object(string name) =>
        new() { CollectionName = "part", ObjectName = name };

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId ==
                "construction_operations.construct_surface_by_dissecting_surfaces"
                ? [new WorkerRetrievedOutput("Resultant Surfaces List", WorkerMpValueKind.CollectionObjectNameList,
                    new WorkerCollectionObjectNameListValue([new("part", "face", WorkerObjectTypeValue.Surface)]))]
                : [];
            var result = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, result, null, "completed", 1));
        }
    }
}
