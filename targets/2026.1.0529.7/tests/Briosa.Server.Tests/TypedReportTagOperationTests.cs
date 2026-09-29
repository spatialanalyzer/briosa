using Briosa.Server.Operations;
using Briosa.Server.Operations.ReportingOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedReportTagOperationTests
{
    private static readonly string[] Ids =
    [
        "reporting_operations.get_defined_report_tags", "reporting_operations.get_report_tag_value",
        "reporting_operations.remove_report_tag", "reporting_operations.set_report_tag_value_from_double",
        "reporting_operations.set_report_tag_value_from_integer", "reporting_operations.set_report_tag_value_from_string"
    ];

    [Fact]
    public void ReportTagOperationsAreRegisteredWithReadAndMutationSafety()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Equal(Api.OperationExecutionScope.GlobalStateRead, GetDefinedReportTagsOperation.Descriptor.ExecutionScope);
        Assert.Equal(Api.ReplaySafety.Safe, GetReportTagValueOperation.Descriptor.ReplaySafety);
        Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, RemoveReportTagOperation.Descriptor.ExecutionScope);
    }

    [Fact]
    public void ReportTagMappingsPreserveTypedGettersAndValues()
    {
        var tagsCommand = GetDefinedReportTagsOperation.CreateCommand(new());
        Assert.Empty(tagsCommand.InputArguments);
        Assert.Equal("GetStringRefListArg", tagsCommand.OutputArguments[0].SdkBinding);
        var tags = GetDefinedReportTagsOperation.CreateResult(Success(
        [new WorkerRetrievedOutput("Defined Tags", WorkerMpValueKind.StringList,
            new WorkerStringListValue(["Customer", "Inspection"]))]));
        Assert.Equal(["Customer", "Inspection"], tags.DefinedTags);

        var valueCommand = GetReportTagValueOperation.CreateCommand(new() { TagName = "Customer" });
        Assert.Equal("Customer", valueCommand.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(["GetStringArg", "GetIntegerArg", "GetDoubleArg"],
            valueCommand.OutputArguments.Select(argument => argument.SdkBinding));
        var value = GetReportTagValueOperation.CreateResult(Success(
        [
            new WorkerRetrievedOutput("Tag Value As String", WorkerMpValueKind.Text, new WorkerTextValue("ready")),
            new WorkerRetrievedOutput("Tag Value As Integer", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(4)),
            new WorkerRetrievedOutput("Tag Value As Double", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2.5d))
        ]));
        Assert.Equal("ready", value.TagValueAsString);
        Assert.Equal(4, value.TagValueAsInteger);
        Assert.Equal(2.5d, value.TagValueAsDouble);

        Assert.Equal(string.Empty, RemoveReportTagOperation.CreateCommand(new()).InputArguments[0]
            .RequireValue<WorkerTextValue>().Value);
        var asDouble = SetReportTagValueFromDoubleOperation.CreateCommand(new()
        {
            TagName = "Tolerance", TagValue = 0.125d
        });
        Assert.Equal(0.125d, asDouble.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        var asInteger = SetReportTagValueFromIntegerOperation.CreateCommand(new()
        {
            TagName = "Count", TagValue = 3
        });
        Assert.Equal(3, asInteger.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        var asString = SetReportTagValueFromStringOperation.CreateCommand(new()
        {
            TagName = "Status", TagValue = "complete"
        });
        Assert.Equal("complete", asString.InputArguments[1].RequireValue<WorkerTextValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientRoutesReportTagOperationsThroughTypedMappings()
    {
        var worker = new ReportTagWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ReportingOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ReportingOperations.ReportingOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var tags = await client.GetDefinedReportTagsAsync(new(), options);
        var tagValue = await client.GetReportTagValueAsync(new() { TagName = "Customer" }, options);
        await client.RemoveReportTagAsync(new() { TagName = "OldTag" }, options);
        await client.SetReportTagValueFromDoubleAsync(new() { TagName = "Tolerance", TagValue = 0.125d }, options);
        await client.SetReportTagValueFromIntegerAsync(new() { TagName = "Count", TagValue = 3 }, options);
        await client.SetReportTagValueFromStringAsync(new() { TagName = "Status", TagValue = "complete" }, options);

        Assert.Equal(["Customer", "Inspection"], tags.DefinedTags);
        Assert.Equal("ready", tagValue.TagValueAsString);
        Assert.Equal(["reporting_operations.get_defined_report_tags", "reporting_operations.get_report_tag_value",
            "reporting_operations.remove_report_tag", "reporting_operations.set_report_tag_value_from_double",
            "reporting_operations.set_report_tag_value_from_integer", "reporting_operations.set_report_tag_value_from_string"],
            worker.Commands.Select(command => command.OperationId));
    }

    private static SuccessfulOperationExecution Success(IReadOnlyList<WorkerMpOutputValue> outputs)
    {
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
        return new(execution, new Api.MpExecutionDetails());
    }

    private sealed class ReportTagWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "reporting_operations.get_defined_report_tags" =>
                [new WorkerRetrievedOutput("Defined Tags", WorkerMpValueKind.StringList,
                    new WorkerStringListValue(["Customer", "Inspection"]))],
                "reporting_operations.get_report_tag_value" =>
                [
                    new WorkerRetrievedOutput("Tag Value As String", WorkerMpValueKind.Text, new WorkerTextValue("ready")),
                    new WorkerRetrievedOutput("Tag Value As Integer", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(4)),
                    new WorkerRetrievedOutput("Tag Value As Double", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2.5d))
                ],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
