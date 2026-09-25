using System.Net;
using Briosa.Server.Operations;
using Briosa.Server.Operations.EventOperations;
using Briosa.Server.Operations.WaveA;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
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

public sealed class TypedEventServiceTests
{
    [Fact]
    public async Task GeneratedClientPreservesEventBindingsDefaultsAndOutputPresence()
    {
        var worker = new EventWorker();
        await using var app = CreateApp(worker);
        await app.StartAsync();
        using var channel = GrpcChannel.ForAddress(Assert.Single(app.Urls));
        var client = new Api.EventOperations.EventOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var eventName = new Api.CollectionObjectName { ObjectName = "event" };
        var events = new[]
        {
            new Api.CollectionItemName { ItemName = "first", ItemType = Api.ItemType.Event },
            new Api.CollectionItemName { ItemName = "second" }
        };
        Assert.Equal(Api.MpExecutionState.Succeeded,
            (await client.DeleteEventAsync(new() { EventName = eventName }, options)).Execution.State);
        var export = new Api.ExportEventRefListRequest
        {
            EventList = { events }, FilePath = new() { Path = "synthetic-events.txt", EmbeddedFile = true }
        };
        await client.ExportEventRefListAsync(export, options);
        export.DecimalPrecision = 0;
        export.OverwriteExistingFile = true;
        await client.ExportEventRefListAsync(export, options);
        var first = await client.GetIthEventFromEventRefListAsync(new() { EventList = { events } }, options);
        Assert.Equal(events[0], first.ResultantItem);
        Assert.True(first.ResultantItem.HasItemType);
        Assert.Equal("resultant_item", Assert.Single(first.Execution.OutputRetrievals).FieldName);
        var second = await client.GetIthEventFromEventRefListAsync(new() { EventList = { events }, EventIndex = 1 }, options);
        Assert.Equal("second", second.ResultantItem.ItemName);
        Assert.Equal(Api.ItemType.Any, second.ResultantItem.ItemType);
        var count = await client.GetNumberOfEventsInEventRefListAsync(new() { EventList = { events } }, options);
        Assert.True(count.HasTotalCount);
        Assert.Equal(0, count.TotalCount); // Explicit zero output must survive mapping.
        var rename = new Api.RenameEventRequest { OriginalEventName = eventName, NewEventName = new() { ObjectName = "renamed" } };
        await client.RenameEventAsync(rename, options);
        rename.OverwriteIfExists = true;
        await client.RenameEventAsync(rename, options);

        Assert.Equal(WorkerObjectTypeValue.Any,
            worker.Commands[0].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.True(worker.Commands[1].InputArguments[1].RequireValue<WorkerFileReferenceValue>().EmbeddedFile);
        Assert.Equal(6, worker.Commands[1].InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(worker.Commands[1].InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0, worker.Commands[2].InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.True(worker.Commands[2].InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0, worker.Commands[3].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(worker.Commands[6].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(worker.Commands[7].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        foreach (var id in worker.Commands.Select(command => command.OperationId).Distinct(StringComparer.Ordinal))
        {
            Assert.DoesNotContain(MpOperationCatalog.Operations, operation => operation.Descriptor.OperationId == id);
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }
        Assert.Equal(5, worker.Commands.Select(command => command.OperationId).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task MissingRequiredValuesAndInvalidItemChoicesDoNotReachTheWorker()
    {
        var worker = new EventWorker();
        await using var app = CreateApp(worker);
        await app.StartAsync();
        using var channel = GrpcChannel.ForAddress(Assert.Single(app.Urls));
        var client = new Api.EventOperations.EventOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        Func<Task>[] invalidCalls =
        [
            async () => await client.DeleteEventAsync(new(), options),
            async () => await client.DeleteEventAsync(new() { EventName = new() }, options),
            async () => await client.ExportEventRefListAsync(new() { FilePath = new() { Path = "synthetic" } }, options),
            async () => await client.ExportEventRefListAsync(new() { EventList = { new Api.CollectionItemName() } }, options),
            async () => await client.ExportEventRefListAsync(new() { EventList = { new Api.CollectionItemName() }, FilePath = new() { Path = " " } }, options),
            async () => await client.GetIthEventFromEventRefListAsync(new(), options),
            async () => await client.GetNumberOfEventsInEventRefListAsync(new(), options),
            async () => await client.GetNumberOfEventsInEventRefListAsync(new() { EventList = { new Api.CollectionItemName { ItemType = Api.ItemType.Unspecified } } }, options),
            async () => await client.GetNumberOfEventsInEventRefListAsync(new() { EventList = { new Api.CollectionItemName { ItemType = (Api.ItemType)int.MaxValue } } }, options),
            async () => await client.RenameEventAsync(new() { OriginalEventName = new() { ObjectName = "event" } }, options),
            async () => await client.RenameEventAsync(new() { NewEventName = new() { ObjectName = "event" } }, options)
        ];
        foreach (var call in invalidCalls)
        {
            var error = await Assert.ThrowsAsync<RpcException>(call);
            Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
            Assert.Equal(Api.ExecutionDisposition.NotStarted, Detail(error).ExecutionDisposition);
        }
        Assert.Empty(worker.Commands);
    }

    [Fact]
    public async Task EventGetterFailurePreservesCompletedExecutionWithoutReplay()
    {
        var worker = new EventWorker { FailOutput = true };
        await using var app = CreateApp(worker);
        await app.StartAsync();
        using var channel = GrpcChannel.ForAddress(Assert.Single(app.Urls));
        var client = new Api.EventOperations.EventOperationsClient(channel);
        var error = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetNumberOfEventsInEventRefListAsync(new() { EventList = { new Api.CollectionItemName() } },
                deadline: DateTime.UtcNow.AddSeconds(20)));
        Assert.Equal(StatusCode.DataLoss, error.StatusCode);
        var detail = Detail(error);
        Assert.Equal(Api.ExecutionDisposition.Completed, detail.ExecutionDisposition);
        Assert.Equal(Api.ReplayGuidance.DoNotReplay, detail.ReplayGuidance);
        Assert.Single(worker.Commands);
    }

    private static Api.OperationError Detail(RpcException error) => Api.OperationError.Parser.ParseFrom(
        Assert.Single(error.Trailers, entry => entry.Key == "briosa-operation-error-bin").ValueBytes);

    private static WebApplication CreateApp(EventWorker worker)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.Sources.Clear();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
            listener => listener.Protocols = HttpProtocols.Http2));
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(new OperationExecutor(worker,
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance), TimeProvider.System));
        var app = builder.Build();
        app.MapGrpcService<EventOperationsService>();
        return app;
    }

    private sealed class EventWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];
        public bool FailOutput { get; init; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            command = RoundTrip(WorkerControlMessage.Execute(Guid.NewGuid(), command)).Command!;
            Commands.Add(command);
            var expectedInputs = command.StepName switch
            {
                "Delete Event" => new[] { "Event Name:SetCollectionObjectNameArg2" },
                "Export Event Ref List" => ["Event List:SetCollectionObjectNameRefListArg", "File Path:SetFilePathArg",
                    "Decimal Precision:SetIntegerArg", "Overwrite existing file?:SetBoolArg"],
                "Get i-th Event From Event Ref List" => ["Event List:SetCollectionObjectNameRefListArg", "Event Index:SetIntegerArg"],
                "Get Number of Events in Event Ref List" => ["Event List:SetCollectionObjectNameRefListArg"],
                "Rename Event" => ["Original Event Name:SetCollectionObjectNameArg2", "New Event Name:SetCollectionObjectNameArg2", "Overwrite if exists?:SetBoolArg"],
                _ => throw new InvalidOperationException("Unexpected event operation.")
            };
            Assert.Equal(expectedInputs, command.InputArguments.Select(argument => argument.Name + ":" + argument.SdkBinding));
            WorkerMpOutputValue[] outputs = [];
            if (command.OutputArguments.Count != 0)
            {
                var output = Assert.Single(command.OutputArguments);
                Assert.Null(output.ObjectTypeWhenOmitted); // No event-specific fallback was established.
                if (output.Kind == WorkerMpValueKind.CollectionItemName)
                {
                    Assert.Equal("Resultant Item", output.Name);
                    Assert.Equal("GetCollectionObjectNameArg", output.SdkBinding);
                    var list = command.InputArguments[0].RequireValue<WorkerCollectionItemNameListValue>();
                    var index = command.InputArguments[1].RequireValue<WorkerIntegerValue>().Value;
                    outputs = [new WorkerRetrievedOutput(output.Name, output.Kind, list.Values[index])];
                }
                else
                {
                    Assert.Equal("Total Count", output.Name);
                    Assert.Equal("GetIntegerArg", output.SdkBinding);
                    outputs = [FailOutput ? new WorkerUnavailableOutput(output.Name, output.Kind) :
                        new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerIntegerValue(0))];
                }
            }
            var response = RoundTrip(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), new(
                WorkerExecutionResponseStatus.Completed, new WorkerMpResultAvailable(2, 1, outputs, null),
                new(WorkerConnectionState.Disconnected, WorkerExecutionReadinessState.Unverified,
                    null, 0, 1, "event-test", DateTimeOffset.UnixEpoch), null)));
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, response.ExecutionResponse!.Execution, null, "completed", 1));
        }

        private static WorkerControlMessage RoundTrip(WorkerControlMessage message)
        {
            using var stream = new MemoryStream();
            using var channel = new WorkerControlChannel(stream);
            channel.Send(message);
            stream.Position = 0;
            return channel.Receive();
        }
    }
}
