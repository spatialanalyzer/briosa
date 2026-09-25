using System.IO.Pipes;
using Briosa.Worker.Control;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

public sealed class ProductionWorkerControlTests
{
    [Theory]
    [InlineData((int)SdkLivenessStatus.Alive, WorkerConnectionFailure.None)]
    [InlineData((int)SdkLivenessStatus.ProcessExited, WorkerConnectionFailure.ProcessExited)]
    [InlineData((int)SdkLivenessStatus.Unavailable, WorkerConnectionFailure.LivenessUnavailable)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The connection manager owns and disposes the injected SDK on its STA.")]
    public async Task LivenessFailureCategorySurvivesThePrivateChannel(int liveness, WorkerConnectionFailure expected)
    {
        var sdk = new ControlledSdk { Liveness = (SdkLivenessStatus)liveness };
        var manager = new SdkConnectionManager("localhost",
            new SdkConnectionPolicy(1, TimeSpan.Zero), () => sdk);
        await using var lifetime = manager.ConfigureAwait(false);
        await manager.StartAsync();
        var observed = await manager.ProbeLivenessAsync();
        var connection = WorkerControlHost.ToControlSnapshot(observed) with { DiagnosticCode = "arbitrary-description" };
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        await channel.SendAsync(WorkerControlMessage.Pong(Guid.NewGuid(), connection));
        stream.Position = 0;
        var decoded = (await channel.ReceiveAsync()).Connection!;
        Assert.Equal(expected, observed.Failure);
        Assert.Equal(expected, decoded.Failure);
        Assert.Equal("arbitrary-description", decoded.DiagnosticCode);
    }

    [Theory]
    [InlineData(WorkerConnectionState.Faulted, (WorkerConnectionFailure)999)]
    [InlineData(WorkerConnectionState.Connected, WorkerConnectionFailure.ProcessExited)]
    public void InvalidConnectionFailureEvidenceIsRejectedBeforeWrite(WorkerConnectionState state, WorkerConnectionFailure failure)
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream);
        var connection = new WorkerConnectionSnapshot(state, WorkerExecutionReadinessState.Unverified,
            null, 0, 1, "test", DateTimeOffset.UnixEpoch, Failure: failure);
        Assert.Throws<WorkerMessageRejectedException>(() => channel.Send(WorkerControlMessage.Pong(Guid.NewGuid(), connection)));
        Assert.Equal(0, stream.Length);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The serialized executor owns and disposes the injected SDK on its STA.")]
    public async Task SerializedExecutorPassesTheOwnedCommandDirectlyToTheSdk()
    {
        var sdk = new ControlledSdk();
        var value = new WorkerDoubleArrayValue([0, 1, 2]);
        var command = new WorkerMpCommand("normal", "Normal",
            [new("Values", WorkerMpValueKind.DoubleArray, value)], []);
        var executor = new SerializedSdkExecutor(() => sdk);
        await using (executor.ConfigureAwait(false))
        {
            await executor.ExecuteAsync(command);
            Assert.Same(command, sdk.LastCommand);
            Assert.Same(value, sdk.LastCommand!.InputArguments[0].Value);
        }
        Assert.True(sdk.Disposed);
        Assert.True(sdk.AllCallsOnSta);
    }

