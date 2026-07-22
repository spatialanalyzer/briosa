using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Briosa.Core.V1Alpha1;
using Grpc.Core;
using Grpc.Net.Client;
using TargetProtocol = Briosa.Sa.V2026_1_0529_7.V1Alpha1;

return await SmokeClientProgram.RunAsync(args).ConfigureAwait(false);

internal static class SmokeClientProgram
{
    private const string ExpectedSpatialAnalyzerTarget = "2026.1.0529.7";
    private const string ExpectedTargetProtocolPackage =
        "briosa.sa.v2026_1_0529_7.v1alpha1";
    private const string ExpectedOperation =
        "/briosa.sa.v2026_1_0529_7.v1alpha1.FileOperations/GetWorkingDirectory";
    private const string ErrorTrailerName = "briosa-operation-error-bin";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The external probe must emit only a stable, value-free failure code.")]
    public static async Task<int> RunAsync(string[] arguments)
    {
        SmokeOptions? options = null;
        try
        {
            options = SmokeOptions.Parse(arguments);
            using var timeout = new CancellationTokenSource(options.Timeout);
            using var channel = GrpcChannel.ForAddress(options.Address);
            var discoveryClient = new DiscoveryService.DiscoveryServiceClient(channel);
            var fileClient = new TargetProtocol.FileOperations.FileOperationsClient(channel);
            var deadline = DateTime.UtcNow.Add(options.Timeout);
            var serverInfo = await discoveryClient.GetServerInfoAsync(
                    new GetServerInfoRequest(),
                    deadline: deadline,
                    cancellationToken: timeout.Token)
                .ResponseAsync.ConfigureAwait(false);
            var capabilities = await discoveryClient.ListCapabilitiesAsync(
                    new ListCapabilitiesRequest(),
                    deadline: deadline,
                    cancellationToken: timeout.Token)
                .ResponseAsync.ConfigureAwait(false);

            ValidateIdentity(
                serverInfo,
                capabilities,
                expectOperation: options.Scenario != SmokeScenario.PolicyDenied);
            var outcome = await ExecuteScenario(
                    options,
                    channel,
                    fileClient,
                    serverInfo,
                    timeout.Token)
                .ConfigureAwait(false);
            WriteReport(options, serverInfo, outcome);
            return 0;
        }
        catch (SmokeFailureException exception)
        {
            WriteFailure(options, exception.DiagnosticCode);
            return 1;
        }
        catch (RpcException exception)
        {
            WriteFailure(options, $"unexpected-rpc-{exception.StatusCode}");
            return 1;
        }
        catch (Exception)
        {
            WriteFailure(options, "smoke-client-unexpected-failure");
            return 1;
        }
    }

    private static void ValidateIdentity(
        GetServerInfoResponse serverInfo,
        ListCapabilitiesResponse capabilities,
        bool expectOperation)
    {
        if (serverInfo.Version is null ||
            serverInfo.Version.SpatialAnalyzerTarget != ExpectedSpatialAnalyzerTarget ||
            serverInfo.Version.TargetProtocolPackage != ExpectedTargetProtocolPackage)
        {
            throw new SmokeFailureException("server-target-identity-mismatch");
        }

        if (capabilities.SpatialAnalyzerTarget != ExpectedSpatialAnalyzerTarget ||
            capabilities.TargetProtocolPackage != ExpectedTargetProtocolPackage)
        {
            throw new SmokeFailureException("capability-target-identity-mismatch");
        }

        var operationAdvertised = capabilities.Operations.Any(operation =>
            operation.FullyQualifiedMethod == ExpectedOperation);
        if (operationAdvertised != expectOperation)
        {
            throw new SmokeFailureException("operation-policy-capability-mismatch");
        }
    }

