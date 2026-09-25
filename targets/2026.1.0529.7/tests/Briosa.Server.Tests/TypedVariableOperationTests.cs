using System.Net;
using Briosa.Server.Operations.Variables;
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

public sealed class TypedVariableOperationTests
{
    [Fact]
    public async Task GeneratedClientExercisesTheTypedPublicService()
    {
        var worker = new VariableWorker();
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
            listener => listener.Protocols = HttpProtocols.Http2));
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(Executor(worker));
        await using var app = builder.Build();
        app.MapGrpcService<VariablesService>();
        await app.StartAsync();
        using var channel = GrpcChannel.ForAddress(Assert.Single(app.Urls));
        var client = new Api.Variables.VariablesClient(channel);
        var deadline = DateTime.UtcNow.AddSeconds(10);

        var setScalar = await client.SetDoubleVariableAsync(new() { Name = "scalar", Value = -12.5 }, deadline: deadline);
        var scalar = await client.GetDoubleVariableAsync(new() { Name = "scalar" }, deadline: deadline);
        Assert.Equal(Api.MpExecutionState.Succeeded, setScalar.Execution.State);
        Assert.True(scalar.HasValue);
        Assert.Equal(-12.5, scalar.Value);

        var request = new Api.SetNamedDoubleListVariableRequest { Name = "list" };
        request.DoubleListVariable.AddRange(Enumerable.Range(0, 4096).Select(value => value * 0.25));
        var setList = await client.SetNamedDoubleListVariableAsync(request, deadline: deadline);
        var list = await client.GetNamedDoubleListVariableAsync(new() { Name = "list" }, deadline: deadline);
        Assert.Equal(Api.MpExecutionState.Succeeded, setList.Execution.State);
        Assert.Equal(request.DoubleListVariable, list.DoubleListVariable);
        Assert.Equal(4, worker.Calls);
        await app.StopAsync();
    }

    [Fact]
    public void ScalarDefaultsAndExplicitZeroUseTheReviewedBindings()
    {
        var absent = SetDoubleVariableOperation.CreateCommand(new());
        var explicitZero = SetDoubleVariableOperation.CreateCommand(new() { Name = "", Value = 0 });
        Assert.Equal("Set Double Variable", absent.StepName);
        Assert.Equal(absent.InputArguments, explicitZero.InputArguments);
        Assert.Equal("SetStringArg", absent.InputArguments[0].SdkBinding);
        Assert.Equal("", absent.InputArguments[0].StringValue);
        Assert.Equal("SetDoubleArg", absent.InputArguments[1].SdkBinding);
        Assert.Equal(0, absent.InputArguments[1].DoubleValue);
        Assert.Empty(absent.OutputArguments);
    }

    [Fact]
    public void ListCommandOwnsItsValuesAndPreservesRequiredListBehavior()
    {
        Assert.Throws<ArgumentException>(() => SetNamedDoubleListVariableOperation.CreateCommand(new()));
        var request = new Api.SetNamedDoubleListVariableRequest { Name = "values" };
        request.DoubleListVariable.AddRange([0.0, -1.25, 2.5]);
        var command = SetNamedDoubleListVariableOperation.CreateCommand(request);
        request.DoubleListVariable[0] = 99;
        request.DoubleListVariable.Clear();
        Assert.Equal("Set Named Double List Variable", command.StepName);
        Assert.Equal("SetDoubleArrayArg", command.InputArguments[1].SdkBinding);
        Assert.Equal([0.0, -1.25, 2.5], command.InputArguments[1].DoubleArrayValue!.Values);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4096)]
    public async Task ListResultsPreserveValuesAndExecutionEvidence(int count)
    {
        var values = Enumerable.Range(0, count).Select(value => value * 0.25).ToArray();
        var worker = new ResultWorker([new("Double List Variable", WorkerMpValueKind.DoubleArray,
            Retrieved: true, DoubleArrayValue: new(values))]);
        var result = await Executor(worker).ExecuteAsync(new Api.GetNamedDoubleListVariableRequest { Name = "values" },
            GetNamedDoubleListVariableOperation.Descriptor, GetNamedDoubleListVariableOperation.CreateCommand,
            GetNamedDoubleListVariableOperation.OutputContracts, GetNamedDoubleListVariableOperation.CreateResult,
            CancellationToken.None);
        Assert.Equal(values, result.DoubleListVariable);
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(2, result.Execution.MpResultCode);
        Assert.Equal(Api.OutputRetrievalState.Retrieved, Assert.Single(result.Execution.OutputRetrievals).State);
        Assert.Equal("GetDoubleArrayArg", Assert.Single(worker.Command!.OutputArguments).SdkBinding);
    }

    [Fact]
    public async Task ZeroScalarResultRetainsProtobufPresence()
    {
        var result = await Executor(new ResultWorker([new("Value", WorkerMpValueKind.FloatingPoint,
            Retrieved: true, DoubleValue: 0)])).ExecuteAsync(new Api.GetDoubleVariableRequest(),
            GetDoubleVariableOperation.Descriptor, GetDoubleVariableOperation.CreateCommand,
            GetDoubleVariableOperation.OutputContracts, GetDoubleVariableOperation.CreateResult,
            CancellationToken.None);
        Assert.True(result.HasValue);
        Assert.Equal(0, result.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingListPayloadIsACompletedOutputFailure(bool retrieved)
    {
        var error = await Assert.ThrowsAsync<RpcException>(() =>
            Executor(new ResultWorker([new("Double List Variable", WorkerMpValueKind.DoubleArray, retrieved)]))
                .ExecuteAsync(new Api.GetNamedDoubleListVariableRequest(),
                    GetNamedDoubleListVariableOperation.Descriptor, GetNamedDoubleListVariableOperation.CreateCommand,
                    GetNamedDoubleListVariableOperation.OutputContracts, GetNamedDoubleListVariableOperation.CreateResult,
                    CancellationToken.None));
        Assert.Equal(StatusCode.DataLoss, error.StatusCode);
        var details = Api.OperationError.Parser.ParseFrom(Assert.Single(error.Trailers).ValueBytes);
        Assert.Equal(Api.ExecutionDisposition.Completed, details.ExecutionDisposition);
        Assert.Equal(Api.OperationFailureKind.OutputRetrievalFailure, details.Kind);
    }

    [Fact]
    public void ReorderedOutputsAreRejectedBeforePositionalMapping()
    {
        var outcome = ResultWorker.Completed([
            new("Maximum", WorkerMpValueKind.FloatingPoint, true, DoubleValue: 10),
            new("Minimum", WorkerMpValueKind.FloatingPoint, true, DoubleValue: 1)]);
        var error = Assert.Throws<RpcException>(() => GrpcOperationOutcomeMapper.RequireSuccess(outcome,
            "ordered-output", Api.ReplaySafety.Safe,
            [new("minimum", "Minimum", WorkerMpValueKind.FloatingPoint), new("maximum", "Maximum", WorkerMpValueKind.FloatingPoint)],
            callerDeadlineExceeded: false));
        Assert.Equal("worker-output-shape-invalid", GrpcOperationOutcomeMapper.GetDiagnosticCode(error));
    }

    private static OperationExecutor Executor(IWorkerCommandExecutor worker) => new(worker,
        new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance), TimeProvider.System);

    private sealed class ResultWorker(IReadOnlyList<WorkerMpOutputValue> outputs) : IWorkerCommandExecutor
    {
        public WorkerMpCommand? Command { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Command = command;
            return Task.FromResult(Completed(outputs));
        }

        public static WorkerExecutionOutcome Completed(IReadOnlyList<WorkerMpOutputValue> values) => new(
            WorkerExecutionStatus.Completed, WorkerExecutionDisposition.Completed,
            new(true, true, true, 2, 1, values, null), null, "completed", 1);
    }

    private sealed class VariableWorker : IWorkerCommandExecutor
    {
        private double _scalar;
        private IReadOnlyList<double> _list = [];
        public int Calls { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Calls++;
            IReadOnlyList<WorkerMpOutputValue> outputs;
            switch (command.OperationId)
            {
                case "variables.set_double_variable":
                    _scalar = command.InputArguments[1].DoubleValue!.Value;
                    outputs = [];
                    break;
                case "variables.get_double_variable":
                    outputs = [new("Value", WorkerMpValueKind.FloatingPoint, true, DoubleValue: _scalar)];
                    break;
                case "variables.set_named_double_list_variable":
                    _list = command.InputArguments[1].DoubleArrayValue!.Values;
                    outputs = [];
                    break;
                case "variables.get_named_double_list_variable":
                    outputs = [new("Double List Variable", WorkerMpValueKind.DoubleArray, true, DoubleArrayValue: new(_list))];
                    break;
                default:
                    throw new InvalidOperationException("Unexpected variable operation.");
            }
            return Task.FromResult(ResultWorker.Completed(outputs));
        }
    }
}
