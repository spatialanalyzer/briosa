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

public sealed class TypedReportNotificationOperationTests
{
    private static readonly string[] Ids =
    [
        "reporting_operations.notify_user_double", "reporting_operations.notify_user_html",
        "reporting_operations.notify_user_integer", "reporting_operations.notify_user_text_array"
    ];

    [Fact]
    public void NotificationOperationsAreRegisteredAndPreserveAtRiskStatus()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Contains("fixture_validation_pending", NotifyUserDoubleOperation.Descriptor.RiskFlags);
        Assert.Contains("fixture_validation_pending", NotifyUserIntegerOperation.Descriptor.RiskFlags);
        Assert.Contains("fixture_validation_pending", NotifyUserTextArrayOperation.Descriptor.RiskFlags);
    }

    [Fact]
    public void NotificationMappingsPreserveDefaultsBindingsAndRequiredFiles()
    {
        var doubleNotice = NotifyUserDoubleOperation.CreateCommand(new());
        Assert.Equal(string.Empty, doubleNotice.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        AssertDefaultFont(doubleNotice.InputArguments[1].RequireValue<WorkerFontValue>());
        Assert.Equal(0d, doubleNotice.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal([0, 0], doubleNotice.InputArguments.Skip(3)
            .Select(argument => argument.RequireValue<WorkerIntegerValue>().Value));

        Assert.Throws<ArgumentException>(() => NotifyUserHtmlOperation.CreateCommand(new()));
        Assert.Equal("message.html", NotifyUserHtmlOperation.CreateCommand(new()
        {
            HtmlFile = File("message.html")
        }).InputArguments[0].RequireValue<WorkerFileReferenceValue>().Path);

        var integerNotice = NotifyUserIntegerOperation.CreateCommand(new());
        Assert.Equal(string.Empty, integerNotice.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        AssertDefaultFont(integerNotice.InputArguments[1].RequireValue<WorkerFontValue>());
        Assert.Equal([0, 0], integerNotice.InputArguments.Skip(2)
            .Select(argument => argument.RequireValue<WorkerIntegerValue>().Value));

        Assert.Throws<ArgumentException>(() => NotifyUserTextArrayOperation.CreateCommand(new()));
        var textNotice = NotifyUserTextArrayOperation.CreateCommand(new()
        {
            NotificationText = { "First line", "Second line" }
        });
        Assert.Equal(["First line", "Second line"], textNotice.InputArguments[0]
            .RequireValue<WorkerStringListValue>().Values);
        Assert.Equal("SetEditTextArg", textNotice.InputArguments[0].SdkBinding);
        AssertDefaultFont(textNotice.InputArguments[1].RequireValue<WorkerFontValue>());
        Assert.False(textNotice.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0, textNotice.InputArguments[3].RequireValue<WorkerIntegerValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientRoutesNotificationOperationsThroughTypedMappings()
    {
        var worker = new NotificationWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ReportingOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ReportingOperations.ReportingOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        await client.NotifyUserDoubleAsync(new(), options);
        await client.NotifyUserHtmlAsync(new() { HtmlFile = File("message.html") }, options);
        await client.NotifyUserIntegerAsync(new(), options);
        await client.NotifyUserTextArrayAsync(new() { NotificationText = { "Notice" } }, options);

        Assert.Equal(Ids, worker.Commands.Select(command => command.OperationId));
    }

    private static void AssertDefaultFont(WorkerFontValue font)
    {
        Assert.Equal("MS Shell Dlg", font.FontName);
        Assert.Equal((byte)8, font.Size);
        Assert.Equal(new WorkerRgbColorValue(0, 0, 0), font.Color);
    }

    private static Api.FileReference File(string path) => new() { Path = path };

    private sealed class NotificationWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
