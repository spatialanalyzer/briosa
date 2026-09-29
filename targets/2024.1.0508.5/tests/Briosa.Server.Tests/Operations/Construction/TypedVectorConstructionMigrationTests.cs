using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Security;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedVectorConstructionMigrationTests
{
    [Fact]
    public async Task GeneratedClientRoutesFiveTypedVectorCommandsAndRetrievesComparison()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var lifetime = host.ConfigureAwait(true);
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(host.Channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        await client.ConstructVectorGroupAreaProfileCheckAsync(new()
        {
            ReferenceVectors = { new Api.VectorName { CollectionName = "part", GroupName = "vectors", Name = "reference" } },
            VectorGroupsToCheck = { Group("check") }, ResultantVectorGroupName = Group("result")
        }, options);
        await client.ConstructVectorGroupFromRelationshipAsync(new()
        {
            RelationshipName = Object("relationship"), VectorGroupName = Group("result")
        }, options);
        var comparison = await client.ConstructVectorGroupGroupToGroupCompareAsync(new()
        {
            VectorGroupName = Object("vectors"), GroupA = Object("a"), GroupB = Object("b")
        }, options);
        await client.ConstructVectorInWorkingCoordinatesBeginDeltaAsync(new()
        {
            VectorGroupName = Object("vectors"), BeginInWorkingCoordinates = Vector(1), DeltaInWorkingCoordinates = Vector(2)
        }, options);
        await client.ConstructVectorInWorkingCoordinatesBeginDirectionMagnitudeAsync(new()
        {
            VectorGroupName = Object("vectors"), BeginInWorkingCoordinates = Vector(1), DirectionInWorkingCoordinates = Vector(2)
        }, options);

        Assert.Equal(5, worker.Commands.Count);
        Assert.Equal(["Reference Vectors", "Vector Groups to Check", "Area Radius", "Area Tolerance", "Resultant Vector Group Name"], worker.Commands[0].InputArguments.Select(a => a.Name));
        Assert.Equal("SetVectorNameRefListArg", worker.Commands[0].InputArguments[0].SdkBinding);
        Assert.Equal("SetCollectionVectorGroupNameRefListArg", worker.Commands[0].InputArguments[1].SdkBinding);
        Assert.Equal("SetColVectorGroupNameArg", worker.Commands[0].InputArguments[4].SdkBinding);
        Assert.Equal("SetCollectionObjectNameArg2", worker.Commands[1].InputArguments[0].SdkBinding);
        Assert.Equal(WorkerObjectTypeValue.VectorGroup, worker.Commands[2].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.PointGroup, worker.Commands[2].InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(["GetIntegerArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg"], worker.Commands[2].OutputArguments.Select(a => a.SdkBinding));
        Assert.Equal(3, comparison.VectorCount);
        Assert.Equal(0.25, comparison.RmsDeviation);
        Assert.Equal(0.5, comparison.MaxAbsoluteDeviation);
        Assert.Equal(0.125, comparison.AverageDeviation);
        Assert.Equal("", worker.Commands[3].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.False(worker.Commands[3].InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("SetVectorArg", worker.Commands[3].InputArguments[3].SdkBinding);
        Assert.Equal(0, worker.Commands[4].InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("SetVectorArg", worker.Commands[4].InputArguments[3].SdkBinding);
        foreach (var id in new[] { "construct_vector_group_area_profile_check", "construct_vector_group_from_relationship", "construct_vector_group_group_to_group_compare", "construct_vector_in_working_coordinates_begin_delta", "construct_vector_in_working_coordinates_begin_direction_magnitude" })
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == $"construction_operations.{id}");
        }
    }

    [Fact]
    public void RequiredVectorInputsAreValidatedForEveryCommand()
    {
        Assert.Throws<ArgumentException>(() => ConstructVectorGroupAreaProfileCheckOperation.CreateCommand(new() { ResultantVectorGroupName = Group("result") }));
        Assert.Throws<ArgumentException>(() => ConstructVectorGroupFromRelationshipOperation.CreateCommand(new() { RelationshipName = Object("relationship") }));
        Assert.Throws<ArgumentException>(() => ConstructVectorGroupGroupToGroupCompareOperation.CreateCommand(new() { VectorGroupName = Object("vectors"), GroupA = Object("a") }));
        Assert.Throws<ArgumentException>(() => ConstructVectorInWorkingCoordinatesBeginDeltaOperation.CreateCommand(new() { VectorGroupName = Object("vectors"), BeginInWorkingCoordinates = Vector(1) }));
        Assert.Throws<ArgumentException>(() => ConstructVectorInWorkingCoordinatesBeginDirectionMagnitudeOperation.CreateCommand(new() { VectorGroupName = Object("vectors"), BeginInWorkingCoordinates = Vector(1) }));
    }

    private static Api.CollectionObjectName Object(string name) => new() { CollectionName = "part", ObjectName = name };
    private static Api.CollectionVectorGroupName Group(string name) => new() { CollectionName = "part", VectorGroupName = name };
    private static Api.Vector Vector(double x) => new() { X = x, Y = 0, Z = 0 };

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];
        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId == "construction_operations.construct_vector_group_group_to_group_compare"
                ? [
                    new WorkerRetrievedOutput("Vector Count", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(3)),
                    new WorkerRetrievedOutput("RMS Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.25)),
                    new WorkerRetrievedOutput("Max Absolute Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.5)),
                    new WorkerRetrievedOutput("Average Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.125))
                ] : [];
            var result = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, result, null, "completed", 1));
        }
    }
}
