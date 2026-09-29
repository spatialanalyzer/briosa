using System.Collections.Immutable;
using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class StringSetterOperationTests
{
    [Fact]
    public async Task GeneratedClientsUseBothTypedRoutes()
    {
        var worker = new RecordingWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService, GdtOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;

        var construction = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var wildcard = await construction.MakeCollectionItemNameRefListWildcardSelectionAsync(
            new() { ItemType = ItemType.Any }, deadline: DateTime.UtcNow.AddSeconds(20));
        Assert.Equal(MpExecutionState.Succeeded, wildcard.Execution.State);
        Assert.Empty(wildcard.ResultantCollectionItemNameRefList);

        var gdt = new Api.GdtOperations.GdtOperationsClient(channel);
        var options = await gdt.SetGdtOptionsAsync(new()
        {
            DistanceBetweenMode = GdtDistanceBetweenMode.Centroid,
            EvaluationMethod = GdtEvaluationMethod.None
        }, deadline: DateTime.UtcNow.AddSeconds(20));
        Assert.Equal(MpExecutionState.Succeeded, options.Execution.State);
        Assert.Equal(["construction_operations.make_collection_item_name_ref_list_wildcard_selection",
            "gdt_operations.set_gdt_options"], worker.Commands.Select(command => command.OperationId));
    }

    [Fact]
    public void WildcardSelectionUsesTypedItemChoiceAndStringSetter()
    {
        var command = MakeCollectionItemNameRefListWildcardSelectionOperation.CreateCommand(new()
        {
            ItemType = ItemType.Any
        });

        Assert.Equal("Make a Collection Item Name Reference List - WildCard Selection", command.StepName);
        Assert.Equal(["Collection Wildcard Criteria", "Item Wildcard Criteria", "Item Type"],
            command.InputArguments.Select(argument => argument.Name));
        Assert.All(command.InputArguments, argument => Assert.Equal("SetStringArg", argument.SdkBinding));
        Assert.Equal("*", command.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("*", command.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(WorkerItemTypeValue.Any,
            command.InputArguments[2].RequireValue<WorkerChoiceValue<WorkerItemTypeValue>>().Value);
        Assert.Contains(SpatialAnalyzerApi.Operations, descriptor =>
            descriptor.OperationId == MakeCollectionItemNameRefListWildcardSelectionOperation.Descriptor.OperationId);
    }

    [Fact]
    public void GdtOptionsUsesStringSettersForReviewedChoices()
    {
        var command = SetGdtOptionsOperation.CreateCommand(new()
        {
            DistanceBetweenMode = GdtDistanceBetweenMode.MinMax,
            EvaluationMethod = GdtEvaluationMethod.Iso2017
        });

        Assert.Equal("Set GD&T Options", command.StepName);
        Assert.Equal("SetStringArg", command.InputArguments[3].SdkBinding);
        Assert.Equal("SetStringArg", command.InputArguments[4].SdkBinding);
        Assert.Equal(WorkerGdtDistanceBetweenModeValue.MinMax,
            command.InputArguments[3].RequireValue<WorkerChoiceValue<WorkerGdtDistanceBetweenModeValue>>().Value);
        Assert.Equal(WorkerGdtEvaluationMethodValue.Iso2017,
            command.InputArguments[4].RequireValue<WorkerChoiceValue<WorkerGdtEvaluationMethodValue>>().Value);
        Assert.Equal(0.039370, command.InputArguments[7].RequireValue<WorkerDoubleValue>().Value);
        Assert.Contains(SpatialAnalyzerApi.Operations, descriptor =>
            descriptor.OperationId == SetGdtOptionsOperation.Descriptor.OperationId);
    }

    [Fact]
    public void RequiredChoicesRejectOmission()
    {
        Assert.Throws<ArgumentException>(() =>
            MakeCollectionItemNameRefListWildcardSelectionOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetGdtOptionsOperation.CreateCommand(new()));
    }

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var outputs = command.OperationId.StartsWith("construction_operations.", StringComparison.Ordinal)
                ? new WorkerRetrievedOutput[]
                {
                    new("Resultant Collection Item Name Reference List", WorkerMpValueKind.CollectionItemNameList,
                        new WorkerCollectionItemNameListValue(ImmutableArray<WorkerCollectionItemNameValue>.Empty))
                }
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1,
                outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(
                WorkerExecutionStatus.Completed, WorkerExecutionDisposition.Completed,
                execution, null, "completed", 1));
        }
    }
}
