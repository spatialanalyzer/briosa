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

public sealed class TypedObjectReportingOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.is_object_of_type",
        "analysis_operations.set_object_reporting_frame"
    ];

    [Fact]
    public void EachOperationHasOneTypedRegistrationAndNoCatalogEntry()
    {
        foreach (var id in MigratedIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    [Fact]
    public void IsObjectOfTypeDefaultsToAnyAndPreservesTargetChoiceDomain()
    {
        var objectName = new Api.CollectionObjectName { ObjectName = "measured" };
        var defaultCommand = IsObjectOfTypeOperation.CreateCommand(new() { ObjectName = objectName });
        var defaultType = defaultCommand.InputArguments[1].RequireValue<WorkerChoiceValue<WorkerObjectTypeValue>>();
        Assert.Equal(WorkerObjectTypeValue.Any, defaultType.Value);
        Assert.Equal("SetObjectTypeArg", defaultCommand.InputArguments[1].SdkBinding);

        var pointGroupCommand = IsObjectOfTypeOperation.CreateCommand(new()
        {
            ObjectName = objectName,
            ObjectType = Api.ObjectType.PointGroup
        });
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            pointGroupCommand.InputArguments[1].RequireValue<WorkerChoiceValue<WorkerObjectTypeValue>>().Value);

        var enhancedCloud = (Api.ObjectType)5;
        if (Enum.IsDefined(enhancedCloud))
        {
            var enhancedCloudCommand = IsObjectOfTypeOperation.CreateCommand(new()
            {
                ObjectName = objectName,
                ObjectType = enhancedCloud
            });
            Assert.Equal(5, (int)enhancedCloudCommand.InputArguments[1]
                .RequireValue<WorkerChoiceValue<WorkerObjectTypeValue>>().Value);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => IsObjectOfTypeOperation.CreateCommand(new()
            {
                ObjectName = objectName,
                ObjectType = enhancedCloud
            }));
        }

        Assert.Throws<ArgumentException>(() => IsObjectOfTypeOperation.CreateCommand(new()));
        Assert.Throws<ArgumentOutOfRangeException>(() => IsObjectOfTypeOperation.CreateCommand(new()
        {
            ObjectName = objectName,
            ObjectType = Api.ObjectType.Unspecified
        }));

        var completed = Completed(new WorkerRetrievedOutput("Resultant", WorkerMpValueKind.Logical,
            new WorkerBooleanValue(true)));
        Assert.True(IsObjectOfTypeOperation.CreateResult(completed).Resultant);
    }

    [Fact]
    public void SetObjectReportingFrameRequiresBothNamesAndPreservesTheirTypes()
    {
        var command = SetObjectReportingFrameOperation.CreateCommand(new()
        {
            ObjectName = new Api.CollectionObjectName { ObjectName = "measured" },
            ReportingFrame = new Api.CollectionObjectName
            {
                ObjectName = "frame-1",
                ObjectType = Api.ObjectType.Frame
            }
        });

        Assert.Equal(MigratedIds[1], command.OperationId);
        Assert.Equal("Set Object Reporting Frame", command.StepName);
        Assert.Equal("SetCollectionObjectNameArg2", command.InputArguments[0].SdkBinding);
        Assert.Equal("SetCollectionObjectNameArg2", command.InputArguments[1].SdkBinding);
        Assert.Equal(WorkerObjectTypeValue.Any,
            command.InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Frame,
            command.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Throws<ArgumentException>(() => SetObjectReportingFrameOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetObjectReportingFrameOperation.CreateCommand(new()
        {
            ObjectName = new Api.CollectionObjectName { ObjectName = "measured" }
        }));
    }

    [Fact]
    public async Task GeneratedClientReachesBothTypedRoutes()
    {
        var worker = new ObjectReportingWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.AnalysisOperations.AnalysisOperationsClient(channel);
        var deadline = DateTime.UtcNow.AddSeconds(20);

        var typeResult = await client.IsObjectOfTypeAsync(new()
        {
            ObjectName = new Api.CollectionObjectName { ObjectName = "measured" }
        }, new CallOptions(deadline: deadline));
        var frameResult = await client.SetObjectReportingFrameAsync(new()
        {
            ObjectName = new Api.CollectionObjectName { ObjectName = "measured" },
            ReportingFrame = new Api.CollectionObjectName { ObjectName = "frame-1", ObjectType = Api.ObjectType.Frame }
        }, new CallOptions(deadline: deadline));

        Assert.True(typeResult.Resultant);
        Assert.Equal(Api.MpExecutionState.Succeeded, frameResult.Execution.State);
        Assert.Equal(MigratedIds, worker.Commands.Select(command => command.OperationId));
    }

    private static SuccessfulOperationExecution Completed(params WorkerMpOutputValue[] outputs) => new(
        WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed"),
        new Api.MpExecutionDetails());

    private sealed class ObjectReportingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId == MigratedIds[0]
                ? [new WorkerRetrievedOutput("Resultant", WorkerMpValueKind.Logical, new WorkerBooleanValue(true))]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
