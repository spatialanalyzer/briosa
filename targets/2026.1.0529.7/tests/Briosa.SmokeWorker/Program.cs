using System.IO.Pipes;
using System.Runtime.InteropServices;
using Briosa.Worker.Control;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

var executableName = Path.GetFileName(Environment.ProcessPath);
if (args.Contains("--hold", StringComparer.Ordinal) ||
    string.Equals(
        executableName,
        "Spatial Analyzer64.exe",
        StringComparison.OrdinalIgnoreCase))
{
    FakeSpatialAnalyzerApplication.Run();
    return 0;
}

return SmokeWorkerProgram.Run(args);

internal static class FakeSpatialAnalyzerApplication
{
    private const uint WindowStyle = 0x00CF0000;
    private const int ShowNormal = 1;
    private const uint DestroyMessage = 0x0002;
    private const uint CloseMessage = 0x0010;
    private static readonly WindowProcedure Procedure = ProcessMessage;

    public static void Run()
    {
        var instance = GetModuleHandle(null);
        var className = $"BriosaFakeSpatialAnalyzer{Environment.ProcessId}";
        var windowClass = new WindowClass
        {
            Size = (uint)Marshal.SizeOf<WindowClass>(),
            Instance = instance,
            Procedure = Procedure,
            ClassName = className
        };
        if (RegisterClassEx(ref windowClass) == 0)
        {
            throw new InvalidOperationException("Could not register the fake application window.");
        }

        var window = CreateWindowEx(
            0,
            className,
            "SpatialAnalyzer Portable Conformance Host",
            WindowStyle,
            100,
            100,
            640,
            480,
            IntPtr.Zero,
            IntPtr.Zero,
            instance,
            IntPtr.Zero);
        if (window == IntPtr.Zero)
        {
            throw new InvalidOperationException("Could not create the fake application window.");
        }

        ShowWindow(window, ShowNormal);
        while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref message);
            DispatchMessage(ref message);
        }
    }

    private static IntPtr ProcessMessage(
        IntPtr window,
        uint message,
        IntPtr wordParameter,
        IntPtr longParameter)
    {
        switch (message)
        {
            case CloseMessage:
                DestroyWindow(window);
                return IntPtr.Zero;
            case DestroyMessage:
                PostQuitMessage(0);
                return IntPtr.Zero;
            default:
                return DefWindowProc(window, message, wordParameter, longParameter);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WindowClass
    {
        public uint Size;
        public uint Style;
        public WindowProcedure? Procedure;
        public int ClassExtra;
        public int WindowExtra;
        public IntPtr Instance;
        public IntPtr Icon;
        public IntPtr Cursor;
        public IntPtr Background;
        public string? MenuName;
        public string? ClassName;
        public IntPtr SmallIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        public IntPtr Window;
        public uint Value;
        public IntPtr WordParameter;
        public IntPtr LongParameter;
        public uint Time;
        public int PointX;
        public int PointY;
        public uint Private;
    }

    private delegate IntPtr WindowProcedure(
        IntPtr window,
        uint message,
        IntPtr wordParameter,
        IntPtr longParameter);

    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? moduleName);

    [DllImport("user32.dll", EntryPoint = "RegisterClassExW", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx(ref WindowClass windowClass);

    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        uint extendedStyle,
        string className,
        string windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        IntPtr parent,
        IntPtr menu,
        IntPtr instance,
        IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr window, int command);

    [DllImport("user32.dll", EntryPoint = "GetMessageW")]
    private static extern int GetMessage(
        out Message message,
        IntPtr window,
        uint minimumMessage,
        uint maximumMessage);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref Message message);

    [DllImport("user32.dll", EntryPoint = "DispatchMessageW")]
    private static extern IntPtr DispatchMessage(ref Message message);

    [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
    private static extern IntPtr DefWindowProc(
        IntPtr window,
        uint message,
        IntPtr wordParameter,
        IntPtr longParameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int exitCode);
}

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
            StartExitSignalWatcher(options.ExitSignalPath);
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

    private static void StartExitSignalWatcher(string? exitSignalPath)
    {
        if (string.IsNullOrWhiteSpace(exitSignalPath))
        {
            return;
        }

        var watcher = new Thread(() =>
        {
            while (!File.Exists(exitSignalPath))
            {
                Thread.Sleep(25);
            }

            Environment.Exit(37);
        })
        {
            IsBackground = true,
            Name = "Briosa smoke worker exit-signal watcher"
        };
        watcher.Start();
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
        string? StatePath,
        string? ExitSignalPath)
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
                    "BRIOSA_TEST_WORKER_STATE_PATH"),
                Environment.GetEnvironmentVariable(
                    "BRIOSA_TEST_WORKER_EXIT_SIGNAL_PATH"));
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
