using System.Net;
using Briosa.Server.Operations;
using Briosa.Server.Operations.DimensionOperations;
using Briosa.Server.Operations.MpSubroutines;
using Briosa.Server.Operations.MpTaskOverview;
using Briosa.Server.Operations.ProcessFlowOperations;
using Briosa.Server.Operations.ScaleBarOperations;
using Briosa.Server.Operations.VectorOperations;
using Briosa.Server.Operations.WaveA;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Briosa.Server.Workers;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedSmallDomainOperationTests
{
    [Fact]
    public async Task GeneratedClientsReachTypedRoutesAcrossAllFiveSteps()
    {
        var worker = new RouteWorker();
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
            listener => listener.Protocols = HttpProtocols.Http2));
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(new OperationExecutor(worker,
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance), TimeProvider.System));
        await using var app = builder.Build();
        app.MapGrpcService<DimensionOperationsService>();
        app.MapGrpcService<ScaleBarOperationsService>();
        app.MapGrpcService<MpSubroutinesService>();
        app.MapGrpcService<ProcessFlowOperationsService>();
        app.MapGrpcService<MpTaskOverviewService>();
        app.MapGrpcService<VectorOperationsService>();
        await app.StartAsync();
        using var channel = GrpcChannel.ForAddress(Assert.Single(app.Urls));
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var dimension = await new Api.DimensionOperations.DimensionOperationsClient(channel)
            .DeleteDimensionAsync(new() { DimensionName = new() { ObjectName = "d" } }, options);
        var scale = await new Api.ScaleBarOperations.ScaleBarOperationsClient(channel)
            .DeleteScaleBarAsync(new() { ScaleBarName = new() { ObjectName = "s" } }, options);
        var subroutine = await new Api.MpSubroutines.MpSubroutinesClient(channel)
            .RunSubroutineAsync(new() { MpSubroutineFilePath = new() { Path = "test.mp" } }, options);
        var answer = await new Api.ProcessFlowOperations.ProcessFlowOperationsClient(channel)
            .AskForStringAsync(new() { QuestionToAsk = "question" }, options);
        var task = await new Api.MpTaskOverview.MpTaskOverviewClient(channel)
            .ShowTaskOverviewListAsync(new() { Show = true }, options);
        var count = await new Api.VectorOperations.VectorOperationsClient(channel)
            .GetNumberOfVectorsInVectorGroupAsync(new() { VectorGroupName = new() { ObjectName = "group" } }, options);

        Assert.All(new[] { dimension.Execution, scale.Execution, subroutine.Execution, answer.Execution,
            task.Execution, count.Execution }, details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.True(answer.HasAnswer);
        Assert.Equal("", answer.Answer);
        Assert.True(count.HasTotalCount);
        Assert.Equal(0, count.TotalCount);
        Assert.Equal(6, worker.Commands.Count);
        Assert.Equal(6, worker.Commands.Select(command => command.OperationId).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void MigratedDomainsHaveOneRegistrationAndNoLegacyCatalogEntry()
    {
        var families = new Dictionary<string, int>
        {
            ["dimension_operations."] = 3,
            ["scale_bar_operations."] = 4,
            ["mp_subroutines."] = 1,
            ["process_flow_operations."] = 8,
            ["mp_task_overview."] = 10,
            ["vector_operations."] = 15
        };
        foreach (var (prefix, count) in families)
        {
            Assert.Equal(count, SpatialAnalyzerApi.Operations.Count(item => item.OperationId.StartsWith(prefix, StringComparison.Ordinal)));
            Assert.DoesNotContain(MpOperationCatalog.Operations,
                item => item.Descriptor.OperationId.StartsWith(prefix, StringComparison.Ordinal));
        }
        Assert.All(SpatialAnalyzerApi.Operations.Where(item =>
                item.OperationId.StartsWith("scale_bar_operations.", StringComparison.Ordinal)),
            item => Assert.Contains("fixture_validation_pending", item.RiskFlags));
        Assert.Contains("fixture_validation_pending", RunSubroutineOperation.Descriptor.RiskFlags);
    }

    [Fact]
    public void RequiredValuesAreCheckedBeforeSdkDispatch()
    {
        Assert.Throws<ArgumentException>(() => SetDimensionToleranceOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => ScaleBarCheckOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => RunSubroutineOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => AskForStringPullDownVersionOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => AskForUserDecisionFromStringsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetOverviewImageOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => DeleteVectorsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => AutoRangeAndSetVectorGroupColorizationSelectedOperation.CreateCommand(new()));
    }

    [Fact]
    public void ProcessFlowPreservesListBindingsAndDefaultFont()
    {
        var pullDown = AskForStringPullDownVersionOperation.CreateCommand(new()
        {
            QuestionOrStatement = { "Choose" }, PossibleAnswers = { "A", "B" }
        });
        Assert.Equal(["SetStringRefListArg", "SetStringRefListArg", "SetFontTypeArg"],
            pullDown.InputArguments.Select(item => item.SdkBinding));
        Assert.Equal("MS Shell Dlg", pullDown.InputArguments[2].RequireValue<WorkerFontValue>().FontName);
        var decision = AskForUserDecisionFromStringsOperation.CreateCommand(new()
        {
            QuestionOrStatement = { "Decide" }
        });
        Assert.Equal(WorkerMpValueKind.EditText, decision.InputArguments[0].Kind);
        Assert.Equal("SetEditTextArg", decision.InputArguments[0].SdkBinding);
        Assert.Empty(decision.InputArguments[0].RequireValue<WorkerStringListValue>().Values.Skip(1));
    }

    [Fact]
    public void VectorAndScaleDefaultsPreserveReviewedMpBehavior()
    {
        var scale = SetInwardPositiveNormalOperation.CreateCommand(new()
        {
            ObjectName = new() { ObjectName = "bar" }
        });
        Assert.True(scale.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        var disabled = SetInwardPositiveNormalOperation.CreateCommand(new()
        {
            ObjectName = new() { ObjectName = "bar" }, InwardPositive = false
        });
        Assert.False(disabled.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);

        var vector = new Api.VectorName { Name = "v" };
        var sort = SortVectorsOperation.CreateCommand(new() { SourceVectors = { vector } });
        Assert.Equal("Magnitude", sort.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(WorkerCoordinateSystemTypeValue.Cartesian,
            sort.InputArguments[2].RequireValue<WorkerChoiceValue<WorkerCoordinateSystemTypeValue>>().Value);
        Assert.Equal("X (R)", sort.InputArguments[3].RequireValue<WorkerTextValue>().Value);
        var colors = SetVectorGroupColorizationOptionsAllOperation.CreateCommand(new());
        var color = colors.InputArguments[0].RequireValue<WorkerColorizationOptionsValue>();
        Assert.Equal(100, color.VectorMagnification);
        Assert.True(color.DrawArrowheads);
        Assert.Equal("Add a Vector to Vector Name Ref List", AddAVectorToVectorNameRefListOperation.Descriptor.MpStep);
    }

    [Fact]
    public void VectorStatisticsRetainIntegerSdkBindingsAndPublicPresence()
    {
        var command = GetVectorGroupPropertiesOperation.CreateCommand(new()
        {
            VectorGroupName = new() { ObjectName = "group" }
        });
        Assert.Equal("GetIntegerArg", command.OutputArguments[4].SdkBinding);
        Assert.Equal(WorkerMpValueKind.WholeNumber, command.OutputArguments[4].Kind);
        var outputs = command.OutputArguments.Select((item, index) =>
            (WorkerMpOutputValue)new WorkerRetrievedOutput(item.Name, item.Kind,
                item.Kind == WorkerMpValueKind.WholeNumber
                    ? new WorkerIntegerValue(index)
                    : new WorkerDoubleValue(index))).ToArray();
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
        var result = GetVectorGroupPropertiesOperation.CreateResult(
            new SuccessfulOperationExecution(execution, new Api.MpExecutionDetails()));
        Assert.True(result.HasVectorsInTolerance2);
        Assert.Equal(4d, result.VectorsInTolerance2);
        Assert.True(result.HasRmsValue);
        Assert.Equal(16d, result.RmsValue);
    }

    private sealed class RouteWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "process_flow_operations.ask_for_string" =>
                    [new WorkerRetrievedOutput("Answer", WorkerMpValueKind.Text, new WorkerTextValue(""))],
                "vector_operations.get_number_of_vectors_in_vector_group" =>
                    [new WorkerRetrievedOutput("Total Count", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(0))],
                _ => []
            };
            Assert.Equal(command.OutputArguments.Select(item => item.Name), outputs.Select(item => item.Name));
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