    private static async Task<ScenarioOutcome> ExecuteScenario(
        SmokeOptions options,
        GrpcChannel channel,
        TargetProtocol.FileOperations.FileOperationsClient client,
        GetServerInfoResponse serverInfo,
        CancellationToken cancellationToken) =>
        options.Scenario switch
        {
            SmokeScenario.Ready => await ExecuteReady(
                client,
                serverInfo,
                options.Timeout,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.Unavailable => await ExecuteUnavailable(
                client,
                serverInfo,
                options.Timeout,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.MpFailure => await ExecuteExpectedFailure(
                client,
                serverInfo,
                options.Timeout,
                StatusCode.FailedPrecondition,
                OperationFailureKind.MpFailure,
                OutputRetrievalState.NotAttempted,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.OutputFailure => await ExecuteExpectedFailure(
                client,
                serverInfo,
                options.Timeout,
                StatusCode.DataLoss,
                OperationFailureKind.OutputRetrievalFailure,
                OutputRetrievalState.Failed,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.Deadline => await ExecuteInterrupted(
                client,
                serverInfo,
                useDeadline: true,
                options.Timeout,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.Cancellation => await ExecuteInterrupted(
                client,
                serverInfo,
                useDeadline: false,
                options.Timeout,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.WatchdogRecovery => await ExecuteWatchdogRecovery(
                client,
                serverInfo,
                options.Timeout,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.PolicyDenied => await ExecutePolicyDenied(
                client,
                serverInfo,
                options.Timeout,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.UnsupportedVersion => await ExecuteUnsupportedVersion(
                channel,
                serverInfo,
                options.Timeout,
                cancellationToken).ConfigureAwait(false),
            _ => throw new SmokeFailureException("unsupported-smoke-scenario")
        };

    private static async Task<ScenarioOutcome> ExecuteReady(
        TargetProtocol.FileOperations.FileOperationsClient client,
        GetServerInfoResponse serverInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        RequireReady(serverInfo);
        await RequireSuccessfulOperation(client, timeout, cancellationToken)
            .ConfigureAwait(false);
        return new ScenarioOutcome(
            OperationSucceeded: true,
            FailureKind: null,
            RecoverySucceeded: false);
    }

    private static async Task<ScenarioOutcome> ExecuteUnavailable(
        TargetProtocol.FileOperations.FileOperationsClient client,
        GetServerInfoResponse serverInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (serverInfo.ReadyForMp)
        {
            throw new SmokeFailureException("server-unexpectedly-ready");
        }

        var error = await RequireFailure(
                client,
                timeout,
                StatusCode.Unavailable,
                cancellationToken)
            .ConfigureAwait(false);
        if (error.Kind is not (
                OperationFailureKind.SpatialAnalyzerUnavailable or
                OperationFailureKind.WorkerUnavailable))
        {
            throw new SmokeFailureException("unexpected-unavailable-kind");
        }

        return new ScenarioOutcome(
            OperationSucceeded: false,
            error.Kind.ToString(),
            RecoverySucceeded: false);
    }

    private static async Task<ScenarioOutcome> ExecutePolicyDenied(
        TargetProtocol.FileOperations.FileOperationsClient client,
        GetServerInfoResponse serverInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        RequireReady(serverInfo);
        var error = await RequireFailure(
                client,
                timeout,
                StatusCode.PermissionDenied,
                cancellationToken)
            .ConfigureAwait(false);
        if (error.Kind != OperationFailureKind.PolicyDenied ||
            error.RetryGuidance != RetryGuidance.DoNotRetry ||
            error.MpExecution is not null)
        {
            throw new SmokeFailureException("unexpected-policy-denial-shape");
        }

        return new ScenarioOutcome(
            OperationSucceeded: false,
            error.Kind.ToString(),
            RecoverySucceeded: false);
    }

    private static async Task<ScenarioOutcome> ExecuteExpectedFailure(
        TargetProtocol.FileOperations.FileOperationsClient client,
        GetServerInfoResponse serverInfo,
        TimeSpan timeout,
        StatusCode expectedStatus,
        OperationFailureKind expectedKind,
        OutputRetrievalState expectedRetrieval,
        CancellationToken cancellationToken)
    {
        RequireReady(serverInfo);
        var error = await RequireFailure(
                client,
                timeout,
                expectedStatus,
                cancellationToken)
            .ConfigureAwait(false);
        if (error.Kind != expectedKind ||
            error.MpExecution is null ||
            error.MpExecution.OutputRetrievals.Count != 1 ||
            error.MpExecution.OutputRetrievals[0].State != expectedRetrieval)
        {
            throw new SmokeFailureException("unexpected-operation-failure-shape");
        }

        return new ScenarioOutcome(
            OperationSucceeded: false,
            error.Kind.ToString(),
            RecoverySucceeded: false);
    }

    private static async Task<ScenarioOutcome> ExecuteInterrupted(
        TargetProtocol.FileOperations.FileOperationsClient client,
        GetServerInfoResponse serverInfo,
        bool useDeadline,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        RequireReady(serverInfo);
        var expectedStatus = useDeadline
            ? StatusCode.DeadlineExceeded
            : StatusCode.Cancelled;
        try
        {
            if (useDeadline)
            {
                _ = await client.GetWorkingDirectoryAsync(
                        new TargetProtocol.GetWorkingDirectoryRequest(),
                        deadline: DateTime.UtcNow.AddMilliseconds(50),
                        cancellationToken: cancellationToken)
                    .ResponseAsync.ConfigureAwait(false);
            }
            else
            {
                using var callerCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                callerCancellation.CancelAfter(TimeSpan.FromMilliseconds(50));
                _ = await client.GetWorkingDirectoryAsync(
                        new TargetProtocol.GetWorkingDirectoryRequest(),
                        cancellationToken: callerCancellation.Token)
                    .ResponseAsync.ConfigureAwait(false);
            }

            throw new SmokeFailureException("interrupted-call-unexpectedly-succeeded");
        }
        catch (RpcException exception) when (exception.StatusCode == expectedStatus)
        {
        }

        await RequireSuccessfulOperation(client, timeout, cancellationToken)
            .ConfigureAwait(false);
        return new ScenarioOutcome(
            OperationSucceeded: false,
            expectedStatus.ToString(),
            RecoverySucceeded: true);
    }

    private static async Task<ScenarioOutcome> ExecuteWatchdogRecovery(
        TargetProtocol.FileOperations.FileOperationsClient client,
        GetServerInfoResponse serverInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        RequireReady(serverInfo);
        var error = await RequireFailure(
                client,
                timeout,
                StatusCode.Unavailable,
                cancellationToken)
            .ConfigureAwait(false);
        if (error.Kind != OperationFailureKind.WorkerWatchdogTimeout ||
            error.RetryGuidance != RetryGuidance.RetryAfterWorkerReplacement)
        {
            throw new SmokeFailureException("unexpected-watchdog-failure-shape");
        }

        await RequireSuccessfulOperation(client, timeout, cancellationToken)
            .ConfigureAwait(false);
        return new ScenarioOutcome(
            OperationSucceeded: false,
            error.Kind.ToString(),
            RecoverySucceeded: true);
    }

    private static async Task<ScenarioOutcome> ExecuteUnsupportedVersion(
        GrpcChannel channel,
        GetServerInfoResponse serverInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        RequireReady(serverInfo);
        var marshaller = Marshallers.Create(
            static (byte[] value) => value,
            static value => value);
        var method = new Method<byte[], byte[]>(
            MethodType.Unary,
            "briosa.sa.v1900_1_0000_0.v1alpha1.FileOperations",
            "GetWorkingDirectory",
            marshaller,
            marshaller);
        using var call = channel.CreateCallInvoker().AsyncUnaryCall(
            method,
            host: null,
            new CallOptions(
                deadline: DateTime.UtcNow.Add(timeout),
                cancellationToken: cancellationToken),
            []);
        try
        {
            _ = await call.ResponseAsync.ConfigureAwait(false);
            throw new SmokeFailureException("unsupported-version-unexpectedly-succeeded");
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.Unimplemented)
        {
        }

        return new ScenarioOutcome(
            OperationSucceeded: false,
            OperationFailureKind.Unsupported.ToString(),
            RecoverySucceeded: false);
    }

    private static async Task RequireSuccessfulOperation(
        TargetProtocol.FileOperations.FileOperationsClient client,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var result = await client.GetWorkingDirectoryAsync(
                new TargetProtocol.GetWorkingDirectoryRequest(),
                deadline: DateTime.UtcNow.Add(timeout),
                cancellationToken: cancellationToken)
            .ResponseAsync.ConfigureAwait(false);
        if (!result.HasDirectory ||
            result.Execution is null ||
            result.Execution.State != MpExecutionState.Succeeded ||
            result.Execution.OutputRetrievals.Count != 1 ||
            result.Execution.OutputRetrievals[0].State !=
                OutputRetrievalState.Retrieved)
        {
            throw new SmokeFailureException("unexpected-operation-success-shape");
        }
    }

    private static async Task<OperationError> RequireFailure(
        TargetProtocol.FileOperations.FileOperationsClient client,
        TimeSpan timeout,
        StatusCode expectedStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await client.GetWorkingDirectoryAsync(
                    new TargetProtocol.GetWorkingDirectoryRequest(),
                    deadline: DateTime.UtcNow.Add(timeout),
                    cancellationToken: cancellationToken)
                .ResponseAsync.ConfigureAwait(false);
            throw new SmokeFailureException("operation-unexpectedly-succeeded");
        }
        catch (RpcException exception) when (exception.StatusCode == expectedStatus)
        {
            var detail = exception.Trailers.SingleOrDefault(
                entry => entry.Key == ErrorTrailerName);
            if (detail is null)
            {
                throw new SmokeFailureException("operation-error-detail-missing");
            }

            return OperationError.Parser.ParseFrom(detail.ValueBytes);
        }
    }

    private static void RequireReady(GetServerInfoResponse serverInfo)
    {
        if (!serverInfo.ReadyForMp ||
            serverInfo.WorkerState != WorkerRuntimeState.Ready ||
            serverInfo.SpatialAnalyzerConnectionState !=
                SpatialAnalyzerConnectionState.Connected)
        {
            throw new SmokeFailureException("server-not-ready-for-mp");
        }
    }

    private static void WriteReport(
        SmokeOptions options,
        GetServerInfoResponse serverInfo,
        ScenarioOutcome outcome) =>
        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema_version = 1,
                success = true,
                scenario = options.ScenarioName,
                spatial_analyzer_target =
                    serverInfo.Version.SpatialAnalyzerTarget,
                worker_state = serverInfo.WorkerState.ToString(),
                spatial_analyzer_connection_state =
                    serverInfo.SpatialAnalyzerConnectionState.ToString(),
                ready_for_mp = serverInfo.ReadyForMp,
                operation_succeeded = outcome.OperationSucceeded,
                failure_kind = outcome.FailureKind,
                recovery_succeeded = outcome.RecoverySucceeded
            },
            JsonOptions));

    private static void WriteFailure(SmokeOptions? options, string diagnosticCode) =>
        Console.Error.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema_version = 1,
                success = false,
                scenario = options?.ScenarioName,
                diagnostic_code = diagnosticCode
            },
            JsonOptions));

    private enum SmokeScenario
    {
        Ready,
        Unavailable,
        MpFailure,
        OutputFailure,
        Deadline,
        Cancellation,
        WatchdogRecovery,
        UnsupportedVersion,
        PolicyDenied
    }

    private sealed record SmokeOptions(
        Uri Address,
        SmokeScenario Scenario,
        string ScenarioName,
        TimeSpan Timeout)
    {
        public static SmokeOptions Parse(string[] arguments)
        {
            var address = new Uri(
                GetArgument(arguments, "--address") ?? "http://127.0.0.1:50051",
                UriKind.Absolute);
            if (!address.IsLoopback || address.Scheme != Uri.UriSchemeHttp)
            {
                throw new SmokeFailureException("smoke-address-must-be-loopback-http");
            }

            var scenarioName = GetArgument(arguments, "--scenario") ?? "ready";
            var scenario = scenarioName switch
            {
                "ready" => SmokeScenario.Ready,
                "unavailable" => SmokeScenario.Unavailable,
                "mp-failure" => SmokeScenario.MpFailure,
                "output-failure" => SmokeScenario.OutputFailure,
                "deadline" => SmokeScenario.Deadline,
                "cancellation" => SmokeScenario.Cancellation,
                "watchdog-recovery" => SmokeScenario.WatchdogRecovery,
                "unsupported-version" => SmokeScenario.UnsupportedVersion,
                "policy-denied" => SmokeScenario.PolicyDenied,
                _ => throw new SmokeFailureException("unsupported-smoke-scenario")
            };
            var timeoutSecondsText = GetArgument(arguments, "--timeout-seconds");
            var timeoutSeconds = timeoutSecondsText is null
                ? 15
                : int.Parse(
                    timeoutSecondsText,
                    System.Globalization.CultureInfo.InvariantCulture);
            if (timeoutSeconds is < 1 or > 120)
            {
                throw new SmokeFailureException("smoke-timeout-out-of-range");
            }

            return new SmokeOptions(
                address,
                scenario,
                scenarioName,
                TimeSpan.FromSeconds(timeoutSeconds));
        }

        private static string? GetArgument(string[] arguments, string name)
        {
            var index = Array.IndexOf(arguments, name);
            return index >= 0 && index + 1 < arguments.Length
                ? arguments[index + 1]
                : null;
        }
    }

    private sealed record ScenarioOutcome(
        bool OperationSucceeded,
        string? FailureKind,
        bool RecoverySucceeded);

    [SuppressMessage(
        "Design",
        "CA1032:Implement standard exception constructors",
        Justification = "The private exception carries one stable diagnostic code inside this process.")]
    private sealed class SmokeFailureException(string diagnosticCode) : Exception
    {
        public string DiagnosticCode { get; } = diagnosticCode;
    }
}
