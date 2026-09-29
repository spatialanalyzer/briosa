using Briosa.Server.Operations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedInstrumentCloudFitOperationTests
{
    private const string AlignCloudToCadId = "instrument_operations.align_cloud_to_cad";
    private const string QuickFitId = "instrument_operations.locate_instrument_group_to_surface_quick_fit";
    private static readonly double[] TransformValues = Enumerable.Range(0, 16).Select(value => (double)value).ToArray();

    [Fact]
    public void CloudAlignmentAndQuickFitCommandsPreserveExactDefaultsAndObjectTypes()
    {
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == AlignCloudToCadId);
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == QuickFitId);

        var cloud = new Api.CollectionObjectName { CollectionName = "Scan", ObjectName = "Cloud" };
        var surface = new Api.CollectionObjectName { CollectionName = "CAD", ObjectName = "Housing" };
        var alignment = AlignCloudToCadOperation.CreateCommand(new()
        {
            Cloud = cloud,
            Surfaces = { surface }
        });
        Assert.Equal(5, alignment.InputArguments.Count);
        Assert.Equal(WorkerObjectTypeValue.Cloud,
            alignment.InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Any,
            alignment.InputArguments[1].RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectType);
        Assert.Equal(0, alignment.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(alignment.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(alignment.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        var skippedAlignment = AlignCloudToCadOperation.CreateCommand(new()
        {
            Cloud = cloud,
            Surfaces = { surface },
            ExecuteAlignment = false
        });
        Assert.False(skippedAlignment.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);

        var quickFit = LocateInstrumentGroupToSurfaceQuickFitOperation.CreateCommand(new()
        {
            Instrument = new() { CollectionName = "Trackers", InstrumentId = 4 },
            MeasuredGroup = new() { CollectionName = "Points", ObjectName = "Measured" },
            SurfacePointsGroup = new() { CollectionName = "Points", ObjectName = "SurfacePoints" },
            SurfaceToFit = surface,
            OtherObjectsToTransform = { cloud }
        });
        Assert.Equal(7, quickFit.InputArguments.Count);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            quickFit.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            quickFit.InputArguments[2].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Surface,
            quickFit.InputArguments[3].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Any,
            quickFit.InputArguments[4].RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectType);
        Assert.All(quickFit.InputArguments.Skip(5), input =>
            Assert.Equal(0, input.RequireValue<WorkerDoubleValue>().Value));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedCloudAndQuickFitRoutesAndMapsOutputs()
    {
        var worker = new InstrumentWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var cloud = new Api.CollectionObjectName { CollectionName = "Scan", ObjectName = "Cloud" };
        var surface = new Api.CollectionObjectName { CollectionName = "CAD", ObjectName = "Housing" };

        var cloudResult = await client.AlignCloudToCadAsync(new()
        {
            Cloud = cloud,
            Surfaces = { surface }
        }, options);
        var quickFitResult = await client.LocateInstrumentGroupToSurfaceQuickFitAsync(new()
        {
            Instrument = new() { CollectionName = "Trackers", InstrumentId = 4 },
            MeasuredGroup = new() { CollectionName = "Points", ObjectName = "Measured" },
            SurfacePointsGroup = new() { CollectionName = "Points", ObjectName = "SurfacePoints" },
            SurfaceToFit = surface,
            OtherObjectsToTransform = { cloud }
        }, options);

        Assert.Equal(new[] { AlignCloudToCadId, QuickFitId }, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(1.25, cloudResult.Alignment.RmsDeviation);
        Assert.Equal(2.25, cloudResult.Alignment.AverageDeviation);
        Assert.Equal(3.25, cloudResult.Alignment.MaximumAbsoluteDeviation);
        Assert.Equal(TransformValues, cloudResult.Alignment.ResultantTransformInWorking.Values);
        Assert.Equal(4.25, quickFitResult.RmsError);
        Assert.Equal(5.25, quickFitResult.MaximumAbsoluteError);
    }

    private sealed class InstrumentWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                AlignCloudToCadId =>
                [
                    new WorkerRetrievedOutput("RMS Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1.25)),
                    new WorkerRetrievedOutput("Average Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2.25)),
                    new WorkerRetrievedOutput("Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(3.25)),
                    new WorkerRetrievedOutput("Resultant Transform in Working Frame", WorkerMpValueKind.Transform,
                        new WorkerTransformValue(TransformValues))
                ],
                QuickFitId =>
                [
                    new WorkerRetrievedOutput("RMS Error", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(4.25)),
                    new WorkerRetrievedOutput("Maximum Absolute Error", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(5.25))
                ],
                _ => throw new InvalidOperationException($"Unexpected operation {command.OperationId}.")
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
