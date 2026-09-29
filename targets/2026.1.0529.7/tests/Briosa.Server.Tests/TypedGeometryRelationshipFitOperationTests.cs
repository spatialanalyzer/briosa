using Briosa.Server.Operations;
using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGeometryRelationshipFitOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.group_to_surface_fit",
        "analysis_operations.import_geometry_fit_profiles",
        "analysis_operations.set_geometry_relationship_fit_profile"
    ];

    private static readonly string[] FixturePendingFlag = ["fixture_validation_pending"];
    private static readonly string[] GroupOutputBindings = ["GetWorldTransformArg", "GetDoubleArg", "GetDoubleArg"];

    [Fact]
    public void EachOperationHasOneTypedRegistrationAndNoCatalogEntry()
    {
        foreach (var id in MigratedIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }

        Assert.Equal(FixturePendingFlag, ImportGeometryFitProfilesOperation.Descriptor.RiskFlags);
    }

    [Fact]
    public void GroupToSurfaceFitPreservesArgumentBindingsDefaultsAndOutputOrder()
    {
        var request = new Api.GroupToSurfaceFitRequest
        {
            GroupToFit = new Api.CollectionObjectName { CollectionName = "Groups", ObjectName = "measured" },
            Surface = new Api.CollectionObjectName { CollectionName = "Geometry", ObjectName = "nominal" }
        };
        var command = GroupToSurfaceFitOperation.CreateCommand(request);

        Assert.Equal("Group To Surface Fit", command.StepName);
        Assert.Equal(MigratedIds[0], command.OperationId);
        Assert.Equal(5, command.InputArguments.Count);
        Assert.Equal("measured", command.InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectName);
        Assert.Equal("nominal", command.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectName);
        Assert.Equal("SetCollectionObjectNameArg2", command.InputArguments[0].SdkBinding);
        Assert.Equal("SetCollectionObjectNameArg2", command.InputArguments[1].SdkBinding);
        Assert.False(command.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0d, command.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, command.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(GroupOutputBindings,
            command.OutputArguments.Select(argument => argument.SdkBinding).ToArray());
        Assert.Throws<ArgumentException>(() => GroupToSurfaceFitOperation.CreateCommand(new()));

        var matrix = Enumerable.Range(0, 16).Select(value => (double)value).ToArray();
        var result = GroupToSurfaceFitOperation.CreateResult(Completed(
            new WorkerRetrievedOutput("Optimum Transform", WorkerMpValueKind.WorldTransform,
                new WorkerWorldTransformValue(new WorkerTransformValue(matrix), 1.5d)),
            new WorkerRetrievedOutput("RMS Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.25d)),
            new WorkerRetrievedOutput("Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.75d))));

        Assert.Equal(matrix, result.OptimumTransform.Transform.Values);
        Assert.Equal(1.5d, result.OptimumTransform.ScaleFactor);
        Assert.Equal(0.25d, result.RmsDeviation);
        Assert.Equal(0.75d, result.MaximumAbsoluteDeviation);
    }

    [Fact]
    public void ImportFitProfilesRequiresPathAndPreservesOverwriteDefaultAndRiskStatus()
    {
        var command = ImportGeometryFitProfilesOperation.CreateCommand(new()
        {
            GeometryFitProfilesFilePath = new Api.FileReference { Path = "profiles.xml" }
        });

        Assert.Equal(MigratedIds[1], command.OperationId);
        Assert.Equal("SetFilePathArg", command.InputArguments[0].SdkBinding);
        Assert.Equal("profiles.xml", command.InputArguments[0].RequireValue<WorkerFileReferenceValue>().Path);
        Assert.False(command.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => ImportGeometryFitProfilesOperation.CreateCommand(new()));

        var completed = Completed();
        Assert.Same(completed.Details, ImportGeometryFitProfilesOperation.CreateResult(completed).Execution);
    }

    [Fact]
    public void RelationshipFitProfileUsesTypedItemListAndExactDefaults()
    {
        var request = new Api.SetGeometryRelationshipFitProfileRequest
        {
            GeometryType = Api.GeometryType.Cylinder
        };
        request.RelationshipRefList.Add(new Api.CollectionItemName
        {
            CollectionName = "Relationships",
            ItemName = "rel-1",
            ItemType = Api.ItemType.Relationship
        });

        var command = SetGeometryRelationshipFitProfileOperation.CreateCommand(request);
        var geometryType = command.InputArguments[0].RequireValue<WorkerChoiceValue<WorkerGeometryTypeValue>>();
        var items = command.InputArguments[1].RequireValue<WorkerCollectionItemNameListValue>();

        Assert.Equal(MigratedIds[2], command.OperationId);
        Assert.Equal(WorkerGeometryTypeValue.Cylinder, geometryType.Value);
        Assert.Equal("SetGeometryTypeArg", command.InputArguments[0].SdkBinding);
        Assert.Equal("SetCollectionObjectNameRefListArg", command.InputArguments[1].SdkBinding);
        Assert.Equal(WorkerItemTypeValue.Relationship, Assert.Single(items.Values).ItemType);
        Assert.Equal(string.Empty, command.InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.False(command.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => SetGeometryRelationshipFitProfileOperation.CreateCommand(new()
        {
            GeometryType = Api.GeometryType.Cylinder
        }));
    }

    [Fact]
    public async Task GeneratedClientReachesAllThreeTypedRoutes()
    {
        var worker = new GeometryRelationshipWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.AnalysisOperations.AnalysisOperationsClient(channel);
        var deadline = DateTime.UtcNow.AddSeconds(20);

        var groupResult = await client.GroupToSurfaceFitAsync(new()
        {
            GroupToFit = new Api.CollectionObjectName { ObjectName = "measured" },
            Surface = new Api.CollectionObjectName { ObjectName = "nominal" }
        }, new CallOptions(deadline: deadline));
        var importResult = await client.ImportGeometryFitProfilesAsync(new()
        {
            GeometryFitProfilesFilePath = new Api.FileReference { Path = "profiles.xml" }
        }, new CallOptions(deadline: deadline));
        var profileResult = await client.SetGeometryRelationshipFitProfileAsync(new()
        {
            GeometryType = Api.GeometryType.Cylinder,
            RelationshipRefList = { new Api.CollectionItemName { ItemName = "rel-1" } }
        }, new CallOptions(deadline: deadline));

        Assert.Equal(Api.MpExecutionState.Succeeded, groupResult.Execution.State);
        Assert.Equal(0.25d, groupResult.RmsDeviation);
        Assert.Equal(Api.MpExecutionState.Succeeded, importResult.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, profileResult.Execution.State);
        Assert.Equal(MigratedIds, worker.Commands.Select(command => command.OperationId));
    }

    private static SuccessfulOperationExecution Completed(params WorkerMpOutputValue[] outputs) => new(
        WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed"),
        new Api.MpExecutionDetails());

    private sealed class GeometryRelationshipWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId == MigratedIds[0]
                ?
                [
                    new WorkerRetrievedOutput("Optimum Transform", WorkerMpValueKind.WorldTransform,
                        new WorkerWorldTransformValue(new WorkerTransformValue(Enumerable.Range(0, 16).Select(value => (double)value).ToArray()), 1d)),
                    new WorkerRetrievedOutput("RMS Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.25d)),
                    new WorkerRetrievedOutput("Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.75d))
                ]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
