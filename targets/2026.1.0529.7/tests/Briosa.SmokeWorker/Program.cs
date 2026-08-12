using System.IO.Pipes;
using Briosa.Worker.Control;

if (args.Contains("--hold", StringComparer.Ordinal))
{
    Thread.Sleep(Timeout.Infinite);
    return 0;
}

return SmokeWorkerProgram.Run(args);

internal static class SmokeWorkerProgram
{
    public static int Run(string[] arguments)
    {
        var options = SmokeWorkerOptions.Parse(arguments);
        var completion = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() => completion.SetResult(RunOnSta(options)))
        {
            IsBackground = false,
            Name = "Briosa smoke worker STA"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task.GetAwaiter().GetResult();
    }

    private static int RunOnSta(SmokeWorkerOptions options)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(
                ".",
                options.PipeName,
                PipeDirection.InOut,
                PipeOptions.None);
            pipe.Connect(15_000);
            using var channel = new WorkerControlChannel(pipe, leaveOpen: true);
            var connection = DisconnectedSnapshot();
            channel.Send(WorkerControlMessage.Ready(
                Environment.ProcessId,
                connection));
            var executionCount = 0;

            while (true)
            {
                var message = channel.Receive();
                switch (message.Kind)
                {
                    case WorkerControlMessageKind.VerifyExecution:
                        connection = ConnectionSnapshot(
                            options.Scenario,
                            options.Scenario == SmokeWorkerScenario.Disconnected
                                ? WorkerExecutionReadinessState.Unverified
                                : WorkerExecutionReadinessState.ExecutionReady);
                        channel.Send(WorkerControlMessage.ExecutionVerificationResult(
                            message.CorrelationId,
                            connection));
                        break;
                    case WorkerControlMessageKind.Ping:
                        channel.Send(WorkerControlMessage.Pong(
                            message.CorrelationId,
                            connection));
                        break;
                    case WorkerControlMessageKind.Connect:
                        connection = ConnectionSnapshot(
                            options.Scenario,
                            WorkerExecutionReadinessState.Unverified);
                        channel.Send(WorkerControlMessage.ConnectionResult(
                            message.CorrelationId,
                            connection));
                        break;
                    case WorkerControlMessageKind.Execute:
                        executionCount++;
                        Execute(
                            channel,
                            message,
                            options,
                            executionCount,
                            connection);
                        break;
                    case WorkerControlMessageKind.Stop:
                        channel.Send(WorkerControlMessage.Stopped(message.CorrelationId));
                        return 0;
                    default:
                        return 4;
                }
            }
        }
        catch (IOException)
        {
            return 3;
        }
    }

    private static void Execute(
        WorkerControlChannel channel,
        WorkerControlMessage message,
        SmokeWorkerOptions options,
        int executionCount,
        WorkerConnectionSnapshot connection)
    {
        if (options.Scenario == SmokeWorkerScenario.Disconnected)
        {
            channel.Send(WorkerControlMessage.ExecutionResult(
                message.CorrelationId,
                new WorkerExecutionResponse(
                    WorkerExecutionResponseStatus.Unavailable,
                    Execution: null,
                    connection,
                    "sdk-connection-not-ready")));
            return;
        }

        if (options.Scenario == SmokeWorkerScenario.HangFirstExecute &&
            ClaimFirstExecution(options.StatePath))
        {
            Thread.Sleep(Timeout.Infinite);
        }

        if (options.Scenario == SmokeWorkerScenario.DelayFirstExecute &&
            executionCount == 1)
        {
            Thread.Sleep(300);
        }

        var mpSucceeded = options.Scenario != SmokeWorkerScenario.MpFailure;
        var outputFailure = options.Scenario == SmokeWorkerScenario.OutputFailure;
        var diagnosticCode = !mpSucceeded
            ? "scripted-mp-failure"
            : outputFailure
                ? "sdk-output-retrieval-failed"
                : null;
        var outputs = !mpSucceeded
            ? []
            : message.Command!.OutputArguments.Select(output =>
                CreateOutput(output, outputFailure))
                .ToArray();
        channel.Send(WorkerControlMessage.ExecutionResult(
            message.CorrelationId,
            new WorkerExecutionResponse(
                WorkerExecutionResponseStatus.Completed,
                new WorkerMpExecutionResult(
                    ExecuteStepReturned: true,
                    MpResultRetrieved: true,
                    mpSucceeded,
                    MpResultCode: mpSucceeded ? 2 : 3,
                    DurationMilliseconds: 5,
                    outputs,
                    diagnosticCode),
                connection,
                DiagnosticCode: null)));
    }

    private static WorkerMpOutputValue CreateOutput(
        WorkerMpOutputArgument output,
        bool outputFailure)
    {
        if (outputFailure)
        {
            return new WorkerMpOutputValue(
                output.Name,
                output.Kind,
                Retrieved: false);
        }

        return output.Kind switch
        {
            WorkerMpValueKind.WholeNumber => new WorkerMpOutputValue(
                output.Name,
                output.Kind,
                Retrieved: true,
                IntegerValue: 3),
            WorkerMpValueKind.CollectionObjectName => new WorkerMpOutputValue(
                output.Name,
                output.Kind,
                Retrieved: true,
                CollectionObjectNameValue: new WorkerCollectionObjectNameValue(
                    "Collection",
                    "Object",
                    WorkerObjectTypeValue.PointGroup)),
            _ => new WorkerMpOutputValue(
                output.Name,
                output.Kind,
                Retrieved: true,
                StringValue: "scripted-output")
        };
    }

    private static WorkerConnectionSnapshot ConnectionSnapshot(
        SmokeWorkerScenario scenario,
        WorkerExecutionReadinessState readinessState)
    {
        var connected = scenario != SmokeWorkerScenario.Disconnected;
        return new WorkerConnectionSnapshot(
            connected ? WorkerConnectionState.Connected : WorkerConnectionState.Faulted,
            readinessState,
            StatusCode: connected ? 0 : -1,
            Attempt: 1,
            MaximumAttempts: 1,
            connected ? "connect-ex-connected" : "sdk-connection-not-ready",
            DateTimeOffset.UtcNow,
            new WorkerRuntimeIdentitySnapshot(
                new WorkerRuntimeIdentityEvidence(
                    Version: null,
                    WorkerRuntimeIdentityEvidenceSource.Unavailable),
                new WorkerRuntimeIdentityEvidence(
                    Version: null,
                    WorkerRuntimeIdentityEvidenceSource.Unavailable)));
    }

    private static WorkerConnectionSnapshot DisconnectedSnapshot() =>
        new(
            WorkerConnectionState.Disconnected,
            WorkerExecutionReadinessState.Unverified,
            StatusCode: null,
            Attempt: 0,
            MaximumAttempts: 1,
            "sdk-started",
            DateTimeOffset.UtcNow,
            new WorkerRuntimeIdentitySnapshot(
                new WorkerRuntimeIdentityEvidence(
                    Version: null,
                    WorkerRuntimeIdentityEvidenceSource.Unavailable),
                new WorkerRuntimeIdentityEvidence(
                    Version: null,
                    WorkerRuntimeIdentityEvidenceSource.Unavailable)));

    private static bool ClaimFirstExecution(string? statePath)
    {
        if (string.IsNullOrWhiteSpace(statePath))
        {
            throw new InvalidOperationException(
                "The hang-first-execute scenario requires a state path.");
        }

        try
        {
            using var marker = new FileStream(
                statePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private enum SmokeWorkerScenario
    {
        Ready,
        Disconnected,
        MpFailure,
        OutputFailure,
        DelayFirstExecute,
        HangFirstExecute
    }

    private sealed record SmokeWorkerOptions(
        string PipeName,
        SmokeWorkerScenario Scenario,
        string? StatePath)
    {
        public static SmokeWorkerOptions Parse(string[] arguments)
        {
            if (!TryGetArgument(arguments, "--control-pipe", out var pipeName))
            {
                throw new ArgumentException(
                    "The control pipe argument is required.",
                    nameof(arguments));
            }

            var scenarioName =
                Environment.GetEnvironmentVariable("BRIOSA_TEST_WORKER_SCENARIO") ??
                "ready";
            var scenario = scenarioName switch
            {
                "ready" => SmokeWorkerScenario.Ready,
                "disconnected" => SmokeWorkerScenario.Disconnected,
                "mp-failure" => SmokeWorkerScenario.MpFailure,
                "output-failure" => SmokeWorkerScenario.OutputFailure,
                "delay-first-execute" => SmokeWorkerScenario.DelayFirstExecute,
                "hang-first-execute" => SmokeWorkerScenario.HangFirstExecute,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(arguments),
                    scenarioName,
                    "The smoke worker scenario is not supported.")
            };
            return new SmokeWorkerOptions(
                pipeName,
                scenario,
                Environment.GetEnvironmentVariable(
                    "BRIOSA_TEST_WORKER_STATE_PATH"));
        }

        private static bool TryGetArgument(
            string[] arguments,
            string name,
            out string value)
        {
            var index = Array.IndexOf(arguments, name);
            if (index >= 0 && index + 1 < arguments.Length)
            {
                value = arguments[index + 1];
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}
