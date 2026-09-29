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

public sealed class TypedInstrumentUsmnOperationTests
{
    private const string OperationId = "instrument_operations.locate_instruments_usmn";

    [Fact]
    public void UsmnCommandPreservesExactDefaultsAndExplicitChoiceMapping()
    {
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == OperationId);

        var request = ValidRequest();
        var command = LocateInstrumentsUsmnOperation.CreateCommand(request);
        Assert.Equal(13, command.InputArguments.Count);
        Assert.Single(command.InputArguments[0].RequireValue<WorkerCollectionInstrumentIdListValue>().Values);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            command.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            command.InputArguments[2].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.False(command.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(command.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerShowUsmnDialogTypeValue.No,
            command.InputArguments[5].RequireValue<WorkerChoiceValue<WorkerShowUsmnDialogTypeValue>>().Value);
        Assert.Equal(0, command.InputArguments[6].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, command.InputArguments[7].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Any,
            command.InputArguments[8].RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectType);
        Assert.False(command.InputArguments[9].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(command.InputArguments[10].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(300, command.InputArguments[11].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(4.0, command.InputArguments[12].RequireValue<WorkerDoubleValue>().Value);

        var enumMappings = new[]
        {
            (Api.ShowUsmnDialog.No, WorkerShowUsmnDialogTypeValue.No),
            (Api.ShowUsmnDialog.Yes, WorkerShowUsmnDialogTypeValue.Yes),
            (Api.ShowUsmnDialog.OnToleranceViolation, WorkerShowUsmnDialogTypeValue.OnToleranceViolation)
        };
        foreach (var (protocolValue, workerValue) in enumMappings)
        {
            var mapped = LocateInstrumentsUsmnOperation.CreateCommand(ValidRequest(protocolValue));
            Assert.Equal(workerValue,
                mapped.InputArguments[5].RequireValue<WorkerChoiceValue<WorkerShowUsmnDialogTypeValue>>().Value);
        }

        var customized = LocateInstrumentsUsmnOperation.CreateCommand(new Api.LocateInstrumentsUsmnRequest
        {
            Instruments = { request.Instruments },
            NominalsGroup = request.NominalsGroup,
            OutputGroup = request.OutputGroup,
            ShowUsmnDialog = Api.ShowUsmnDialog.Yes,
            ExcludedGroups = { request.ExcludedGroups },
            MoveInWorkingFrame = true,
            AutoRejectOutliersAndResolve = true,
            MaximumAcceptableRmsError = 0.2,
            MaximumAcceptableError = 0.5,
            ExcludeSingleInstrumentPoints = true,
            RunUncertaintyFieldAnalysis = true,
            AnalysisSamples = 25,
            AnalysisTimeLimit = 0
        });
        Assert.True(customized.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0.2, customized.InputArguments[6].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(25, customized.InputArguments[11].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(0, customized.InputArguments[12].RequireValue<WorkerDoubleValue>().Value);

        Assert.Throws<ArgumentException>(() =>
            LocateInstrumentsUsmnOperation.CreateCommand(new Api.LocateInstrumentsUsmnRequest()));
        Assert.Throws<ArgumentException>(() =>
            LocateInstrumentsUsmnOperation.CreateCommand(ValidRequest(Api.ShowUsmnDialog.Unspecified)));
        Assert.Throws<ArgumentException>(() =>
            LocateInstrumentsUsmnOperation.CreateCommand(ValidRequest((Api.ShowUsmnDialog)99)));
    }

    [Fact]
    public async Task GeneratedClientReachesUsmnRouteAndMapsOutputs()
    {
        var worker = new InstrumentWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);

        var result = await client.LocateInstrumentsUsmnAsync(ValidRequest(Api.ShowUsmnDialog.OnToleranceViolation),
            new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));

        Assert.Equal(OperationId, Assert.Single(worker.Commands).OperationId);
        Assert.Equal(WorkerShowUsmnDialogTypeValue.OnToleranceViolation,
            worker.Commands[0].InputArguments[5]
                .RequireValue<WorkerChoiceValue<WorkerShowUsmnDialogTypeValue>>().Value);
        Assert.Equal(3.25, result.RmsError);
        Assert.Equal(5.25, result.MaximumError);
    }

    private static Api.LocateInstrumentsUsmnRequest ValidRequest(Api.ShowUsmnDialog showDialog = Api.ShowUsmnDialog.No) => new()
    {
        Instruments = { new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 } },
        NominalsGroup = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Nominals" },
        OutputGroup = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Located" },
        ShowUsmnDialog = showDialog,
        ExcludedGroups = { new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Excluded" } }
    };

    private sealed class InstrumentWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs =
            [
                new WorkerRetrievedOutput("RMS Error Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(3.25)),
                new WorkerRetrievedOutput("Max Error Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(5.25))
            ];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
