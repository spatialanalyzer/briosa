using System.IO.Pipes;
using System.Text.Json;
using Briosa.Worker.Control;

namespace Briosa.Worker.TestHost;

internal static class TestWorkerProcess
{
    public static int Run(string[] arguments)
    {
        var options = TestWorkerOptions.Parse(arguments);
        var completion = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() => completion.SetResult(RunOnSta(options)))
        {
            IsBackground = false,
            Name = "Briosa fake worker STA"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task.GetAwaiter().GetResult();
    }

    private static int RunOnSta(TestWorkerOptions options)
    {
        var record = new LifecycleRecord(
            Environment.CurrentManagedThreadId,
            Thread.CurrentThread.GetApartmentState().ToString(),
            ReleaseThreadId: null,
            ReleaseApartment: null);
        WriteRecord(options.LifecycleRecordPath, record);
        var executionCount = 0;

        try
        {
            using var pipe = new NamedPipeClientStream(
                ".",
                options.PipeName,
                PipeDirection.InOut,
                PipeOptions.None);
            pipe.Connect(15_000);
            using var channel = new WorkerControlChannel(pipe, leaveOpen: true);
            channel.Send(
                WorkerControlMessage.Ready(
                    Environment.ProcessId,
                    ConnectionSnapshot()));

            while (true)
            {
                var message = channel.Receive();
                switch (message.Kind)
                {
                    case WorkerControlMessageKind.Ping:
                        if (options.Scenario == TestWorkerScenario.HangOnPing)
                        {
                            Thread.Sleep(Timeout.Infinite);
                        }

                        if (options.Scenario == TestWorkerScenario.CrashOnPing)
                        {
                            Environment.Exit(42);
                        }

                        channel.Send(WorkerControlMessage.Pong(message.CorrelationId));
                        break;
                    case WorkerControlMessageKind.Execute:
                        executionCount++;
                        if (options.Scenario == TestWorkerScenario.HangOnExecute)
                        {
                            Thread.Sleep(Timeout.Infinite);
                        }

                        if (options.Scenario == TestWorkerScenario.CrashOnExecute)
                        {
                            Environment.Exit(43);
                        }

                        var delayed = options.Scenario == TestWorkerScenario.DelayFirstExecute &&
                            executionCount == 1;
                        if (delayed)
                        {
                            Thread.Sleep(300);
                        }

                        channel.Send(
                            WorkerControlMessage.ExecutionResult(
                                message.CorrelationId,
                                CompletedExecution(
                                    message.Command!,
                                    mpSucceeded:
                                        options.Scenario != TestWorkerScenario.MpFailure,
                                    delayed)));
                        break;
                    case WorkerControlMessageKind.Stop:
                        if (options.Scenario == TestWorkerScenario.IgnoreStop)
                        {
                            Thread.Sleep(Timeout.Infinite);
                        }

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
        finally
        {
            WriteRecord(
                options.LifecycleRecordPath,
                record with
                {
                    ReleaseThreadId = Environment.CurrentManagedThreadId,
                    ReleaseApartment = Thread.CurrentThread.GetApartmentState().ToString()
                });
        }
    }

    private static WorkerExecutionResponse CompletedExecution(
        WorkerMpCommand command,
        bool mpSucceeded,
        bool delayed) =>
        new(
            WorkerExecutionResponseStatus.Completed,
            new WorkerMpExecutionResult(
                ExecuteStepReturned: true,
                mpSucceeded,
                mpSucceeded ? 0 : 42,
                DurationMilliseconds: delayed ? 300 : 5,
                mpSucceeded
                    ? [.. command.OutputArguments.Select(CreateOutputValue)]
                    : [],
                mpSucceeded ? null : "scripted-mp-failure"),
            ConnectionSnapshot(),
            DiagnosticCode: null);

    private static WorkerMpOutputValue CreateOutputValue(WorkerMpOutputArgument output) =>
        output.Kind switch
        {
            WorkerMpValueKind.Logical =>
                new(output.Name, output.Kind, Retrieved: true, BooleanValue: true),
            WorkerMpValueKind.WholeNumber =>
                new(output.Name, output.Kind, Retrieved: true, IntegerValue: 7),
            WorkerMpValueKind.FloatingPoint =>
                new(output.Name, output.Kind, Retrieved: true, DoubleValue: 1.25),
            WorkerMpValueKind.Text =>
                new(output.Name, output.Kind, Retrieved: true, StringValue: "scripted-output"),
            WorkerMpValueKind.PointName =>
                new(
                    output.Name,
                    output.Kind,
                    Retrieved: true,
                    PointNameValue: new WorkerPointNameValue(
                        "Collection",
                        "Group",
                        "Point")),
            WorkerMpValueKind.Vector =>
                new(
                    output.Name,
                    output.Kind,
                    Retrieved: true,
                    VectorValue: new WorkerVectorValue(1, 2, 3)),
            WorkerMpValueKind.ToleranceVectorOptions =>
                new(
                    output.Name,
                    output.Kind,
                    Retrieved: true,
                    ToleranceVectorOptionsValue: CreateToleranceVectorOptions()),
            _ => new(output.Name, output.Kind, Retrieved: false)
        };

    private static WorkerToleranceVectorOptionsValue CreateToleranceVectorOptions() =>
        new(
            new WorkerToleranceLimit(Enabled: true, Value: 1),
            new WorkerToleranceLimit(Enabled: true, Value: 2),
            new WorkerToleranceLimit(Enabled: true, Value: 3),
            new WorkerToleranceLimit(Enabled: true, Value: 4),
            new WorkerToleranceLimit(Enabled: false, Value: -1),
            new WorkerToleranceLimit(Enabled: false, Value: -2),
            new WorkerToleranceLimit(Enabled: false, Value: -3),
            new WorkerToleranceLimit(Enabled: false, Value: -4));

    private static WorkerConnectionSnapshot ConnectionSnapshot() =>
        new(
            WorkerConnectionState.Connected,
            "localhost",
            StatusCode: 0,
            Attempt: 1,
            MaximumAttempts: 1,
            "connect-ex-connected",
            DateTimeOffset.UtcNow);

    private static void WriteRecord(string? path, LifecycleRecord record)
    {
        if (path is null)
        {
            return;
        }

        File.WriteAllText(path, JsonSerializer.Serialize(record));
    }

    private sealed record LifecycleRecord(
        int InitializationThreadId,
        string InitializationApartment,
        int? ReleaseThreadId,
        string? ReleaseApartment);
}

internal enum TestWorkerScenario
{
    Normal,
    HangOnPing,
    CrashOnPing,
    IgnoreStop,
    MpFailure,
    DelayFirstExecute,
    HangOnExecute,
    CrashOnExecute
}

internal sealed record TestWorkerOptions(
    string PipeName,
    TestWorkerScenario Scenario,
    string? LifecycleRecordPath)
{
    public static TestWorkerOptions Parse(string[] arguments)
    {
        if (!TryGetArgument(arguments, "--control-pipe", out var pipeName))
        {
            throw new ArgumentException("The control pipe argument is required.", nameof(arguments));
        }

        var scenario = TryGetArgument(arguments, "--scenario", out var scenarioName)
            ? ParseScenario(scenarioName)
            : TestWorkerScenario.Normal;
        var recordPath = TryGetArgument(arguments, "--lifecycle-record", out var path)
            ? path
            : null;
        return new TestWorkerOptions(pipeName, scenario, recordPath);
    }

    private static TestWorkerScenario ParseScenario(string value) =>
        value switch
        {
            "normal" => TestWorkerScenario.Normal,
            "hang-on-ping" => TestWorkerScenario.HangOnPing,
            "crash-on-ping" => TestWorkerScenario.CrashOnPing,
            "ignore-stop" => TestWorkerScenario.IgnoreStop,
            "mp-failure" => TestWorkerScenario.MpFailure,
            "delay-first-execute" => TestWorkerScenario.DelayFirstExecute,
            "hang-on-execute" => TestWorkerScenario.HangOnExecute,
            "crash-on-execute" => TestWorkerScenario.CrashOnExecute,
            _ => throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "The fake worker scenario is not supported.")
        };

    private static bool TryGetArgument(string[] arguments, string name, out string value)
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
