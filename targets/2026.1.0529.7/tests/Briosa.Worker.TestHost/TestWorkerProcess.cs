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
                // Lets a test play a same-user impersonator on the launched pipe.
                if (options.PipeRecordPath is not null)
                {
                    var pending = options.PipeRecordPath + ".pending";
                    File.WriteAllLines(pending,
                    [
                        options.PipeName,
                        Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    ]);
                    File.Move(pending, options.PipeRecordPath);
                }

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
            if (options.ProtocolVersionOffset is { } offset and not 0)
            {
                // A worker built for another private protocol version: the host
                // must reject its Ready before it sends any command.
                SendOnProtocolVersion(pipe, WorkerControlMessage.Ready(Environment.ProcessId, connection),
                    WorkerControlProtocol.CurrentVersion + offset);
                Thread.Sleep(Timeout.Infinite);
            }

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
                        if (options.SdkFaultOutput is { } faultedOutput && executionCount == 1)
                        {
                            // An output-getter fault on the first execution, sent as the
                            // production worker host sends it: a frame over the response
                            // budget is replaced by the bounded fallback.
                            var fault = SdkFaultResponse(message.Command!, faultedOutput,
                                options.OutputTextLengths ?? [], options.Scenario);
                            try
                            {
                                channel.Send(WorkerControlMessage.ExecutionResult(message.CorrelationId, fault));
                            }
                            catch (WorkerMessageRejectedException)
                            {
                                channel.Send(WorkerExecutionDelivery.Undeliverable(message.CorrelationId, fault));
                            }

                            break;
                        }

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
                                options.Scenario,
                                options.OutputTextLength));
                        if (options.Scenario == TestWorkerScenario.UnexpectedKindOnExecute)
                        {
                            completed = WorkerControlMessage.Pong(message.CorrelationId, connection);
                        }
                        else if (options.Scenario == TestWorkerScenario.MismatchedCorrelationOnExecute)
                        {
                            completed = WorkerControlMessage.ExecutionResult(
                                Guid.NewGuid(),
                                completed.ExecutionResponse!);
                        }
                        else if (options.Scenario == TestWorkerScenario.TruncatedFrameOnExecute)
                        {
                            SendTruncatedFrameAndClose(pipe, completed);
                            Thread.Sleep(Timeout.Infinite);
                        }

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
        TestWorkerScenario scenario,
        int? outputTextLength) =>
        new(
            WorkerExecutionResponseStatus.Completed,
            WorkerMpExecutionResult.FromEvidence(
                executeStepReturned: true,
                mpResultRetrieved: true,
                mpSucceeded,
                mpSucceeded ? 2 : 3,
                durationMilliseconds: delayed ? 300 : 5,
                mpSucceeded
                    ? [.. command.OutputArguments.Select(output => CreateOutputValue(output, outputTextLength))]
                    : [],
                mpSucceeded ? null : "scripted-mp-failure"),
            ConnectionSnapshot(
                WorkerExecutionReadinessState.ExecutionReady,
                scenario: scenario),
            DiagnosticCode: null);

    // Never fabricate a value the production adapter cannot retrieve; doing so would hide
    // declared outputs that fail after MP success in the real worker.
    // The named output's getter faults; Text outputs before and after it take the
    // given lengths in order, and every other output its usual value.
    private static WorkerExecutionResponse SdkFaultResponse(
        WorkerMpCommand command,
        string faultedOutput,
        IReadOnlyList<int> textLengths,
        TestWorkerScenario scenario)
    {
        var nextLength = 0;
        var outputs = command.OutputArguments.Select(output =>
            output.Name == faultedOutput
                ? new WorkerUnavailableOutput(output.Name, output.Kind,
                    WorkerSdkFaultDiagnosticCodes.OutputGetter, WorkerUnavailableOutputReason.SdkCallFaulted)
                : output.Kind == WorkerMpValueKind.Text && nextLength < textLengths.Count
                    ? new WorkerRetrievedOutput(output.Name, output.Kind,
                        new WorkerTextValue(new string('x', textLengths[nextLength++])))
                    : CreateOutputValue(output, outputTextLength: null)).ToArray();
        return new WorkerExecutionResponse(
            WorkerExecutionResponseStatus.Completed,
            new WorkerSdkCallFaulted(WorkerSdkCallPhase.OutputGetter, 5, outputs,
                WorkerSdkFaultDiagnosticCodes.OutputGetter, resultCode: 2),
            ConnectionSnapshot(WorkerExecutionReadinessState.ExecutionReady, scenario: scenario),
            DiagnosticCode: null);
    }

    // --output-text-length makes every Text output that many characters, so a test
    // can send a result of a chosen size through the real private channel.
    private static WorkerMpOutputValue CreateOutputValue(WorkerMpOutputArgument output, int? outputTextLength) =>
        output.Kind switch
        {
            _ when !WorkerRetrievableOutputKinds.Contains(output.Kind) =>
                new WorkerUnavailableOutput(output.Name, output.Kind),
            WorkerMpValueKind.Text when outputTextLength is { } length =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerTextValue(new string('x', length))),
            WorkerMpValueKind.Logical =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerBooleanValue(true)),
            WorkerMpValueKind.WholeNumber =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerIntegerValue(7)),
            WorkerMpValueKind.FloatingPoint =>
                new WorkerRetrievedOutput(output.Name, output.Kind, new WorkerDoubleValue(1.25)),
            WorkerMpValueKind.Text or
            WorkerMpValueKind.CollectionName =>
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

    // Encodes the message on the current version, then rewrites its version field,
    // which the channel itself refuses to send.
    private static void SendOnProtocolVersion(Stream pipe, WorkerControlMessage message, int version)
    {
        using var frame = new MemoryStream();
        using (var encoder = new WorkerControlChannel(frame, leaveOpen: true))
        {
            encoder.Send(message);
        }

        var payload = System.Text.Encoding.UTF8.GetString(frame.GetBuffer(), sizeof(int), (int)frame.Length - sizeof(int));
        var current = $"\"protocolVersion\":{WorkerControlProtocol.CurrentVersion}";
        if (!payload.Contains(current, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The encoded message has no protocol version field.");
        }

        var rewritten = System.Text.Encoding.UTF8.GetBytes(payload.Replace(current,
            $"\"protocolVersion\":{version.ToString(System.Globalization.CultureInfo.InvariantCulture)}", StringComparison.Ordinal));
        var header = new byte[sizeof(int)];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header, rewritten.Length);
        pipe.Write(header);
        pipe.Write(rewritten);
        pipe.Flush();
    }

    private static void SendTruncatedFrameAndClose(Stream pipe, WorkerControlMessage message)
    {
        using var frame = new MemoryStream();
        using (var encoder = new WorkerControlChannel(frame, leaveOpen: true))
        {
            encoder.Send(message);
        }

        // A valid header followed by half of its payload, then end of stream.
        pipe.Write(frame.GetBuffer(), 0, (int)(frame.Length / 2));
        pipe.Flush();
        pipe.Dispose();
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
    SdkProcessExitOnPing,
    UnexpectedKindOnExecute,
    MismatchedCorrelationOnExecute,
    TruncatedFrameOnExecute
}

