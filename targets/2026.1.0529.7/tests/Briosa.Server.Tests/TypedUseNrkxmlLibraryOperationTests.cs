using Briosa.Server.Operations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedUseNrkxmlLibraryOperationTests
{
    [Fact]
    public async Task NrkxmlLibraryRouteIsRegisteredAndMapsTheDefault()
    {
        Assert.Single(SpatialAnalyzerApi.Operations,
            operation => operation.OperationId == "file_operations.use_nrkxml_library");
        Assert.True(UseNrkxmlLibraryOperation.CreateCommand(new()).InputArguments[0]
            .RequireValue<WorkerBooleanValue>().Value);
        Assert.False(UseNrkxmlLibraryOperation.CreateCommand(new() { UseLibrary = false })
            .InputArguments[0].RequireValue<WorkerBooleanValue>().Value);

        var worker = new NrkxmlWorker();
        var grpcHost = await GrpcTestHost.StartAsync<FileOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.FileOperations.FileOperationsClient(channel);
        await client.UseNrkxmlLibraryAsync(new(),
            new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));

        var command = Assert.Single(worker.Commands);
        Assert.Equal("file_operations.use_nrkxml_library", command.OperationId);
        Assert.True(command.InputArguments[0].RequireValue<WorkerBooleanValue>().Value);
    }

    private sealed class NrkxmlWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var result = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, result, null, "completed", 1));
        }
    }
}
