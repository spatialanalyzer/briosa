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

public sealed class TypedInstrumentGuideAndQuickAlignOperationTests
{
    private const string GuideId = "instrument_operations.guide_objects_in_6d_based_on_point_measurements";
    private const string QuickAlignId = "instrument_operations.quick_align";

    [Fact]
    public void GuideAndQuickAlignCommandsPreserveRequiredAndOptionalLists()
    {
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == GuideId);
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == QuickAlignId);

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var destination = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Destination" };
        var movingReference = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "MovingReference" };
        var movedObject = new Api.CollectionObjectName { CollectionName = "CAD", ObjectName = "Part" };
        var guide = GuideObjectsIn6dBasedOnPointMeasurementsOperation.CreateCommand(new()
        {
            Instrument = instrument,
            DestinationGroup = destination,
            MovingReferenceGroup = movingReference,
            ObjectsToMove = { movedObject }
        });
        Assert.Equal(4, guide.InputArguments.Count);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            guide.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            guide.InputArguments[2].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Any,
            guide.InputArguments[3].RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectType);

        var guideWithOptions = GuideObjectsIn6dBasedOnPointMeasurementsOperation.CreateCommand(new()
        {
            Instrument = instrument,
            DestinationGroup = destination,
            MovingReferenceGroup = movingReference,
            ObjectsToMove = { movedObject },
            InitialSurveyGroup = new() { CollectionName = "Points", ObjectName = "InitialSurvey" },
            PositionalTolerance = new(),
            RotationalTolerance = new()
        });
        Assert.Equal(7, guideWithOptions.InputArguments.Count);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            guideWithOptions.InputArguments[4].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.IsType<WorkerToleranceVectorOptionsValue>(guideWithOptions.InputArguments[5].Value);
        Assert.IsType<WorkerToleranceVectorOptionsValue>(guideWithOptions.InputArguments[6].Value);

        var quickAlign = QuickAlignOperation.CreateCommand(new()
        {
            Instruments = { instrument },
            Objects = { movedObject }
        });
        Assert.Equal(3, quickAlign.InputArguments.Count);
        Assert.Single(quickAlign.InputArguments[0].RequireValue<WorkerCollectionInstrumentIdListValue>().Values);
        Assert.Equal(WorkerObjectTypeValue.Any,
            quickAlign.InputArguments[1].RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectType);
        Assert.False(quickAlign.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);

        var quickAlignWithLists = QuickAlignOperation.CreateCommand(new()
        {
            Instruments = { instrument },
            Objects = { movedObject },
            NominalPoints = { new Api.PointName { CollectionName = "Points", GroupName = "Nominal", TargetName = "P1" } },
            NominalPointOfViewNames = { "Front" },
            AlignToIndividualFacesOnly = true
        });
        Assert.Equal(5, quickAlignWithLists.InputArguments.Count);
        Assert.Equal("P1", quickAlignWithLists.InputArguments[2]
            .RequireValue<WorkerPointNameListValue>().Values[0].TargetName);
        Assert.Equal("Front", quickAlignWithLists.InputArguments[3]
            .RequireValue<WorkerStringListValue>().Values[0]);
        Assert.True(quickAlignWithLists.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedGuideAndQuickAlignRoutes()
    {
        var worker = new InstrumentWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var group = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Group" };
        var movedObject = new Api.CollectionObjectName { CollectionName = "CAD", ObjectName = "Part" };

        await client.GuideObjectsIn6dBasedOnPointMeasurementsAsync(new()
        {
            Instrument = instrument,
            DestinationGroup = group,
            MovingReferenceGroup = group,
            ObjectsToMove = { movedObject }
        }, options);
        await client.QuickAlignAsync(new()
        {
            Instruments = { instrument },
            Objects = { movedObject }
        }, options);

        Assert.Equal(new[] { GuideId, QuickAlignId }, worker.Commands.Select(command => command.OperationId));
        Assert.Equal(4, worker.Commands[0].InputArguments.Count);
        Assert.Equal(3, worker.Commands[1].InputArguments.Count);
    }

    private sealed class InstrumentWorker : IWorkerCommandExecutor
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
