using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGdtDatumMeasurementsTests
{
    [Fact]
    public async Task GeneratedClientUsesTypedDatumMeasurementRoutes()
    {
        var worker = new DatumWorker();
        var grpcHost = await GrpcTestHost.StartAsync<GdtOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.GdtOperations.GdtOperationsClient(channel);
        var datum = new Api.CollectionItemName { CollectionName = "C", ItemName = "D" };

        var get = await client.GetDatumMeasurementsAsync(new() { Datum = datum },
            deadline: DateTime.UtcNow.AddSeconds(20));
        Assert.Equal(Api.MpExecutionState.Succeeded, get.Execution.State);
        Assert.Equal("P", Assert.Single(get.Measurements.PointNames).TargetName);
        Assert.Equal("Cloud", Assert.Single(get.Measurements.CloudNames).ObjectName);
        Assert.Equal(["SetCollectionObjectNameArg2"], worker.Commands[0].InputArguments.Select(x => x.SdkBinding));
        Assert.Equal(["GetPointNameRefListArg", "GetCollectionObjectNameRefListArg"],
            worker.Commands[0].OutputArguments.Select(x => x.SdkBinding));

        var set = await client.SetDatumMeasurementsAsync(new()
        {
            Datum = datum,
            PointNames = { new Api.PointName { CollectionName = "C", TargetName = "P" } },
            CloudNames = { new Api.CollectionObjectName { CollectionName = "C", ObjectName = "Cloud" } }
        }, deadline: DateTime.UtcNow.AddSeconds(20));
        Assert.Equal(Api.MpExecutionState.Succeeded, set.Execution.State);
        Assert.Equal(["SetCollectionObjectNameArg2", "SetPointNameRefListArg",
            "SetCollectionObjectNameRefListArg", "SetBoolArg"],
            worker.Commands[1].InputArguments.Select(x => x.SdkBinding));
        Assert.False(worker.Commands[1].InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public void RequiredInputsAndRegistryAreEnforced()
    {
        Assert.Throws<ArgumentException>(() => GetDatumMeasurementsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetDatumMeasurementsOperation.CreateCommand(new()
            { Datum = new Api.CollectionItemName { ItemName = "D" } }));
        foreach (var id in new[] { GetDatumMeasurementsOperation.Descriptor.OperationId,
                     SetDatumMeasurementsOperation.Descriptor.OperationId })
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    private sealed class DatumWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerRetrievedOutput[] outputs = command.OperationId.EndsWith("get_datum_measurements", StringComparison.Ordinal)
                ? [
                    new("Point Names", WorkerMpValueKind.PointNameList,
                        new WorkerPointNameListValue([new WorkerPointNameValue("C", "", "P")])),
                    new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList,
                        new WorkerCollectionObjectNameListValue(
                            [new WorkerCollectionObjectNameValue("C", "Cloud", WorkerObjectTypeValue.Cloud)]))
                  ]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(
                true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(
                WorkerExecutionStatus.Completed, WorkerExecutionDisposition.Completed,
                execution, null, "completed", 1));
        }
    }
}