    [Theory]
    [InlineData("oversized", WorkerMpValueKind.Text)]
    [InlineData("non-finite", WorkerMpValueKind.FloatingPoint)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The production connection manager owns and disposes this injected SDK on its STA; the test must not dispose it on a different thread.")]
    public async Task UndeliverableOutputsPreserveCompletionAndLeaveTheProductionPipeUsable(
        string operation, WorkerMpValueKind kind)
    {
        var name = $"briosa-control-test-{Guid.NewGuid():N}";
        using var pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var sdk = new ControlledSdk();
        var host = WorkerControlHost.RunAsync(name, "localhost", () => { sdk.Observe(); return sdk; });
        try
        {
            await pipe.WaitForConnectionAsync(timeout.Token);
            using var channel = new WorkerControlChannel(pipe, leaveOpen: true);
            Assert.Equal(WorkerControlMessageKind.Ready, (await channel.ReceiveAsync(timeout.Token)).Kind);
            var connected = await Exchange(channel, WorkerControlMessage.Connect(Guid.NewGuid()), timeout.Token);
            Assert.Equal(WorkerConnectionState.Connected, connected.Connection!.State);
            var verified = await Exchange(channel, WorkerControlMessage.VerifyExecution(Guid.NewGuid()), timeout.Token);
            Assert.Equal(WorkerExecutionReadinessState.ExecutionReady, verified.Connection!.ExecutionReadinessState);

            var response = await Exchange(channel, WorkerControlMessage.Execute(Guid.NewGuid(),
                new(operation, operation, [], [new("Value", kind)])), timeout.Token);
            var result = Assert.IsType<WorkerMpOutputsUnavailable>(response.ExecutionResponse!.Execution);
            Assert.True(result.ExecuteStepReturned);
            Assert.True(result.MpResultRetrieved);
            Assert.True(result.MpSucceeded);
            Assert.Equal(2, result.MpResultCode);
            Assert.Equal(7, result.DurationMilliseconds);
            Assert.Empty(result.OutputValues);

            Assert.Equal(WorkerControlMessageKind.Pong,
                (await Exchange(channel, WorkerControlMessage.Ping(Guid.NewGuid()), timeout.Token)).Kind);
            var next = await Exchange(channel, WorkerControlMessage.Execute(Guid.NewGuid(),
                new("normal", "Normal", [], [new("Value", WorkerMpValueKind.Text)])), timeout.Token);
            Assert.Equal("normal", ((Assert.Single(next.ExecutionResponse!.Execution!.OutputValues).ReadValue() as WorkerTextValue)?.Value));
            Assert.Equal(WorkerControlMessageKind.Stopped,
                (await Exchange(channel, WorkerControlMessage.Stop(Guid.NewGuid()), timeout.Token)).Kind);
            Assert.Equal(0, await host.WaitAsync(timeout.Token));
            Assert.Single(sdk.Threads);
            Assert.True(sdk.AllCallsOnSta);
            Assert.True(sdk.Disposed);
            Assert.Equal(2, sdk.OperationCalls);
        }
        finally
        {
            await pipe.DisposeAsync();
            await host.WaitAsync(TimeSpan.FromSeconds(3));
        }
    }

    private static async Task<WorkerControlMessage> Exchange(WorkerControlChannel channel,
        WorkerControlMessage request, CancellationToken token)
    {
        await channel.SendAsync(request, token).ConfigureAwait(false);
        var response = await channel.ReceiveAsync(token).ConfigureAwait(false);
        Assert.Equal(request.CorrelationId, response.CorrelationId);
        return response;
    }

    private sealed class ControlledSdk : ISpatialAnalyzerSdk
    {
        public HashSet<int> Threads { get; } = [];
        public bool AllCallsOnSta { get; private set; } = true;
        public bool Disposed { get; private set; }
        public int OperationCalls { get; private set; }
        public WorkerMpCommand? LastCommand { get; private set; }
        public void Observe()
        {
            Threads.Add(Environment.CurrentManagedThreadId);
            AllCallsOnSta &= Thread.CurrentThread.GetApartmentState() == ApartmentState.STA;
        }
        public SdkLivenessStatus Liveness { get; init; } = SdkLivenessStatus.Alive;
        public SdkLivenessStatus GetLiveness() { Observe(); return Liveness; }
        public string? GetActivatedSdkVersion() { Observe(); return null; }
        public SdkConnectionResult Connect(string host) { Observe(); return new(SdkConnectionStatus.Connected, 0, null); }
        public WorkerMpExecutionResult Execute(WorkerMpCommand command)
        {
            Observe();
            if (command.OperationId == SdkConnectionManager.VerificationOperationId)
                return new WorkerMpResultAvailable(2, 1,
                    [new WorkerRetrievedOutput(SdkConnectionManager.VerificationOutputName, WorkerMpValueKind.Text, new WorkerTextValue("fake"))], null);
            OperationCalls++;
            LastCommand = command;
            WorkerMpOutputValue value = command.OperationId switch
            {
                "oversized" => new WorkerRetrievedOutput("Value", WorkerMpValueKind.Text, new WorkerTextValue(new string('x', 100_000))),
                "non-finite" => new WorkerRetrievedOutput("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(double.NaN)),
                _ => new WorkerRetrievedOutput("Value", WorkerMpValueKind.Text, new WorkerTextValue("normal"))
            };
            return new WorkerMpResultAvailable(2, 7, [value], null);
        }
        public void Dispose() { Observe(); Disposed = true; }
    }
}
