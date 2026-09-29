using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedRelationshipOptionMigrationTests
{
    [Fact]
    public void WatchTemplateKeepsExactDefaultsAndRequiresUdpSettings()
    {
        var request = new Api.RelationshipWatchWindowTemplateRequest
        {
            WatchWindowTemplateName = Object("watch"), UdpNetworkTransmitSettings = new()
        };
        var command = RelationshipWatchWindowTemplateOperation.CreateCommand(request);
        Assert.Equal("Relationship Watch Window Template", command.StepName);
        Assert.Equal(14, command.InputArguments.Count);
        Assert.Equal(4, command.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(3, command.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(new WorkerFontValue("MS Shell Dlg", 8, new(0, 0, 0)),
            command.InputArguments[3].RequireValue<WorkerFontValue>());
        Assert.Equal(new WorkerRgbColorValue(255, 0, 0),
            command.InputArguments[4].RequireValue<WorkerRgbColorValue>());
        Assert.Equal("SetUdpTransmitSettingsArg", command.InputArguments[11].SdkBinding);
        Assert.Equal(new WorkerUdpTransmitSettingsValue(false, true, string.Empty, 10000),
            command.InputArguments[11].RequireValue<WorkerUdpTransmitSettingsValue>());
        Assert.True(command.InputArguments[7].RequireValue<WorkerBooleanValue>().Value);
        request.UdpNetworkTransmitSettings = null;
        Assert.Throws<ArgumentException>(() => RelationshipWatchWindowTemplateOperation.CreateCommand(request));
    }

    [Fact]
    public void ScalarTolerancesAndOutlierRiskRemainExplicit()
    {
        var request = new Api.SetObjectToObjectDirectionRelationshipTolerancesRequest
        {
            RelationshipName = Item(), AngleBetweenVectorsTolerances = new(),
            MutualPerpendicularLengthTolerances = new()
        };
        var command = SetObjectToObjectDirectionRelationshipTolerancesOperation.CreateCommand(request);
        Assert.Equal(["SetCollectionObjectNameArg2", "SetToleranceScalarOptionsArg", "SetToleranceScalarOptionsArg"],
            command.InputArguments.Select(argument => argument.SdkBinding));
        request.MutualPerpendicularLengthTolerances = null;
        Assert.Throws<ArgumentException>(() => SetObjectToObjectDirectionRelationshipTolerancesOperation.CreateCommand(request));

        Assert.Equal(["fixture_validation_pending"], SetRelationshipOutlierRejectionScalarTypeOperation.Descriptor.RiskFlags);
        Assert.Equal("SetCollectionObjectNameArg2",
            SetRelationshipOutlierRejectionScalarTypeOperation.CreateCommand(new() { RelationshipName = Object("R") })
                .InputArguments[0].SdkBinding);
        Assert.Throws<ArgumentException>(() =>
            SetRelationshipOutlierRejectionScalarTypeOperation.CreateCommand(new()));
    }

    [Fact]
    public void VoxelDisplayPreservesDefaultsAndRejectsUnknownMode()
    {
        var request = new Api.SetRelationshipVoxelCloudDisplayRequest { RelationshipName = Object("R") };
        var command = SetRelationshipVoxelCloudDisplayOperation.CreateCommand(request);
        Assert.Equal(["SetCollectionObjectNameArg2", "SetBoolArg", "SetDoubleArg", "SetIntegerArg",
            "SetDoubleArg", "SetSurfaceAnalysisModeArg", "SetColorizationOptionsArg", "SetBoolArg"],
            command.InputArguments.Select(argument => argument.SdkBinding));
        Assert.True(command.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(-1d, command.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(3, command.InputArguments[3].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(125d, command.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(WorkerSurfaceAnalysisModeValue.Relationship,
            command.InputArguments[5].RequireValue<WorkerChoiceValue<WorkerSurfaceAnalysisModeValue>>().Value);
        Assert.False(command.InputArguments[7].RequireValue<WorkerBooleanValue>().Value);
        request.SurfaceAnalysisMode = (Api.SurfaceAnalysisMode)999;
        Assert.Throws<ArgumentOutOfRangeException>(() => SetRelationshipVoxelCloudDisplayOperation.CreateCommand(request));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedVoxelRoute()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(host.Channel);
        var result = await client.SetRelationshipVoxelCloudDisplayAsync(new() { RelationshipName = Object("R") });
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal("relationship_operations.set_relationship_voxel_cloud_display", Assert.Single(worker.Commands).OperationId);
        var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.SetRelationshipVoxelCloudDisplayAsync(new()));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
        Assert.Single(worker.Commands);
    }

    private static Api.CollectionObjectName Object(string name) => new() { CollectionName = "Relationships", ObjectName = name };
    private static Api.CollectionItemName Item() => new() { CollectionName = "Relationships", ItemName = "R" };

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
