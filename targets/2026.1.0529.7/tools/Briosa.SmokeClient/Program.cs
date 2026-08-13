using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using Briosa;
using Google.Protobuf.Reflection;
using Grpc.Core;
using Grpc.Net.Client;
using TargetProtocol = Briosa;

return await SmokeClientProgram.RunAsync(args).ConfigureAwait(false);

internal static class SmokeClientProgram
{
    private const string ExpectedSpatialAnalyzerTarget = "2026.1.0529.7";
    private const string ExpectedProtocolPackage = "briosa";
    private const string ExpectedFileOperation =
        "/briosa.FileOperations/GetWorkingDirectory";
    private const string ErrorTrailerName = "briosa-operation-error-bin";
    private static readonly IReadOnlyList<ServiceDescriptor> MpServices =
        [
            TargetProtocol.AnalysisOperations.Descriptor,
            TargetProtocol.ConstructionOperations.Descriptor,
            TargetProtocol.FileOperations.Descriptor,
            TargetProtocol.UtilityOperations.Descriptor
        ];
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
            var sdkLifecycleClient = new TargetProtocol.SpatialAnalyzerSdkLifecycle
                .SpatialAnalyzerSdkLifecycleClient(channel);
            var fileClient = new TargetProtocol.FileOperations.FileOperationsClient(channel);
            var analysisClient =
                new TargetProtocol.AnalysisOperations.AnalysisOperationsClient(channel);
            var constructionClient =
                new TargetProtocol.ConstructionOperations.ConstructionOperationsClient(
                    channel);
            var utilityClient =
                new TargetProtocol.UtilityOperations.UtilityOperationsClient(channel);
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
                options.ExpectOperation);
            var outcome = await ExecuteScenario(
                    options,
                    channel,
                    sdkLifecycleClient,
                    fileClient,
                    analysisClient,
                    constructionClient,
                    utilityClient,
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
            serverInfo.Version.ProtocolPackage != ExpectedProtocolPackage)
        {
            throw new SmokeFailureException("server-target-identity-mismatch");
        }

        if (capabilities.SpatialAnalyzerTarget != ExpectedSpatialAnalyzerTarget ||
            capabilities.ProtocolPackage != ExpectedProtocolPackage)
        {
            throw new SmokeFailureException("capability-target-identity-mismatch");
        }

        var expectedOperations = MpServices
            .SelectMany(service => service.Methods.Select(method =>
                $"/{service.FullName}/{method.Name}"))
            .Where(method => expectOperation || method != ExpectedFileOperation)
            .Order(StringComparer.Ordinal);
        var advertisedOperations = capabilities.Operations
            .Select(operation => operation.FullyQualifiedMethod)
            .Order(StringComparer.Ordinal);
        if (!expectedOperations.SequenceEqual(
                advertisedOperations,
                StringComparer.Ordinal))
        {
            throw new SmokeFailureException("operation-policy-capability-mismatch");
        }
    }

    private static async Task<ScenarioOutcome> ExecuteScenario(
        SmokeOptions options,
        GrpcChannel channel,
        TargetProtocol.SpatialAnalyzerSdkLifecycle
            .SpatialAnalyzerSdkLifecycleClient sdkLifecycleClient,
        TargetProtocol.FileOperations.FileOperationsClient fileClient,
        TargetProtocol.AnalysisOperations.AnalysisOperationsClient analysisClient,
        TargetProtocol.ConstructionOperations.ConstructionOperationsClient
            constructionClient,
        TargetProtocol.UtilityOperations.UtilityOperationsClient utilityClient,
        GetServerInfoResponse serverInfo,
        CancellationToken cancellationToken) =>
        options.Scenario switch
        {
            SmokeScenario.Ready => await ExecuteReady(
                fileClient,
                analysisClient,
                constructionClient,
                utilityClient,
                serverInfo,
                options.Timeout,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.Unavailable => await ExecuteUnavailable(
                fileClient,
                serverInfo,
                options.Timeout,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.MpFailure => await ExecuteExpectedFailure(
                fileClient,
                serverInfo,
                options.Timeout,
                StatusCode.FailedPrecondition,
                OperationFailureKind.MpFailure,
                OutputRetrievalState.NotAttempted,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.OutputFailure => await ExecuteExpectedFailure(
                fileClient,
                serverInfo,
                options.Timeout,
                StatusCode.DataLoss,
                OperationFailureKind.OutputRetrievalFailure,
                OutputRetrievalState.Failed,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.Deadline => await ExecuteInterrupted(
                fileClient,
                serverInfo,
                useDeadline: true,
                options.Timeout,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.Cancellation => await ExecuteInterrupted(
                fileClient,
                serverInfo,
                useDeadline: false,
                options.Timeout,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.WatchdogRecovery => await ExecuteWatchdogRecovery(
                sdkLifecycleClient,
                fileClient,
                serverInfo,
                options.Timeout,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.PolicyDenied => await ExecutePolicyDenied(
                fileClient,
                serverInfo,
                options.Timeout,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.UnsupportedService => await ExecuteUnsupportedService(
                channel,
                serverInfo,
                options.Timeout,
                cancellationToken).ConfigureAwait(false),
            _ => throw new SmokeFailureException("unsupported-smoke-scenario")
        };

    private static async Task<ScenarioOutcome> ExecuteReady(
        TargetProtocol.FileOperations.FileOperationsClient fileClient,
        TargetProtocol.AnalysisOperations.AnalysisOperationsClient analysisClient,
        TargetProtocol.ConstructionOperations.ConstructionOperationsClient
            constructionClient,
        TargetProtocol.UtilityOperations.UtilityOperationsClient utilityClient,
        GetServerInfoResponse serverInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        RequireReady(serverInfo);
        await RequireSuccessfulOperation(fileClient, timeout, cancellationToken)
            .ConfigureAwait(false);
        await RequireSuccessfulCollectionEnumeration(
                analysisClient,
                timeout,
                cancellationToken)
            .ConfigureAwait(false);
        await RequireSuccessfulActiveContext(
                constructionClient,
                utilityClient,
                timeout,
                cancellationToken)
            .ConfigureAwait(false);
        return new ScenarioOutcome(
            OperationSucceeded: true,
            StatusCode.OK,
            TypedErrorObserved: false,
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
            StatusCode.Unavailable,
            TypedErrorObserved: true,
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
            error.ExecutionDisposition != ExecutionDisposition.NotStarted ||
            error.RecoveryGuidance != RecoveryGuidance.None ||
            error.ReplayGuidance != ReplayGuidance.DoNotReplay ||
            error.ReplaySafety != ReplaySafety.Safe ||
            error.MpExecution is not null)
        {
            throw new SmokeFailureException("unexpected-policy-denial-shape");
        }

        return new ScenarioOutcome(
            OperationSucceeded: false,
            StatusCode.PermissionDenied,
            TypedErrorObserved: true,
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
            expectedStatus,
            TypedErrorObserved: true,
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
            expectedStatus,
            TypedErrorObserved: false,
            FailureKind: null,
            RecoverySucceeded: true);
    }

    private static async Task<ScenarioOutcome> ExecuteWatchdogRecovery(
        TargetProtocol.SpatialAnalyzerSdkLifecycle
            .SpatialAnalyzerSdkLifecycleClient sdkLifecycleClient,
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
            error.ExecutionDisposition != ExecutionDisposition.StartedOutcomeUnknown ||
            error.RecoveryGuidance != RecoveryGuidance.WorkerReplacement ||
            error.ReplayGuidance != ReplayGuidance.MayReplay ||
            error.ReplaySafety != ReplaySafety.Safe)
        {
            throw new SmokeFailureException("unexpected-watchdog-failure-shape");
        }

        var state = await sdkLifecycleClient.GetSpatialAnalyzerSdkStateAsync(
                new TargetProtocol.GetSpatialAnalyzerSdkStateRequest(),
                deadline: DateTime.UtcNow.Add(timeout),
                cancellationToken: cancellationToken)
            .ResponseAsync.ConfigureAwait(false);
        if (state.State.SdkState != TargetProtocol.SpatialAnalyzerSdkState.Faulted ||
            !state.State.HasSdkGeneration)
        {
            throw new SmokeFailureException("watchdog-sdk-state-not-faulted");
        }

        var recovered = await sdkLifecycleClient.RecoverSpatialAnalyzerSdkAsync(
                new TargetProtocol.RecoverSpatialAnalyzerSdkRequest
                {
                    ExpectedSdkGeneration = state.State.SdkGeneration,
                    Mode = TargetProtocol.SpatialAnalyzerSdkRecoveryMode.ReplaceWithoutReplay
                },
                deadline: DateTime.UtcNow.Add(timeout),
                cancellationToken: cancellationToken)
            .ResponseAsync.ConfigureAwait(false);
        var connected = await sdkLifecycleClient.ConnectToSpatialAnalyzerAsync(
                new TargetProtocol.ConnectToSpatialAnalyzerRequest
                {
                    ExpectedSdkGeneration = recovered.State.SdkGeneration
                },
                deadline: DateTime.UtcNow.Add(timeout),
                cancellationToken: cancellationToken)
            .ResponseAsync.ConfigureAwait(false);
        if (!connected.State.ReadyForMp)
        {
            throw new SmokeFailureException("watchdog-sdk-recovery-not-ready");
        }

        await RequireSuccessfulOperation(client, timeout, cancellationToken)
            .ConfigureAwait(false);
        return new ScenarioOutcome(
            OperationSucceeded: false,
            StatusCode.Unavailable,
            TypedErrorObserved: true,
            error.Kind.ToString(),
            RecoverySucceeded: true);
    }

    private static async Task<ScenarioOutcome> ExecuteUnsupportedService(
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
            "briosa.UnsupportedOperations",
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
            throw new SmokeFailureException("unsupported-service-unexpectedly-succeeded");
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.Unimplemented)
        {
        }

        return new ScenarioOutcome(
            OperationSucceeded: false,
            StatusCode.Unimplemented,
            TypedErrorObserved: false,
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

    private static async Task RequireSuccessfulCollectionEnumeration(
        TargetProtocol.AnalysisOperations.AnalysisOperationsClient client,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var countResult = await client.GetNumberOfCollectionsAsync(
                new TargetProtocol.GetNumberOfCollectionsRequest(),
                deadline: DateTime.UtcNow.Add(timeout),
                cancellationToken: cancellationToken)
            .ResponseAsync.ConfigureAwait(false);
        if (!countResult.HasTotalCount ||
            countResult.TotalCount <= 0 ||
            countResult.Execution is null ||
            countResult.Execution.State != MpExecutionState.Succeeded ||
            countResult.Execution.OutputRetrievals.Count != 1 ||
            countResult.Execution.OutputRetrievals[0].State !=
                OutputRetrievalState.Retrieved)
        {
            throw new SmokeFailureException(
                "unexpected-collection-count-success-shape");
        }

        var nameResult = await client.GetIThCollectionNameAsync(
                new TargetProtocol.GetIThCollectionNameRequest
                {
                    CollectionIndex = 0
                },
                deadline: DateTime.UtcNow.Add(timeout),
                cancellationToken: cancellationToken)
            .ResponseAsync.ConfigureAwait(false);
        if (!nameResult.HasResultantName ||
            nameResult.Execution is null ||
            nameResult.Execution.State != MpExecutionState.Succeeded ||
            nameResult.Execution.OutputRetrievals.Count != 1 ||
            nameResult.Execution.OutputRetrievals[0].State !=
                OutputRetrievalState.Retrieved)
        {
            throw new SmokeFailureException(
                "unexpected-collection-name-success-shape");
        }
    }

    private static async Task RequireSuccessfulActiveContext(
        TargetProtocol.ConstructionOperations.ConstructionOperationsClient
            constructionClient,
        TargetProtocol.UtilityOperations.UtilityOperationsClient utilityClient,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        TargetProtocol.GetActiveCollectionNameResult collectionResult;
        try
        {
            collectionResult = await constructionClient.GetActiveCollectionNameAsync(
                    new TargetProtocol.GetActiveCollectionNameRequest(),
                    deadline: DateTime.UtcNow.Add(timeout),
                    cancellationToken: cancellationToken)
                .ResponseAsync.ConfigureAwait(false);
        }
        catch (RpcException exception)
        {
            throw UnexpectedOperationFailure("active-collection", exception);
        }
        if (!collectionResult.HasCurrentlyActiveCollectionName)
        {
            throw new SmokeFailureException(
                "unexpected-active-collection-success-shape");
        }
        RequireSuccessfulRetrievals(
            collectionResult.Execution,
            expectedCount: 1,
            "unexpected-active-collection-success-shape");

        TargetProtocol.GetActiveUnitsResult unitsResult;
        try
        {
            unitsResult = await utilityClient.GetActiveUnitsAsync(
                    new TargetProtocol.GetActiveUnitsRequest(),
                    deadline: DateTime.UtcNow.Add(timeout),
                    cancellationToken: cancellationToken)
                .ResponseAsync.ConfigureAwait(false);
        }
        catch (RpcException exception)
        {
            throw UnexpectedOperationFailure("active-units", exception);
        }
        if (!unitsResult.HasLength ||
            !unitsResult.HasAngular ||
            !unitsResult.HasTemperature)
        {
            throw new SmokeFailureException("unexpected-active-units-success-shape");
        }
        RequireSuccessfulRetrievals(
            unitsResult.Execution,
            expectedCount: 3,
            "unexpected-active-units-success-shape");

        TargetProtocol.GetWorkingFramePropertiesResult frameResult;
        try
        {
            frameResult = await utilityClient.GetWorkingFramePropertiesAsync(
                    new TargetProtocol.GetWorkingFramePropertiesRequest(),
                    deadline: DateTime.UtcNow.Add(timeout),
                    cancellationToken: cancellationToken)
                .ResponseAsync.ConfigureAwait(false);
        }
        catch (RpcException exception)
        {
            throw UnexpectedOperationFailure("working-frame", exception);
        }
        if (!frameResult.HasFrameName ||
            !frameResult.HasCollectionName ||
            frameResult.WorkingFrame is null ||
            !frameResult.WorkingFrame.HasCollectionName ||
            !frameResult.WorkingFrame.HasObjectName ||
            frameResult.WorkingFrame.ObjectType == TargetProtocol.ObjectType.Unspecified)
        {
            throw new SmokeFailureException("unexpected-working-frame-success-shape");
        }
        RequireSuccessfulRetrievals(
            frameResult.Execution,
            expectedCount: 3,
            "unexpected-working-frame-success-shape");
    }

    private static void RequireSuccessfulRetrievals(
        MpExecutionDetails? execution,
        int expectedCount,
        string diagnosticCode)
    {
        if (execution is null ||
            execution.State != MpExecutionState.Succeeded ||
            execution.OutputRetrievals.Count != expectedCount ||
            execution.OutputRetrievals.Any(retrieval =>
                retrieval.State != OutputRetrievalState.Retrieved))
        {
            throw new SmokeFailureException(diagnosticCode);
        }
    }

    private static SmokeFailureException UnexpectedOperationFailure(
        string operation,
        RpcException exception)
    {
        var detail = exception.Trailers.SingleOrDefault(
            entry => entry.Key == ErrorTrailerName);
        if (detail is not null)
        {
            try
            {
                var diagnosticCode = OperationError.Parser
                    .ParseFrom(detail.ValueBytes)
                    .DiagnosticCode;
                if (Regex.IsMatch(diagnosticCode, "^[a-z0-9-]{1,128}$"))
                {
                    return new SmokeFailureException(
                        $"{operation}-{diagnosticCode}");
                }
            }
            catch (Google.Protobuf.InvalidProtocolBufferException)
            {
            }
        }

        return new SmokeFailureException(
            $"{operation}-rpc-{exception.StatusCode}");
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
                grpc_status = ToCanonicalStatusName(outcome.GrpcStatus),
                operation_succeeded = outcome.OperationSucceeded,
                typed_error_observed = outcome.TypedErrorObserved,
                failure_kind = outcome.FailureKind,
                recovery_succeeded = outcome.RecoverySucceeded
            },
            JsonOptions));

    private static string ToCanonicalStatusName(StatusCode status) =>
        Regex.Replace(status.ToString(), "([a-z0-9])([A-Z])", "$1_$2")
            .ToUpperInvariant();

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
        UnsupportedService,
        PolicyDenied
    }

    private sealed record SmokeOptions(
        Uri Address,
        SmokeScenario Scenario,
        string ScenarioName,
        bool ExpectOperation,
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
                "unsupported-service" => SmokeScenario.UnsupportedService,
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
                scenario != SmokeScenario.PolicyDenied,
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
        StatusCode GrpcStatus,
        bool TypedErrorObserved,
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
