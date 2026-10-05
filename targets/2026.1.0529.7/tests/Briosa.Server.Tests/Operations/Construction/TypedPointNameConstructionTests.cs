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

public sealed class TypedPointNameConstructionTests
{
    [Fact]
    public async Task GeneratedClientUsesTypedPointNameAndPointReferenceListRoutes()
    {
        var worker = new PointNameWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var selectedPoint = await client.MakePointNameRuntimeSelectAsync(new(), options);
        var uniquePoint = await client.MakePointNameEnsureUniqueAsync(new()
        {
            PointName = new() { CollectionName = "Parts", GroupName = "Points", TargetName = "P1" }
        }, options);
        var groupPoints = await client.MakePointNameRefListFromGroupAsync(new()
        {
            GroupName = new() { CollectionName = "Parts", ObjectName = "Points" }
        }, options);
        var selectedPoints = await client.MakePointNameRefListRuntimeSelectAsync(new()
        {
            UserPrompt = "Select points"
        }, options);
        var wildcardPoints = await client.MakePointNameRefListWildcardSelectAsync(new(), options);

        Assert.All(new[] { selectedPoint.Execution, uniquePoint.Execution, groupPoints.Execution,
            selectedPoints.Execution, wildcardPoints.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal("P1", selectedPoint.ResultantPointName.TargetName);
        Assert.Equal("P2", uniquePoint.ResultantPointName.TargetName);
        Assert.Equal("P3", Assert.Single(groupPoints.ResultantPointNameList).TargetName);
        Assert.Equal("P4", Assert.Single(selectedPoints.ResultantPointNameList).TargetName);
        Assert.Equal("P5", Assert.Single(wildcardPoints.ResultantPointNameList).TargetName);
        Assert.Equal(5, worker.Commands.Count);

        Assert.Equal(string.Empty, worker.Commands[0].InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("SetPointNameArg", worker.Commands[1].InputArguments[0].SdkBinding);
        Assert.False(worker.Commands[1].InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            worker.Commands[2].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal("SetCollectionObjectNameArg2", worker.Commands[2].InputArguments[0].SdkBinding);
        Assert.Equal("Select points", worker.Commands[3].InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.All(worker.Commands[4].InputArguments, argument =>
            Assert.Equal("*", argument.RequireValue<WorkerTextValue>().Value));

        var missingPointName = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.MakePointNameEnsureUniqueAsync(new(), options));
        var missingGroupName = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.MakePointNameRefListFromGroupAsync(new(), options));
        Assert.All(new[] { missingPointName, missingGroupName },
            exception => Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode));
        Assert.Equal(5, worker.Commands.Count);
    }

    [Fact]
    public void PointNameOperationsAreSingleRegisteredReadOnlyOperations()
    {
        var operations = new[]
        {
            MakePointNameRuntimeSelectOperation.Descriptor,
            MakePointNameEnsureUniqueOperation.Descriptor,
            MakePointNameRefListFromGroupOperation.Descriptor,
            MakePointNameRefListRuntimeSelectOperation.Descriptor,
            MakePointNameRefListWildcardSelectOperation.Descriptor
        };
        foreach (var operation in operations)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateRead, operation.ExecutionScope);
            // Re-prompting an operator is not a safe replay (#242).
            Assert.Equal(
                operation.OperationId.EndsWith("_runtime_select", StringComparison.Ordinal)
                    ? Api.ReplaySafety.Unsafe
                    : Api.ReplaySafety.Safe,
                operation.ReplaySafety);
            Assert.Empty(operation.RiskFlags);
        }
    }

    private sealed class PointNameWorker : IWorkerCommandExecutor
    {
        private int _pointNumber;
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var point = new WorkerPointNameValue("Parts", "Points", $"P{++_pointNumber}");
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "construction_operations.make_point_name_runtime_select" or
                "construction_operations.make_point_name_ensure_unique" =>
                [new WorkerRetrievedOutput(command.OperationId.EndsWith("runtime_select", StringComparison.Ordinal)
                        ? "Resultant Point Name" : "Point Name",
                    WorkerMpValueKind.PointName, point)],
                _ =>
                [new WorkerRetrievedOutput("Resultant Point Name List", WorkerMpValueKind.PointNameList,
                    new WorkerPointNameListValue([point]))]
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
