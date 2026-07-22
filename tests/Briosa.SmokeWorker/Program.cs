using System.IO.Pipes;
using Briosa.Worker.Control;

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
            channel.Send(WorkerControlMessage.Ready(
                Environment.ProcessId,
                ConnectionSnapshot(options.Scenario)));
            var executionCount = 0;

            while (true)
            {
                var message = channel.Receive();
                switch (message.Kind)
                {
                    case WorkerControlMessageKind.Ping:
                        channel.Send(WorkerControlMessage.Pong(message.CorrelationId));
                        break;
                    case WorkerControlMessageKind.Execute:
                        executionCount++;
                        Execute(channel, message, options, executionCount);
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
        int executionCount)
    {
        if (options.Scenario == SmokeWorkerScenario.Disconnected)
        {
            channel.Send(WorkerControlMessage.ExecutionResult(
                message.CorrelationId,
                new WorkerExecutionResponse(
                    WorkerExecutionResponseStatus.Unavailable,
                    Execution: null,
                    ConnectionSnapshot(options.Scenario),
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
                new WorkerMpOutputValue(
                    output.Name,
                    output.Kind,
                    Retrieved: !outputFailure,
                    StringValue: outputFailure ? null : "scripted-output"))
                .ToArray();
        channel.Send(WorkerControlMessage.ExecutionResult(
            message.CorrelationId,
            new WorkerExecutionResponse(
                WorkerExecutionResponseStatus.Completed,
                new WorkerMpExecutionResult(
                    ExecuteStepReturned: true,
                    mpSucceeded,
                    MpResultCode: mpSucceeded ? 0 : 42,
                    DurationMilliseconds: 5,
                    outputs,
                    diagnosticCode),
                ConnectionSnapshot(options.Scenario),
                DiagnosticCode: null)));
    }

    private static WorkerConnectionSnapshot ConnectionSnapshot(
        SmokeWorkerScenario scenario)
    {
        var connected = scenario != SmokeWorkerScenario.Disconnected;
        return new WorkerConnectionSnapshot(
            connected ? WorkerConnectionState.Connected : WorkerConnectionState.Faulted,
            "localhost",
            StatusCode: connected ? 0 : -1,
            Attempt: 1,
            MaximumAttempts: 1,
            connected ? "connect-ex-connected" : "sdk-connection-not-ready",
            DateTimeOffset.UtcNow);
    }

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
