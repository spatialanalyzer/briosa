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

    [Theory]
    [InlineData((int)SpatialAnalyzerSdkAdapterTests.SdkFaultPhase.SetStep, WorkerSdkFaultDiagnosticCodes.BeforeExecute)]
    [InlineData((int)SpatialAnalyzerSdkAdapterTests.SdkFaultPhase.Setter, WorkerSdkFaultDiagnosticCodes.BeforeExecute)]
    [InlineData((int)SpatialAnalyzerSdkAdapterTests.SdkFaultPhase.ExecuteStep, WorkerSdkFaultDiagnosticCodes.ExecuteStep)]
    [InlineData((int)SpatialAnalyzerSdkAdapterTests.SdkFaultPhase.GetMPStepResult, WorkerSdkFaultDiagnosticCodes.MpResultRetrieval)]
    [InlineData((int)SpatialAnalyzerSdkAdapterTests.SdkFaultPhase.Getter, WorkerSdkFaultDiagnosticCodes.OutputGetter)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The production connection manager owns and disposes the adapter, the adapter owns the fake SDK calls, and the diagnostics writer holds no resources.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2025:Ensure tasks using IDisposable instances complete before the instances are disposed", Justification = "The diagnostics writer is never disposed and the host task is awaited before the test ends.")]
    public async Task PerCallSdkFaultKeepsTheProductionPipeAndWorkerUsable(
        int faultPhase, string diagnosticCode)
    {
        var name = $"briosa-control-test-{Guid.NewGuid():N}";
        using var pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var phase = (SpatialAnalyzerSdkAdapterTests.SdkFaultPhase)faultPhase;
        var calls = new SpatialAnalyzerSdkAdapterTests.RecordingSdkCalls
        {
            FaultPhase = phase,
            FaultStepName = "Faulting Step"
        };
        var diagnostics = new StringWriter();
        var host = WorkerControlHost.RunAsync(name, "localhost",
            () => new SpatialAnalyzerSdkAdapter(calls), diagnostics);
        try
        {
            await pipe.WaitForConnectionAsync(timeout.Token);
            using var channel = new WorkerControlChannel(pipe, leaveOpen: true);
            await ConnectAndVerify(channel, timeout.Token);

            var response = await Exchange(channel, WorkerControlMessage.Execute(Guid.NewGuid(),
                new("faulting", "Faulting Step",
                    [new("Value", WorkerMpValueKind.Text, new WorkerTextValue("input"))],
                    [new("Value", WorkerMpValueKind.Text)])), timeout.Token);
            Assert.Equal(WorkerExecutionResponseStatus.Completed, response.ExecutionResponse!.Status);
            var execution = response.ExecutionResponse.Execution!;
            Assert.Equal(diagnosticCode, execution.DiagnosticCode);
            Assert.Equal(WorkerExecutionReadinessState.ExecutionReady,
                response.ExecutionResponse.Connection.ExecutionReadinessState);
            var fault = Assert.IsType<WorkerSdkCallFaulted>(execution);
            Assert.Equal(phase switch
            {
                SpatialAnalyzerSdkAdapterTests.SdkFaultPhase.SetStep or
                    SpatialAnalyzerSdkAdapterTests.SdkFaultPhase.Setter => WorkerSdkCallPhase.BeforeExecute,
                SpatialAnalyzerSdkAdapterTests.SdkFaultPhase.ExecuteStep => WorkerSdkCallPhase.ExecuteStep,
                SpatialAnalyzerSdkAdapterTests.SdkFaultPhase.GetMPStepResult => WorkerSdkCallPhase.MpResultRetrieval,
                _ => WorkerSdkCallPhase.OutputGetter
            }, fault.Phase);
            if (phase == SpatialAnalyzerSdkAdapterTests.SdkFaultPhase.Getter)
            {
                Assert.True(execution.MpSucceeded);
                Assert.False(Assert.Single(execution.OutputValues).Retrieved);
            }
            else
            {
                Assert.Empty(execution.OutputValues);
            }

            Assert.Equal(WorkerControlMessageKind.Pong,
                (await Exchange(channel, WorkerControlMessage.Ping(Guid.NewGuid()), timeout.Token)).Kind);
            var next = await Exchange(channel, WorkerControlMessage.Execute(Guid.NewGuid(),
                new("normal", "Normal", [], [new("Value", WorkerMpValueKind.Text)])), timeout.Token);
            Assert.Equal("scripted-output",
                (Assert.Single(next.ExecutionResponse!.Execution!.OutputValues).ReadValue() as WorkerTextValue)?.Value);
            Assert.Equal(WorkerControlMessageKind.Stopped,
                (await Exchange(channel, WorkerControlMessage.Stop(Guid.NewGuid()), timeout.Token)).Kind);
            Assert.Equal(0, await host.WaitAsync(timeout.Token));
            Assert.Empty(diagnostics.ToString());
        }
        finally
        {
            await pipe.DisposeAsync();
            await host.WaitAsync(TimeSpan.FromSeconds(3));
        }
    }

    // A getter fault together with a value the private channel cannot encode
    // (oversized text or NaN) must still reach the host as the SDK call fault,
    // dropping only that value and keeping every other output's status.
    [Theory]
    [InlineData("Result", 3)]
    [InlineData("Planar Offset", 2)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The production connection manager owns and disposes the adapter, the adapter owns the fake SDK calls, and the diagnostics writer holds no resources.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2025:Ensure tasks using IDisposable instances complete before the instances are disposed", Justification = "The diagnostics writer is never disposed and the host task is awaited before the test ends.")]
    public async Task GetterFaultWithAnUndeliverableValueStaysAnSdkCallFault(
        string undeliverableOutput, int faultedGetter)
    {
        var calls = new SpatialAnalyzerSdkAdapterTests.RecordingSdkCalls
        {
            FaultPhase = SpatialAnalyzerSdkAdapterTests.SdkFaultPhase.Getter,
            FaultOrdinal = faultedGetter,
            FaultStepName = UndeliverableStep,
            UndeliverableOutputName = undeliverableOutput
        };

        var execution = await ExecuteUndeliverableCommand(calls);

        var fault = Assert.IsType<WorkerSdkCallFaulted>(execution);
        Assert.Equal(WorkerSdkCallPhase.OutputGetter, fault.Phase);
        Assert.Equal(WorkerSdkFaultDiagnosticCodes.OutputGetter, fault.DiagnosticCode);
        Assert.True(fault.MpSucceeded);
        Assert.Equal(2, fault.MpResultCode);
        Assert.Equal(UndeliverableOutputs.Select(output => output.Name), fault.Outputs.Select(output => output.Name));
        for (var index = 0; index < fault.Outputs.Count; index++)
        {
            var output = fault.Outputs[index];
            if (output.Name == undeliverableOutput)
            {
                Assert.Equal(WorkerUnavailableOutputReason.EncodingRejected,
                    Assert.IsType<WorkerUnavailableOutput>(output).Reason);
            }
            else if (index == faultedGetter - 1)
            {
                Assert.Equal(WorkerUnavailableOutputReason.SdkCallFaulted,
                    Assert.IsType<WorkerUnavailableOutput>(output).Reason);
            }
            else
            {
                Assert.IsType<WorkerRetrievedOutput>(output);
            }
        }
    }

    // Without an SDK call fault, an undeliverable value keeps today's bounded
    // outputs-unavailable result.
    [Theory]
    [InlineData("Result")]
    [InlineData("Planar Offset")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The production connection manager owns and disposes the adapter, the adapter owns the fake SDK calls, and the diagnostics writer holds no resources.")]
    public async Task UndeliverableValueWithoutAFaultStaysOutputsUnavailable(string undeliverableOutput)
    {
        var calls = new SpatialAnalyzerSdkAdapterTests.RecordingSdkCalls
        {
            UndeliverableOutputName = undeliverableOutput
        };

        var execution = await ExecuteUndeliverableCommand(calls);

        var unavailable = Assert.IsType<WorkerMpOutputsUnavailable>(execution);
        Assert.Equal(WorkerExecutionDelivery.OutputEncodingRejected, unavailable.DiagnosticCode);
        Assert.True(unavailable.MpSucceeded);
        Assert.Empty(unavailable.OutputValues);
    }

    private const string UndeliverableStep = "Undeliverable Step";

    private static readonly WorkerMpOutputArgument[] UndeliverableOutputs =
    [
        new("Planar Offset", WorkerMpValueKind.FloatingPoint),
        new("Result", WorkerMpValueKind.Text),
        new("Count", WorkerMpValueKind.WholeNumber)
    ];

    // Runs one command through the production control host, adapter and named
    // pipe, then proves the pipe and worker stay usable.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The production connection manager owns and disposes the adapter, the adapter owns the fake SDK calls, and the diagnostics writer holds no resources.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2025:Ensure tasks using IDisposable instances complete before the instances are disposed", Justification = "The diagnostics writer is never disposed and the host task is awaited before the helper returns.")]
    private static async Task<WorkerMpExecutionResult> ExecuteUndeliverableCommand(
        SpatialAnalyzerSdkAdapterTests.RecordingSdkCalls calls)
    {
        var name = $"briosa-control-test-{Guid.NewGuid():N}";
        using var pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var diagnostics = new StringWriter();
        var host = WorkerControlHost.RunAsync(name, "localhost",
            () => new SpatialAnalyzerSdkAdapter(calls), diagnostics);
        try
        {
            await pipe.WaitForConnectionAsync(timeout.Token).ConfigureAwait(false);
            using var channel = new WorkerControlChannel(pipe, leaveOpen: true);
            await ConnectAndVerify(channel, timeout.Token).ConfigureAwait(false);

            var response = await Exchange(channel, WorkerControlMessage.Execute(Guid.NewGuid(),
                new("undeliverable", UndeliverableStep, [], UndeliverableOutputs)), timeout.Token).ConfigureAwait(false);
            Assert.Equal(WorkerExecutionResponseStatus.Completed, response.ExecutionResponse!.Status);
            var execution = response.ExecutionResponse.Execution!;

            Assert.Equal(WorkerControlMessageKind.Pong,
                (await Exchange(channel, WorkerControlMessage.Ping(Guid.NewGuid()), timeout.Token).ConfigureAwait(false)).Kind);
            var next = await Exchange(channel, WorkerControlMessage.Execute(Guid.NewGuid(),
                new("normal", "Normal", [], [new("Value", WorkerMpValueKind.Text)])), timeout.Token).ConfigureAwait(false);
            Assert.Equal("scripted-output",
                (Assert.Single(next.ExecutionResponse!.Execution!.OutputValues).ReadValue() as WorkerTextValue)?.Value);
            Assert.Equal(WorkerControlMessageKind.Stopped,
                (await Exchange(channel, WorkerControlMessage.Stop(Guid.NewGuid()), timeout.Token).ConfigureAwait(false)).Kind);
            Assert.Equal(0, await host.WaitAsync(timeout.Token).ConfigureAwait(false));
            Assert.Empty(diagnostics.ToString());
            return execution;
        }
        finally
        {
            await pipe.DisposeAsync().ConfigureAwait(false);
            await host.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        }
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The production connection manager owns and disposes the adapter, the adapter owns the fake SDK calls, and the diagnostics writer holds no resources.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2025:Ensure tasks using IDisposable instances complete before the instances are disposed", Justification = "The diagnostics writer is never disposed and the host task is awaited before the test ends.")]
    public async Task UntrustworthyStateFaultFailStopsWithoutRawExceptionText()
    {
        var name = $"briosa-control-test-{Guid.NewGuid():N}";
        using var pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var calls = new SpatialAnalyzerSdkAdapterTests.RecordingSdkCalls
        {
            FaultPhase = SpatialAnalyzerSdkAdapterTests.SdkFaultPhase.ExecuteStep,
            FaultStepName = "Faulting Step",
            Fault = new System.Runtime.InteropServices.InvalidComObjectException(
                SpatialAnalyzerSdkAdapterTests.RecordingSdkCalls.SensitiveFaultText)
        };
        var diagnostics = new StringWriter();
        var host = WorkerControlHost.RunAsync(name, "localhost",
            () => new SpatialAnalyzerSdkAdapter(calls), diagnostics);
        try
        {
            await pipe.WaitForConnectionAsync(timeout.Token);
            using var channel = new WorkerControlChannel(pipe, leaveOpen: true);
            await ConnectAndVerify(channel, timeout.Token);

            await channel.SendAsync(WorkerControlMessage.Execute(Guid.NewGuid(),
                new("faulting", "Faulting Step", [], [])), timeout.Token);

            // The worker exits without a response, so the supervisor retires the
            // generation and reports an unknown outcome.
            Assert.Equal(WorkerControlHost.UnexpectedFaultExitCode, await host.WaitAsync(timeout.Token));
            await Assert.ThrowsAsync<EndOfStreamException>(
                () => channel.ReceiveAsync(timeout.Token).AsTask());
            var written = diagnostics.ToString();
            Assert.Contains("worker-unexpected-fault", written, StringComparison.Ordinal);
            Assert.Contains(nameof(System.Runtime.InteropServices.InvalidComObjectException), written, StringComparison.Ordinal);
            Assert.DoesNotContain("Sensitive", written, StringComparison.Ordinal);
            Assert.DoesNotContain("fault text", written, StringComparison.Ordinal);
        }
        finally
        {
            await pipe.DisposeAsync();
            await host.WaitAsync(TimeSpan.FromSeconds(3));
        }
    }

    private static async Task ConnectAndVerify(WorkerControlChannel channel, CancellationToken token)
    {
        Assert.Equal(WorkerControlMessageKind.Ready, (await channel.ReceiveAsync(token).ConfigureAwait(false)).Kind);
        var connected = await Exchange(channel, WorkerControlMessage.Connect(Guid.NewGuid()), token).ConfigureAwait(false);
        Assert.Equal(WorkerConnectionState.Connected, connected.Connection!.State);
        var verified = await Exchange(channel, WorkerControlMessage.VerifyExecution(Guid.NewGuid()), token).ConfigureAwait(false);
        Assert.Equal(WorkerExecutionReadinessState.ExecutionReady, verified.Connection!.ExecutionReadinessState);
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
