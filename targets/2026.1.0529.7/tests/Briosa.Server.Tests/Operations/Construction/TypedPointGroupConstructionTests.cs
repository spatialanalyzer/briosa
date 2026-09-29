using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedPointGroupConstructionTests
{
    [Fact]
    public async Task GeneratedClientConstructsPointGroupsAndPreservesDefaults()
    {
        var worker = new PointGroupWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var fromCloud = await client.ConstructPointGroupFromPointCloudAsync(new()
        {
            CloudName = new() { CollectionName = "Clouds", ObjectName = "Cloud A" },
            PointGroupName = new() { CollectionName = "Parts", ObjectName = "Points" }
        }, options);
        var fromPointNames = await client.ConstructPointGroupFromPointNameRefListAsync(new()
        {
            PointNameList = { new Api.PointName { TargetName = "P1" }, new Api.PointName { TargetName = "P2" } },
            GroupName = new() { CollectionName = "Parts", ObjectName = "Points" }
        }, options);
        var fromVectorGroups = await client.ConstructPointGroupsFromVectorGroupsAsync(new()
        {
            VectorGroups = { new Api.CollectionObjectName { CollectionName = "Vectors", ObjectName = "Vectors A" } },
            MakeVectorBeginPoints = true
        }, options);
        var copied = await client.CopyGroupsExcludingObscuredPointsAsync(new()
        {
            InstrumentId = new() { CollectionName = "Instruments", InstrumentId = 7 },
            GroupNames = { new Api.CollectionObjectName { CollectionName = "Parts", ObjectName = "Points" } },
            NewCollectionName = new() { Name = "Visible points" }
        }, options);
        var averaged = await client.AverageSetOfGroupsAsync(new()
        {
            GroupNames = { new Api.CollectionObjectName { ObjectName = "Points A" } },
            ResultingGroupName = new() { ObjectName = "Average" }
        }, options);

        Assert.All(new[] { fromCloud.Execution, fromPointNames.Execution,
            fromVectorGroups.Execution, copied.Execution, averaged.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal("Points", Assert.Single(fromVectorGroups.PointGroups).ObjectName);
        Assert.Equal(Api.ObjectType.PointGroup, Assert.Single(fromVectorGroups.PointGroups).ObjectType);
        Assert.Equal(5, worker.Commands.Count);
        Assert.Equal(1.25d, averaged.Statistics.RmsDeviation);
        Assert.Equal(2.5d, averaged.Statistics.MaxAbsoluteDeviation);
        Assert.Equal(0.75d, averaged.Statistics.AverageDeviation);

        var cloudArgs = worker.Commands[0].InputArguments;
        Assert.Equal(WorkerObjectTypeValue.Cloud,
            cloudArgs[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            cloudArgs[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal("pt", cloudArgs[2].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0, cloudArgs[3].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(0d, cloudArgs[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(cloudArgs[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0.5d, cloudArgs[6].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(cloudArgs[7].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("SetPointNameRefListArg", worker.Commands[1].InputArguments[0].SdkBinding);
        Assert.Equal(2, worker.Commands[1].InputArguments[0]
            .RequireValue<WorkerPointNameListValue>().Values.Count);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            worker.Commands[1].InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(3, worker.Commands[2].InputArguments.Count);
        Assert.Equal(WorkerObjectTypeValue.Any,
            Assert.Single(worker.Commands[2].InputArguments[0]
                .RequireValue<WorkerCollectionObjectNameListValue>().Values).ObjectType);
        Assert.True(worker.Commands[2].InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(worker.Commands[2].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(7, worker.Commands[3].InputArguments[0]
            .RequireValue<WorkerCollectionInstrumentIdValue>().InstrumentId);
        Assert.Equal("SetCollectionNameArg", worker.Commands[3].InputArguments[2].SdkBinding);
        var averageArgs = worker.Commands[4].InputArguments;
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            Assert.Single(averageArgs[0].RequireValue<WorkerCollectionObjectNameListValue>().Values).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            averageArgs[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(0d, averageArgs[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, averageArgs[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, averageArgs[4].RequireValue<WorkerDoubleValue>().Value);

        var missingCloud = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointGroupFromPointCloudAsync(new()
            {
                PointGroupName = new() { ObjectName = "Points" }
            }, options));
        var emptyPointNames = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointGroupFromPointNameRefListAsync(new()
            {
                GroupName = new() { ObjectName = "Points" }
            }, options));
        var emptyVectorGroups = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointGroupsFromVectorGroupsAsync(new(), options));
        var missingInstrument = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.CopyGroupsExcludingObscuredPointsAsync(new()
            {
                GroupNames = { new Api.CollectionObjectName { ObjectName = "Points" } },
                NewCollectionName = new() { Name = "Visible points" }
            }, options));
        Assert.All(new[] { missingCloud, emptyPointNames, emptyVectorGroups, missingInstrument },
            exception => Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode));
        Assert.Equal(5, worker.Commands.Count);
    }

    [Fact]
    public void PointGroupOperationsAreRegisteredAsUnsafeGlobalMutations()
    {
        var operations = new[]
        {
            ConstructPointGroupFromPointCloudOperation.Descriptor,
            ConstructPointGroupFromPointNameRefListOperation.Descriptor,
            ConstructPointGroupsFromVectorGroupsOperation.Descriptor,
            CopyGroupsExcludingObscuredPointsOperation.Descriptor,
            AverageSetOfGroupsOperation.Descriptor
        };
        foreach (var operation in operations)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
            Assert.Empty(operation.RiskFlags);
        }

        var explicitSuffix = ConstructPointGroupsFromVectorGroupsOperation.CreateCommand(new()
        {
            VectorGroups = { new Api.CollectionObjectName { ObjectName = "Vectors" } },
            OptionalGroupNameSuffix = string.Empty
        });
        Assert.Equal(4, explicitSuffix.InputArguments.Count);
        Assert.Equal(string.Empty, explicitSuffix.InputArguments[1].RequireValue<WorkerTextValue>().Value);
    }

    private sealed class PointGroupWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "construction_operations.construct_point_groups_from_vector_groups" =>
                [new WorkerRetrievedOutput("Point Groups", WorkerMpValueKind.CollectionObjectNameList,
                    new WorkerCollectionObjectNameListValue([new("Parts", "Points", WorkerObjectTypeValue.PointGroup)]))],
                "construction_operations.average_set_of_groups" =>
                [
                    new WorkerRetrievedOutput("RMS Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1.25)),
                    new WorkerRetrievedOutput("Max Absolute Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2.5)),
                    new WorkerRetrievedOutput("Average Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.75))
                ],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
