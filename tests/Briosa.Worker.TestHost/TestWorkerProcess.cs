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

        try
        {
            using var pipe = new NamedPipeClientStream(
                ".",
                options.PipeName,
                PipeDirection.InOut,
                PipeOptions.None);
            pipe.Connect(15_000);
            using var channel = new WorkerControlChannel(pipe, leaveOpen: true);
            channel.Send(WorkerControlMessage.Ready(Environment.ProcessId));

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
    IgnoreStop
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
