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
            if (options.Scenario == TestWorkerScenario.HangBeforeReady)
            {
                Thread.Sleep(Timeout.Infinite);
            }

            using var pipe = new NamedPipeClientStream(
                ".",
                options.PipeName,
                PipeDirection.InOut,
                PipeOptions.None);
            pipe.Connect(15_000);
            using var channel = new WorkerControlChannel(pipe, leaveOpen: true);
            var connection = options.Scenario switch
            {
                TestWorkerScenario.SdkActivationFailed =>
                    FaultedSnapshot("fake-start-rejected", WorkerConnectionFailure.ActivationFailed),
                TestWorkerScenario.SdkActivationFailedTimeoutText =>
                    FaultedSnapshot("fake-start-rejected-timeout-word-only", WorkerConnectionFailure.ActivationFailed),
                TestWorkerScenario.Disconnected or
                    TestWorkerScenario.ConnectUnavailableOnce or
                    TestWorkerScenario.HangOnConnect or
                    TestWorkerScenario.SdkProcessExitOnPing =>
                        DisconnectedSnapshot(),
                _ => ConnectionSnapshot(
                        WorkerExecutionReadinessState.Unverified,
                        scenario: options.Scenario)
            };
            var connectCount = 0;
            channel.Send(
                WorkerControlMessage.Ready(
                    Environment.ProcessId,
                    connection));

            while (true)
            {
                var message = channel.Receive();
                switch (message.Kind)
                {
                    case WorkerControlMessageKind.VerifyExecution:
                        if (options.Scenario == TestWorkerScenario.HangOnVerify)
                        {
                            Thread.Sleep(Timeout.Infinite);
                        }

                        if (options.Scenario == TestWorkerScenario.CrashOnVerify)
                        {
                            Environment.Exit(44);
                        }

                        var verificationFailed =
                            options.Scenario == TestWorkerScenario.RejectVerify;
                        connection = ConnectionSnapshot(
                            verificationFailed
                                ? WorkerExecutionReadinessState.OperatorRecoveryRequired
                                : WorkerExecutionReadinessState.ExecutionReady,
                            verificationFailed
                                ? "execution-readiness-probe-mp-failed"
                                : "execution-readiness-verified",
                            options.Scenario);
                        channel.Send(WorkerControlMessage.ExecutionVerificationResult(
                            message.CorrelationId,
                            connection));
                        break;
                    case WorkerControlMessageKind.Ping:
                        if (options.Scenario == TestWorkerScenario.HangOnPing)
                        {
                            Thread.Sleep(Timeout.Infinite);
                        }

                        if (options.Scenario == TestWorkerScenario.CrashOnPing)
                        {
                            Environment.Exit(42);
                        }

                        if (options.Scenario == TestWorkerScenario.SdkProcessExitOnPing &&
                            connection.State == WorkerConnectionState.Connected)
                        {
                            connection = connection with
                            {
                                State = WorkerConnectionState.Faulted,
                                ExecutionReadinessState =
                                    WorkerExecutionReadinessState.Unverified,
                                DiagnosticCode = "fake-engine-ended",
                                Failure = WorkerConnectionFailure.ProcessExited,
                                TransitionedAt = DateTimeOffset.UtcNow
                            };
                        }

                        channel.Send(WorkerControlMessage.Pong(
                            message.CorrelationId,
                            connection));
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

                        var completed = WorkerControlMessage.ExecutionResult(
                            message.CorrelationId,
                            CompletedExecution(
                                message.Command!,
                                mpSucceeded: options.Scenario != TestWorkerScenario.MpFailure,
                                delayed,
                                options.Scenario));
                        if (options.Scenario == TestWorkerScenario.CrashAfterExecute)
                        {
                            Environment.Exit(45);
                        }

                        if (options.Scenario == TestWorkerScenario.DropExecutionResponse)
                        {
                            Thread.Sleep(Timeout.Infinite);
                        }

                        channel.Send(completed);
                        break;
                    case WorkerControlMessageKind.Connect:
                        if (options.Scenario == TestWorkerScenario.HangOnConnect)
                        {
                            Thread.Sleep(Timeout.Infinite);
                        }

                        connectCount++;
                        connection = options.Scenario ==
                                TestWorkerScenario.ConnectUnavailableOnce &&
                            connectCount == 1
                                ? FaultedSnapshot("connect-ex-unavailable")
                                : ConnectionSnapshot(
                                    WorkerExecutionReadinessState.Unverified,
                                    scenario: options.Scenario);
                        channel.Send(WorkerControlMessage.ConnectionResult(
                            message.CorrelationId,
                            connection));
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
        bool delayed,
        TestWorkerScenario scenario) =>
        new(
            WorkerExecutionResponseStatus.Completed,
            WorkerMpExecutionResult.FromEvidence(
                executeStepReturned: true,
                mpResultRetrieved: true,
                mpSucceeded,
                mpSucceeded ? 2 : 3,
                durationMilliseconds: delayed ? 300 : 5,
                mpSucceeded
                    ? [.. command.OutputArguments.Select(CreateOutputValue)]
                    : [],
                mpSucceeded ? null : "scripted-mp-failure"),
            ConnectionSnapshot(
                WorkerExecutionReadinessState.ExecutionReady,
                scenario: scenario),
            DiagnosticCode: null);

    private static WorkerMpOutputValue CreateOutputValue(WorkerMpOutputArgument output) =>
        output.Kind switch
        {
            WorkerMpValueKind.Logical =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerBooleanValue(true)),
            WorkerMpValueKind.WholeNumber =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerIntegerValue(7)),
            WorkerMpValueKind.FloatingPoint =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerDoubleValue(1.25)),
            WorkerMpValueKind.Text or
            WorkerMpValueKind.ChartName or
            WorkerMpValueKind.CloudName or
            WorkerMpValueKind.CollectionName or
            WorkerMpValueKind.FrameName or
            WorkerMpValueKind.VectorGroupName or
            WorkerMpValueKind.ViewName =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerTextValue("scripted-output")),
            WorkerMpValueKind.PointName =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerPointNameValue(
                        "Collection",
                        "Group",
                        "Point")),
            WorkerMpValueKind.Vector =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerVectorValue(1, 2, 3)),
            WorkerMpValueKind.ToleranceVectorOptions =>
                new WorkerRetrievedOutput(output.Name, output.Kind, CreateToleranceVectorOptions()),
            WorkerMpValueKind.CollectionInstrumentId =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerCollectionInstrumentIdValue("Collection", 17)),
            WorkerMpValueKind.CollectionInstrumentIdList =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerCollectionInstrumentIdListValue(
                            [new WorkerCollectionInstrumentIdValue("Collection", 17)])),
            WorkerMpValueKind.CollectionMachineId =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerCollectionMachineIdValue("Collection", 18)),
            WorkerMpValueKind.CollectionItemName =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerCollectionItemNameValue(
                            "Collection", "Picture", WorkerItemTypeValue.Picture)),
            WorkerMpValueKind.CollectionItemNameList =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerCollectionItemNameListValue(
                            [new WorkerCollectionItemNameValue(
                                "Collection", "Report", WorkerItemTypeValue.SaReport)])),
            WorkerMpValueKind.CollectionObjectName =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerCollectionObjectNameValue(
                            "Collection", "Object", WorkerObjectTypeValue.PointGroup)),
            WorkerMpValueKind.CollectionObjectNameList =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerCollectionObjectNameListValue(
                            [new WorkerCollectionObjectNameValue(
                                "Collection", "Object", WorkerObjectTypeValue.PointGroup)])),
            WorkerMpValueKind.CollectionGroupNameList =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerCollectionGroupNameListValue(
                            [new WorkerCollectionGroupNameValue("Collection", "Group")])),
            WorkerMpValueKind.CollectionVectorGroupName =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerCollectionVectorGroupNameValue("Collection", "Vectors")),
            WorkerMpValueKind.CollectionVectorGroupNameList =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerCollectionVectorGroupNameListValue(
                            [new WorkerCollectionVectorGroupNameValue(
                                "Collection", "Vectors")])),
            WorkerMpValueKind.PointNameList =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerPointNameListValue(
                            [new WorkerPointNameValue("Collection", "Group", "Point")])),
            WorkerMpValueKind.StringList =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerStringListValue(["A", "B"])),
            WorkerMpValueKind.VectorNameList =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerVectorNameListValue(
                            [new WorkerVectorNameValue(
                                "Collection", "Vectors", "Vector")])),
            _ => new WorkerUnavailableOutput(output.Name, output.Kind)
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

    private static WorkerConnectionSnapshot ConnectionSnapshot(
        WorkerExecutionReadinessState readinessState,
        string diagnosticCode = "connect-ex-connected",
        TestWorkerScenario scenario = TestWorkerScenario.Normal) =>
        new(
            WorkerConnectionState.Connected,
            readinessState,
            StatusCode: 0,
            Attempt: 1,
            MaximumAttempts: 1,
            diagnosticCode,
            DateTimeOffset.UtcNow,
            scenario switch
            {
                TestWorkerScenario.RuntimeIdentityMismatch =>
                    new WorkerRuntimeIdentitySnapshot(
                        new WorkerRuntimeIdentityEvidence(
                            "2025.0",
                            WorkerRuntimeIdentityEvidenceSource.RuntimeVerified),
                        new WorkerRuntimeIdentityEvidence(
                            "2026.1.0529.7",
                            WorkerRuntimeIdentityEvidenceSource.RuntimeVerified)),
                TestWorkerScenario.MalformedRuntimeIdentity =>
                    new WorkerRuntimeIdentitySnapshot(
                        new WorkerRuntimeIdentityEvidence(
                            Version: null,
                            WorkerRuntimeIdentityEvidenceSource.RuntimeVerified),
                        new WorkerRuntimeIdentityEvidence(
                            Version: null,
                            WorkerRuntimeIdentityEvidenceSource.Unavailable)),
                _ => new WorkerRuntimeIdentitySnapshot(
                    new WorkerRuntimeIdentityEvidence(
                        Version: null,
                        WorkerRuntimeIdentityEvidenceSource.Unavailable),
                    new WorkerRuntimeIdentityEvidence(
                        Version: null,
                        WorkerRuntimeIdentityEvidenceSource.Unavailable))
            });

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

    private static WorkerConnectionSnapshot FaultedSnapshot(string diagnosticCode, WorkerConnectionFailure failure = WorkerConnectionFailure.None) =>
        new(
            WorkerConnectionState.Faulted,
            WorkerExecutionReadinessState.Unverified,
            StatusCode: -1,
            Attempt: 1,
            MaximumAttempts: 1,
            diagnosticCode,
            DateTimeOffset.UtcNow,
            new WorkerRuntimeIdentitySnapshot(
                new WorkerRuntimeIdentityEvidence(
                    Version: null,
                    WorkerRuntimeIdentityEvidenceSource.Unavailable),
                new WorkerRuntimeIdentityEvidence(
                    Version: null,
                    WorkerRuntimeIdentityEvidenceSource.Unavailable)), Failure: failure);

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
    CrashOnExecute,
    CrashAfterExecute,
    DropExecutionResponse,
    HangOnVerify,
    CrashOnVerify,
    RejectVerify,
    RuntimeIdentityMismatch,
    MalformedRuntimeIdentity,
    Disconnected,
    HangBeforeReady,
    SdkActivationFailed,
    SdkActivationFailedTimeoutText,
    ConnectUnavailableOnce,
    HangOnConnect,
    SdkProcessExitOnPing
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
            "crash-after-execute" => TestWorkerScenario.CrashAfterExecute,
            "drop-execution-response" => TestWorkerScenario.DropExecutionResponse,
            "hang-on-verify" => TestWorkerScenario.HangOnVerify,
            "crash-on-verify" => TestWorkerScenario.CrashOnVerify,
            "reject-verify" => TestWorkerScenario.RejectVerify,
            "runtime-identity-mismatch" => TestWorkerScenario.RuntimeIdentityMismatch,
            "malformed-runtime-identity" => TestWorkerScenario.MalformedRuntimeIdentity,
            "disconnected" => TestWorkerScenario.Disconnected,
            "hang-before-ready" => TestWorkerScenario.HangBeforeReady,
            "sdk-activation-failed" => TestWorkerScenario.SdkActivationFailed,
            "sdk-activation-failed-timeout-text" => TestWorkerScenario.SdkActivationFailedTimeoutText,
            "connect-unavailable-once" => TestWorkerScenario.ConnectUnavailableOnce,
            "hang-on-connect" => TestWorkerScenario.HangOnConnect,
            "sdk-process-exit-on-ping" => TestWorkerScenario.SdkProcessExitOnPing,
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