internal sealed record TestWorkerOptions(
    string PipeName,
    TestWorkerScenario Scenario,
    string? LifecycleRecordPath,
    string? PipeRecordPath,
    int? OutputTextLength = null,
    int? ProtocolVersionOffset = null,
    string? SdkFaultOutput = null,
    IReadOnlyList<int>? OutputTextLengths = null)
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
        var pipeRecordPath = TryGetArgument(arguments, "--pipe-record", out var pipeRecord)
            ? pipeRecord
            : null;
        int? outputTextLength = TryGetArgument(arguments, "--output-text-length", out var length)
            ? int.Parse(length, System.Globalization.CultureInfo.InvariantCulture)
            : null;
        int? protocolVersionOffset = TryGetArgument(arguments, "--protocol-version-offset", out var offset)
            ? int.Parse(offset, System.Globalization.CultureInfo.InvariantCulture)
            : null;
        string? sdkFaultOutput = TryGetArgument(arguments, "--sdk-fault-output", out var faulted)
            ? faulted
            : null;
        IReadOnlyList<int>? outputTextLengths = TryGetArgument(arguments, "--output-text-lengths", out var lengths)
            ? [.. lengths.Split(',').Select(item => int.Parse(item, System.Globalization.CultureInfo.InvariantCulture))]
            : null;
        return new TestWorkerOptions(pipeName, scenario, recordPath, pipeRecordPath, outputTextLength,
            protocolVersionOffset, sdkFaultOutput, outputTextLengths);
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
            "unexpected-kind-on-execute" => TestWorkerScenario.UnexpectedKindOnExecute,
            "mismatched-correlation-on-execute" => TestWorkerScenario.MismatchedCorrelationOnExecute,
            "truncated-frame-on-execute" => TestWorkerScenario.TruncatedFrameOnExecute,
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
