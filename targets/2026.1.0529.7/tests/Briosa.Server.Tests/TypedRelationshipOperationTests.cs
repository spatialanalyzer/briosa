using System.Net;
using Briosa.Server.Operations.RelationshipOperations;
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

public sealed class TypedRelationshipOperationTests
{
    [Fact]
    public void NestedDefaultsAreIndependentAndUseExactBindings()
    {
        var request = new Api.SetRelationshipFitConstraintsScalarTypeRequest
        {
            RelationshipName = new() { ObjectName = "relationship" }
        };
        var omitted = SetRelationshipFitConstraintsScalarTypeOperation.CreateCommand(request);
        Assert.Equal("Set Relationship Fit Constraints (Scalar Type)", omitted.StepName);
        Assert.Equal("SetCollectionObjectNameArg2", omitted.InputArguments[0].SdkBinding);
        Assert.Equal(new WorkerCollectionObjectNameValue("", "relationship", WorkerObjectTypeValue.Any),
            (omitted.InputArguments[0].Value as WorkerCollectionObjectNameValue));
        Assert.Equal("SetFitConstraintScalarOptionsArg", omitted.InputArguments[1].SdkBinding);
        Assert.Equal(new WorkerFitConstraintScalarOptionsValue(new(false, 0), new(false, 0)),
            (omitted.InputArguments[1].Value as WorkerFitConstraintScalarOptionsValue));

        request.FitConstraintOptions = new() { High = new(), Low = new() };
        Assert.Equal(omitted.InputArguments,
            SetRelationshipFitConstraintsScalarTypeOperation.CreateCommand(request).InputArguments);
        request.FitConstraintOptions.High.Enabled = true;
        request.FitConstraintOptions.Low.Value = -0.5;
        var partial = SetRelationshipFitConstraintsScalarTypeOperation.CreateCommand(request);
        Assert.Equal(new WorkerFitConstraintScalarOptionsValue(new(true, 0), new(false, -0.5)),
            (partial.InputArguments[1].Value as WorkerFitConstraintScalarOptionsValue));
        request.FitConstraintOptions.High.Value = 99;
        Assert.Equal(0, (partial.InputArguments[1].Value as WorkerFitConstraintScalarOptionsValue)!.High.Value);
    }

    [Fact]
    public async Task GeneratedClientExercisesNestedOptionsAndOutputPresence()
    {
        var worker = new RelationshipWorker();
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
            listener => listener.Protocols = HttpProtocols.Http2));
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(new OperationExecutor(worker,
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance), TimeProvider.System));
        await using var app = builder.Build();
        app.MapGrpcService<RelationshipOperationsService>();
        await app.StartAsync();
        using var channel = GrpcChannel.ForAddress(Assert.Single(app.Urls));
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        var relationship = new Api.CollectionObjectName { ObjectName = "relationship" };
        var set = await client.SetRelationshipFitConstraintsScalarTypeAsync(new()
        {
            RelationshipName = relationship,
            FitConstraintOptions = new()
            {
                High = new() { Enabled = true, Value = 0 },
                Low = new() { Enabled = false, Value = -0.5 }
            }
        }, deadline: deadline);
        var get = await client.GetRelationshipFitConstraintsScalarTypeAsync(new()
        {
            RelationshipName = relationship
        }, deadline: deadline);

        Assert.Equal(Api.MpExecutionState.Succeeded, set.Execution.State);
        Assert.Equal(2, get.Execution.MpResultCode);
        Assert.True(get.HasUseHighTolerance);
        Assert.False(get.UseHighTolerance);
        Assert.True(get.HasHighTolerance);
        Assert.Equal(0, get.HighTolerance);
        Assert.True(get.UseLowTolerance);
        Assert.Equal(-3.5, get.LowTolerance);
        Assert.True(get.FitConstraintOptions.High.HasEnabled);
        Assert.True(get.FitConstraintOptions.High.Enabled);
        Assert.True(get.FitConstraintOptions.High.HasValue);
        Assert.Equal(0, get.FitConstraintOptions.High.Value);
        Assert.False(get.FitConstraintOptions.Low.Enabled);
        Assert.Equal(-0.5, get.FitConstraintOptions.Low.Value);
        Assert.Equal(5, get.Execution.OutputRetrievals.Count);

        var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.SetRelationshipFitConstraintsScalarTypeAsync(new(), deadline: deadline));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
        var details = Api.OperationError.Parser.ParseFrom(Assert.Single(invalid.Trailers, entry => entry.Key == "briosa-operation-error-bin").ValueBytes);
        Assert.Equal(Api.ExecutionDisposition.NotStarted, details.ExecutionDisposition);
        Assert.Equal(2, worker.Calls);
        await app.StopAsync();
    }

    [Theory]
    [InlineData(9999)]
    [InlineData(-1)]
    public void UnknownObjectTypesAreRejectedBeforeWorkerSubmission(int type)
    {
        var request = new Api.GetRelationshipFitConstraintsScalarTypeRequest
        {
            RelationshipName = new() { ObjectName = "relationship", ObjectType = (Api.ObjectType)type }
        };
        Assert.Throws<ArgumentException>(() => GetRelationshipFitConstraintsScalarTypeOperation.CreateCommand(request));
    }

    private sealed class RelationshipWorker : IWorkerCommandExecutor
    {
        private WorkerFitConstraintScalarOptionsValue _options = new(new(false, 0), new(false, 0));
        public int Calls { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Calls++;
            command = RoundTrip(WorkerControlMessage.Execute(Guid.NewGuid(), command)).Command!;
            IReadOnlyList<WorkerMpOutputValue> outputs;
            if (command.OperationId == SetRelationshipFitConstraintsScalarTypeOperation.Descriptor.OperationId)
            {
                _options = (command.InputArguments[1].Value as WorkerFitConstraintScalarOptionsValue)!;
                outputs = [];
            }
            else
            {
                Assert.Equal(GetRelationshipFitConstraintsScalarTypeOperation.Descriptor.OperationId, command.OperationId);
                Assert.Equal(["GetBoolArg", "GetDoubleArg", "GetBoolArg", "GetDoubleArg", "GetFitConstraintScalarOptionsArg"],
                    command.OutputArguments.Select(output => output.SdkBinding));
                outputs =
                [
                    new WorkerRetrievedOutput("Use High Tolerance?", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
                    new WorkerRetrievedOutput("High Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0)),
                    new WorkerRetrievedOutput("Use Low Tolerance?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("Low Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(-3.5)),
                    new WorkerRetrievedOutput("Fit Constraint Options", WorkerMpValueKind.FitConstraintScalarOptions, _options)
                ];
            }
            var response = RoundTrip(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), new(
                WorkerExecutionResponseStatus.Completed, new WorkerMpResultAvailable(2, 1, outputs, null),
                new(WorkerConnectionState.Disconnected, WorkerExecutionReadinessState.Unverified,
                    null, 0, 1, "test", DateTimeOffset.UnixEpoch), null)));
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
