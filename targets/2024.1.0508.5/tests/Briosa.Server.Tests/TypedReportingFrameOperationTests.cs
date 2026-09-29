using Briosa.Server.Operations;
using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedReportingFrameOperationTests
{
    [Fact]
    public void CommandsPreserveExactBindingsWithoutAnUnverifiedTypeFallback()
    {
        var objectName = new Api.CollectionObjectName { CollectionName = "Fixture", ObjectName = "Object" };
        var objectCommand = GetObjectReportingFrameOperation.CreateCommand(new() { ObjectName = objectName });
        var relationshipCommand = GetRelationshipReportingFrameOperation.CreateCommand(new()
        {
            RelationshipName = new Api.CollectionObjectName { CollectionName = "Fixture", ObjectName = "Relationship" }
        });

        Assert.Equal("Get Object Reporting Frame", objectCommand.StepName);
        Assert.Equal("Get Relationship Reporting Frame", relationshipCommand.StepName);
        foreach (var command in new[] { objectCommand, relationshipCommand })
        {
            Assert.Equal("SetCollectionObjectNameArg2", Assert.Single(command.InputArguments).SdkBinding);
            var output = Assert.Single(command.OutputArguments);
            Assert.Equal("Reporting Frame", output.Name);
            Assert.Equal("GetCollectionObjectNameArg", output.SdkBinding);
            Assert.Null(output.ObjectTypeWhenOmitted);
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == command.OperationId);
        }

        Assert.Equal(["fixture_validation_pending"], GetObjectReportingFrameOperation.Descriptor.RiskFlags);
        Assert.Equal(["fixture_validation_pending"], GetRelationshipReportingFrameOperation.Descriptor.RiskFlags);
        Assert.Throws<ArgumentException>(() => GetObjectReportingFrameOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetRelationshipReportingFrameOperation.CreateCommand(new()));
    }

    [Fact]
    public async Task GeneratedClientsReceiveTheTypedFrameResult()
    {
        var worker = new RecordingWorker(_ =>
            [new WorkerRetrievedOutput("Reporting Frame", WorkerMpValueKind.CollectionObjectName,
                new WorkerCollectionObjectNameValue("Fixture", "Frame", WorkerObjectTypeValue.Frame))]);
        var analysisHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker);
        await using (analysisHost.ConfigureAwait(true))
        {
            var client = new Api.AnalysisOperations.AnalysisOperationsClient(analysisHost.Channel);
            var result = await client.GetObjectReportingFrameAsync(new()
            {
                ObjectName = new Api.CollectionObjectName { CollectionName = "Fixture", ObjectName = "Object" }
            }, deadline: DateTime.UtcNow.AddSeconds(20));
            Assert.Equal(Api.ObjectType.Frame, result.ReportingFrame.ObjectType);
            Assert.Equal("Frame", result.ReportingFrame.ObjectName);
            Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        }

        var relationshipHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker);
        await using (relationshipHost.ConfigureAwait(true))
        {
            var client = new Api.RelationshipOperations.RelationshipOperationsClient(relationshipHost.Channel);
            var result = await client.GetRelationshipReportingFrameAsync(new()
            {
                RelationshipName = new Api.CollectionObjectName { CollectionName = "Fixture", ObjectName = "Relationship" }
            }, deadline: DateTime.UtcNow.AddSeconds(20));
            Assert.Equal(Api.ObjectType.Frame, result.ReportingFrame.ObjectType);
            Assert.Equal("Frame", result.ReportingFrame.ObjectName);
            Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        }

        Assert.Equal(2, worker.Commands.Count);
    }
}
