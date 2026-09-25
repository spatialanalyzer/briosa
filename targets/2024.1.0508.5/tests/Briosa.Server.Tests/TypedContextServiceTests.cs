using System.Net;
using Briosa.Server.Operations;
using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Operations.UtilityOperations;
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

public sealed class TypedContextServiceTests
{
    [Fact]
    public async Task GeneratedClientsUseTheTypedReadOnlyContextMappings()
    {
        var worker = new ContextWorker();
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
            listener => listener.Protocols = HttpProtocols.Http2));
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(new OperationExecutor(worker,
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance), TimeProvider.System));
        await using var app = builder.Build();
        app.MapGrpcService<AnalysisOperationsService>();
        app.MapGrpcService<ConstructionOperationsService>();
        app.MapGrpcService<FileOperationsService>();
        app.MapGrpcService<UtilityOperationsService>();
        await app.StartAsync();
        using var channel = GrpcChannel.ForAddress(Assert.Single(app.Urls));
        var analysis = new Api.AnalysisOperations.AnalysisOperationsClient(channel);
        var construction = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var file = new Api.FileOperations.FileOperationsClient(channel);
        var utility = new Api.UtilityOperations.UtilityOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var directory = await file.GetWorkingDirectoryAsync(new(), options);
        Assert.True(directory.HasDirectory);
        Assert.Equal("", directory.Directory);
        Assert.Equal(Api.MpExecutionState.Succeeded, directory.Execution.State);
        var count = await analysis.GetNumberOfCollectionsAsync(new(), options);
        Assert.True(count.HasTotalCount);
        Assert.Equal(3, count.TotalCount);
        Assert.Equal("Collection 0", (await analysis.GetIthCollectionNameAsync(new(), options)).ResultantName);
        Assert.Equal("Collection 2", (await analysis.GetIthCollectionNameAsync(new() { CollectionIndex = 2 }, options)).ResultantName);
        Assert.Equal("Collection 0", (await construction.GetActiveCollectionNameAsync(new(), options)).CurrentlyActiveCollectionName);
        var units = await utility.GetActiveUnitsAsync(new(), options);
        Assert.Equal("mm", units.Length);
        Assert.Equal("deg", units.Angular);
        Assert.Equal("C", units.Temperature);
        Assert.Equal(["length", "angular", "temperature"], units.Execution.OutputRetrievals.Select(item => item.FieldName));
        var frame = await utility.GetWorkingFramePropertiesAsync(new(), options);
        Assert.Equal("Frame 1", frame.FrameName);
        Assert.Equal("Collection 0", frame.CollectionName);
        Assert.Equal(Api.ObjectType.Frame, frame.WorkingFrame.ObjectType);
        Assert.Equal(frame.FrameName, frame.WorkingFrame.ObjectName);
        Assert.Equal(frame.CollectionName, frame.WorkingFrame.CollectionName);

        foreach (var operation in worker.SeenOperations)
        {
            Assert.DoesNotContain(MpOperationCatalog.Operations, item => item.Descriptor.OperationId == operation);
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation);
        }
        Assert.Equal(6, worker.SeenOperations.Count);
    }

    private sealed class ContextWorker : IWorkerCommandExecutor
    {
        public HashSet<string> SeenOperations { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            command = RoundTrip(WorkerControlMessage.Execute(Guid.NewGuid(), command)).Command!;
            SeenOperations.Add(command.OperationId);
            if (command.StepName == "Get i-th Collection Name")
            {
                var input = Assert.Single(command.InputArguments);
                Assert.Equal("Collection Index", input.Name);
                Assert.Equal("SetIntegerArg", input.SdkBinding);
            }
            else Assert.Empty(command.InputArguments);

            WorkerMpOutputValue[] outputs = command.StepName switch
            {
                "Get Working Directory" => [Text("Directory", "")],
                "Get Number of Collections" => [new WorkerRetrievedOutput("Total Count", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(3))],
                "Get i-th Collection Name" => [new WorkerRetrievedOutput("Resultant Name", WorkerMpValueKind.CollectionName,
                    new WorkerTextValue($"Collection {Assert.IsType<WorkerIntegerValue>(command.InputArguments[0].Value).Value}"))],
                "Get Active Collection Name" => [Text("Currently Active Collection Name", "Collection 0")],
                "Get Active Units" => [Text("Length", "mm"), Text("Angular", "deg"), Text("Temperature", "C")],
                "Get Working Frame Properties" => [Text("Frame Name", "Frame 1"), Text("Collection Name", "Collection 0"),
                    new WorkerRetrievedOutput("Working Frame", WorkerMpValueKind.CollectionObjectName,
                        new WorkerCollectionObjectNameValue("Collection 0", "Frame 1", WorkerObjectTypeValue.Frame))],
                _ => throw new InvalidOperationException("Unexpected context operation.")
            };
            Assert.Equal(outputs.Select(output => output.Name), command.OutputArguments.Select(output => output.Name));
            Assert.Equal(outputs.Select(output => output.Kind), command.OutputArguments.Select(output => output.Kind));
            foreach (var output in command.OutputArguments)
            {
                var binding = output.Kind switch
                {
                    WorkerMpValueKind.WholeNumber => "GetIntegerArg",
                    WorkerMpValueKind.CollectionName => "GetCollectionNameArg",
                    WorkerMpValueKind.CollectionObjectName => "GetCollectionObjectNameArg",
                    _ => "GetStringArg"
                };
                Assert.Equal(binding, output.SdkBinding);
            }
            if (command.StepName == "Get Working Frame Properties")
                Assert.Equal(WorkerObjectTypeValue.Frame, command.OutputArguments[2].ObjectTypeWhenOmitted);

            var response = RoundTrip(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), new(
                WorkerExecutionResponseStatus.Completed, new WorkerMpResultAvailable(2, 1, outputs, null),
                new(WorkerConnectionState.Disconnected, WorkerExecutionReadinessState.Unverified,
                    null, 0, 1, "test", DateTimeOffset.UnixEpoch), null)));
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, response.ExecutionResponse!.Execution, null, "completed", 1));
        }

        private static WorkerRetrievedOutput Text(string name, string value) => new(name, WorkerMpValueKind.Text, new WorkerTextValue(value));

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
