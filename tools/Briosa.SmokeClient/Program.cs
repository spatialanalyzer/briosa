using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    private const string GetCollectionCountOperation =
        "/briosa.sa.v2026_1_0529_7.v1alpha1.CollectionOperations/GetCollectionCount";
    private const string GetCollectionNameByIndexOperation =
        "/briosa.sa.v2026_1_0529_7.v1alpha1.CollectionOperations/GetCollectionNameByIndex";
    private const string ConstructPointInWorkingCoordinatesOperation =
        "/briosa.sa.v2026_1_0529_7.v1alpha1.CollectionOperations/ConstructPointInWorkingCoordinates";
    private const string DeletePointsOperation =
        "/briosa.sa.v2026_1_0529_7.v1alpha1.CollectionOperations/DeletePoints";
    private const string RenamePointOperation =
        "/briosa.sa.v2026_1_0529_7.v1alpha1.CollectionOperations/RenamePoint";
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
            var collectionClient =
                new TargetProtocol.CollectionOperations.CollectionOperationsClient(channel);
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
                options.ExpectedFullyQualifiedMethod,
                options.ExpectOperation);
            var outcome = await ExecuteScenario(
                    options,
                    channel,
                    fileClient,
                    collectionClient,
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
        string expectedFullyQualifiedMethod,
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
            operation.FullyQualifiedMethod == expectedFullyQualifiedMethod);
        if (operationAdvertised != expectOperation)
        {
            throw new SmokeFailureException("operation-policy-capability-mismatch");
        }
    }

    private static async Task<ScenarioOutcome> ExecuteScenario(
        SmokeOptions options,
        GrpcChannel channel,
        TargetProtocol.FileOperations.FileOperationsClient client,
        TargetProtocol.CollectionOperations.CollectionOperationsClient collectionClient,
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
            SmokeScenario.CollectionCountReady => await ExecuteCollectionCountReady(
                collectionClient,
                serverInfo,
                options.Timeout,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.CollectionNameMissingIndex =>
                await ExecuteCollectionNameMissingIndex(
                    collectionClient,
                    serverInfo,
                    options.Timeout,
                    cancellationToken).ConfigureAwait(false),
            SmokeScenario.CollectionCountPolicyDenied =>
                await ExecuteCollectionCountPolicyDenied(
                    collectionClient,
                    serverInfo,
                    options.Timeout,
                    cancellationToken).ConfigureAwait(false),
            SmokeScenario.CollectionCountMpFailure =>
                await ExecuteCollectionCountMpFailure(
                    collectionClient,
                    serverInfo,
                    options.Timeout,
                    cancellationToken).ConfigureAwait(false),
            SmokeScenario.ConstructPointReady => await ExecuteConstructPointReady(
                collectionClient,
                serverInfo,
                options.Timeout,
                cancellationToken).ConfigureAwait(false),
            SmokeScenario.ConstructPointMissingCoordinates =>
                await ExecuteConstructPointMissingCoordinates(
                    collectionClient,
                    serverInfo,
                    options.Timeout,
                    cancellationToken).ConfigureAwait(false),
            SmokeScenario.DeletePointsPolicyDenied =>
                await ExecuteDeletePointsPolicyDenied(
                    collectionClient,
                    serverInfo,
                    options.Timeout,
                    cancellationToken).ConfigureAwait(false),
            SmokeScenario.RenamePointMpFailure => await ExecuteRenamePointMpFailure(
                collectionClient,
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

    private static async Task<ScenarioOutcome> ExecuteCollectionCountReady(
        TargetProtocol.CollectionOperations.CollectionOperationsClient client,
        GetServerInfoResponse serverInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        RequireReady(serverInfo);
        var result = await client.GetCollectionCountAsync(
                new TargetProtocol.GetCollectionCountRequest(),
                deadline: DateTime.UtcNow.Add(timeout),
                cancellationToken: cancellationToken)
            .ResponseAsync.ConfigureAwait(false);
        if (!result.HasCollectionCount ||
            result.Execution is null ||
            result.Execution.State != MpExecutionState.Succeeded ||
            result.Execution.OutputRetrievals.Count != 1 ||
            result.Execution.OutputRetrievals[0].State !=
                OutputRetrievalState.Retrieved)
        {
            throw new SmokeFailureException("unexpected-collection-count-success-shape");
        }

        return new ScenarioOutcome(
            OperationSucceeded: true,
            StatusCode.OK,
            TypedErrorObserved: false,
            FailureKind: null,
            RecoverySucceeded: false);
    }

    private static async Task<ScenarioOutcome> ExecuteCollectionNameMissingIndex(
        TargetProtocol.CollectionOperations.CollectionOperationsClient client,
        GetServerInfoResponse serverInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        RequireReady(serverInfo);
        OperationError error;
        try
        {
            _ = await client.GetCollectionNameByIndexAsync(
                    new TargetProtocol.GetCollectionNameByIndexRequest(),
                    deadline: DateTime.UtcNow.Add(timeout),
                    cancellationToken: cancellationToken)
                .ResponseAsync.ConfigureAwait(false);
            throw new SmokeFailureException("missing-index-unexpectedly-succeeded");
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.InvalidArgument)
        {
            error = ReadOperationError(exception);
        }

        RequireNotStartedError(error, OperationFailureKind.Validation);
        return new ScenarioOutcome(
            OperationSucceeded: false,
            StatusCode.InvalidArgument,
            TypedErrorObserved: true,
            error.Kind.ToString(),
            RecoverySucceeded: false);
    }

    private static async Task<ScenarioOutcome> ExecuteCollectionCountPolicyDenied(
        TargetProtocol.CollectionOperations.CollectionOperationsClient client,
        GetServerInfoResponse serverInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        RequireReady(serverInfo);
        var error = await RequireCollectionCountFailure(
                client,
                timeout,
                StatusCode.PermissionDenied,
                cancellationToken)
            .ConfigureAwait(false);
        RequireNotStartedError(error, OperationFailureKind.PolicyDenied);
        return new ScenarioOutcome(
            OperationSucceeded: false,
            StatusCode.PermissionDenied,
            TypedErrorObserved: true,
            error.Kind.ToString(),
            RecoverySucceeded: false);
    }

    private static async Task<ScenarioOutcome> ExecuteCollectionCountMpFailure(
        TargetProtocol.CollectionOperations.CollectionOperationsClient client,
        GetServerInfoResponse serverInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        RequireReady(serverInfo);
        var error = await RequireCollectionCountFailure(
                client,
                timeout,
                StatusCode.FailedPrecondition,
                cancellationToken)
            .ConfigureAwait(false);
        if (error.Kind != OperationFailureKind.MpFailure ||
            error.MpExecution is null ||
            error.MpExecution.OutputRetrievals.Count != 1 ||
            error.MpExecution.OutputRetrievals[0].State !=
                OutputRetrievalState.NotAttempted)
        {
            throw new SmokeFailureException("unexpected-collection-count-mp-failure-shape");
        }

        return new ScenarioOutcome(
            OperationSucceeded: false,
            StatusCode.FailedPrecondition,
            TypedErrorObserved: true,
            error.Kind.ToString(),
            RecoverySucceeded: false);
    }

    private static async Task<ScenarioOutcome> ExecuteConstructPointReady(
        TargetProtocol.CollectionOperations.CollectionOperationsClient client,
        GetServerInfoResponse serverInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        RequireReady(serverInfo);
        var result = await client.ConstructPointInWorkingCoordinatesAsync(
                new TargetProtocol.ConstructPointInWorkingCoordinatesRequest
                {
                    PointName = PointName("Point A"),
                    WorkingCoordinates = new TargetProtocol.Vector3
                    {
                        X = 1,
                        Y = 2,
                        Z = 3
                    }
                },
                deadline: DateTime.UtcNow.Add(timeout),
                cancellationToken: cancellationToken)
            .ResponseAsync.ConfigureAwait(false);
        if (result.Execution is null ||
            result.Execution.State != MpExecutionState.Succeeded ||
            result.Execution.OutputRetrievals.Count != 0)
        {
            throw new SmokeFailureException("unexpected-construct-point-success-shape");
        }

        return new ScenarioOutcome(true, StatusCode.OK, false, null, false);
    }

    private static async Task<ScenarioOutcome> ExecuteConstructPointMissingCoordinates(
        TargetProtocol.CollectionOperations.CollectionOperationsClient client,
        GetServerInfoResponse serverInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        RequireReady(serverInfo);
        OperationError error;
        try
        {
            _ = await client.ConstructPointInWorkingCoordinatesAsync(
                    new TargetProtocol.ConstructPointInWorkingCoordinatesRequest
                    {
                        PointName = PointName("Point A")
                    },
                    deadline: DateTime.UtcNow.Add(timeout),
                    cancellationToken: cancellationToken)
                .ResponseAsync.ConfigureAwait(false);
            throw new SmokeFailureException("missing-coordinates-unexpectedly-succeeded");
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.InvalidArgument)
        {
            error = ReadOperationError(exception);
        }

        RequireNotStartedError(
            error,
            OperationFailureKind.Validation,
            ReplaySafety.Unknown);
        return new ScenarioOutcome(false, StatusCode.InvalidArgument, true, error.Kind.ToString(), false);
    }

    private static async Task<ScenarioOutcome> ExecuteDeletePointsPolicyDenied(
        TargetProtocol.CollectionOperations.CollectionOperationsClient client,
        GetServerInfoResponse serverInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        RequireReady(serverInfo);
        OperationError error;
        try
        {
            var request = new TargetProtocol.DeletePointsRequest();
            request.PointNames = new TargetProtocol.PointNameList();
            request.PointNames.Values.Add(PointName("Point A"));
            _ = await client.DeletePointsAsync(
                    request,
                    deadline: DateTime.UtcNow.Add(timeout),
                    cancellationToken: cancellationToken)
                .ResponseAsync.ConfigureAwait(false);
            throw new SmokeFailureException("delete-points-policy-unexpectedly-succeeded");
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.PermissionDenied)
        {
            error = ReadOperationError(exception);
        }

        RequireNotStartedError(
            error,
            OperationFailureKind.PolicyDenied,
            ReplaySafety.Unknown);
        return new ScenarioOutcome(false, StatusCode.PermissionDenied, true, error.Kind.ToString(), false);
    }

    private static async Task<ScenarioOutcome> ExecuteRenamePointMpFailure(
        TargetProtocol.CollectionOperations.CollectionOperationsClient client,
        GetServerInfoResponse serverInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        RequireReady(serverInfo);
        OperationError error;
        try
        {
            _ = await client.RenamePointAsync(
                    new TargetProtocol.RenamePointRequest
                    {
                        OriginalPointName = PointName("Point A"),
                        NewPointName = PointName("Point B")
                    },
                    deadline: DateTime.UtcNow.Add(timeout),
                    cancellationToken: cancellationToken)
                .ResponseAsync.ConfigureAwait(false);
            throw new SmokeFailureException("rename-point-mp-failure-unexpectedly-succeeded");
        }
        catch (RpcException exception) when (
            exception.StatusCode == StatusCode.FailedPrecondition)
        {
            error = ReadOperationError(exception);
        }

        if (error.Kind != OperationFailureKind.MpFailure ||
            error.ExecutionDisposition != ExecutionDisposition.Completed ||
            error.ReplaySafety != ReplaySafety.Unknown ||
            error.ReplayGuidance != ReplayGuidance.DoNotReplay ||
            error.MpExecution is null ||
            error.MpExecution.OutputRetrievals.Count != 0)
        {
            throw new SmokeFailureException("unexpected-rename-point-mp-failure-shape");
        }

        return new ScenarioOutcome(false, StatusCode.FailedPrecondition, true, error.Kind.ToString(), false);
    }

    private static TargetProtocol.PointName PointName(string targetName) => new()
    {
        CollectionName = "Portable Collection",
        GroupName = "Portable Group",
        TargetName = targetName
    };

    private static void RequireNotStartedError(
        OperationError error,
        OperationFailureKind expectedKind,
        ReplaySafety expectedReplaySafety = ReplaySafety.Safe)
    {
        if (error.Kind != expectedKind ||
            error.ExecutionDisposition != ExecutionDisposition.NotStarted ||
            error.RecoveryGuidance != RecoveryGuidance.None ||
            error.ReplayGuidance != ReplayGuidance.DoNotReplay ||
            error.ReplaySafety != expectedReplaySafety ||
            error.MpExecution is not null)
        {
            throw new SmokeFailureException("unexpected-not-started-error-shape");
        }
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

        await RequireSuccessfulOperation(client, timeout, cancellationToken)
            .ConfigureAwait(false);
        return new ScenarioOutcome(
            OperationSucceeded: false,
            StatusCode.Unavailable,
            TypedErrorObserved: true,
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
            return ReadOperationError(exception);
        }
    }

    private static async Task<OperationError> RequireCollectionCountFailure(
        TargetProtocol.CollectionOperations.CollectionOperationsClient client,
        TimeSpan timeout,
        StatusCode expectedStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await client.GetCollectionCountAsync(
                    new TargetProtocol.GetCollectionCountRequest(),
                    deadline: DateTime.UtcNow.Add(timeout),
                    cancellationToken: cancellationToken)
                .ResponseAsync.ConfigureAwait(false);
            throw new SmokeFailureException("operation-unexpectedly-succeeded");
        }
        catch (RpcException exception) when (exception.StatusCode == expectedStatus)
        {
            return ReadOperationError(exception);
        }
    }

    private static OperationError ReadOperationError(RpcException exception)
    {
        var detail = exception.Trailers.SingleOrDefault(
            entry => entry.Key == ErrorTrailerName);
        if (detail is null)
        {
            throw new SmokeFailureException("operation-error-detail-missing");
        }

        return OperationError.Parser.ParseFrom(detail.ValueBytes);
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
        UnsupportedVersion,
        PolicyDenied,
        CollectionCountReady,
        CollectionNameMissingIndex,
        CollectionCountPolicyDenied,
        CollectionCountMpFailure,
        ConstructPointReady,
        ConstructPointMissingCoordinates,
        DeletePointsPolicyDenied,
        RenamePointMpFailure
    }

    private sealed record SmokeOptions(
        Uri Address,
        SmokeScenario Scenario,
        string ScenarioName,
        bool ExpectOperation,
        string ExpectedFullyQualifiedMethod,
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
                "collection-count-ready" => SmokeScenario.CollectionCountReady,
                "collection-name-missing-index" =>
                    SmokeScenario.CollectionNameMissingIndex,
                "collection-count-policy-denied" =>
                    SmokeScenario.CollectionCountPolicyDenied,
                "collection-count-mp-failure" =>
                    SmokeScenario.CollectionCountMpFailure,
                "construct-point-ready" => SmokeScenario.ConstructPointReady,
                "construct-point-missing-coordinates" =>
                    SmokeScenario.ConstructPointMissingCoordinates,
                "delete-points-policy-denied" =>
                    SmokeScenario.DeletePointsPolicyDenied,
                "rename-point-mp-failure" => SmokeScenario.RenamePointMpFailure,
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

            var fixturePath = GetArgument(arguments, "--fixture");
            var fixtureExpectation = fixturePath is null
                ? DefaultFixtureExpectation(scenario)
                : ReadFixtureExpectation(fixturePath, scenarioName);

            return new SmokeOptions(
                address,
                scenario,
                scenarioName,
                fixtureExpectation.OperationAdvertised,
                fixtureExpectation.FullyQualifiedMethod,
                TimeSpan.FromSeconds(timeoutSeconds));
        }

        private static FixtureExpectation DefaultFixtureExpectation(
            SmokeScenario scenario) =>
            scenario switch
            {
                SmokeScenario.CollectionCountReady or
                SmokeScenario.CollectionCountMpFailure => new(
                    OperationAdvertised: true,
                    GetCollectionCountOperation),
                SmokeScenario.CollectionCountPolicyDenied => new(
                    OperationAdvertised: false,
                    GetCollectionCountOperation),
                SmokeScenario.CollectionNameMissingIndex => new(
                    OperationAdvertised: true,
                    GetCollectionNameByIndexOperation),
                SmokeScenario.ConstructPointReady or
                SmokeScenario.ConstructPointMissingCoordinates => new(
                    OperationAdvertised: true,
                    ConstructPointInWorkingCoordinatesOperation),
                SmokeScenario.DeletePointsPolicyDenied => new(
                    OperationAdvertised: false,
                    DeletePointsOperation),
                SmokeScenario.RenamePointMpFailure => new(
                    OperationAdvertised: true,
                    RenamePointOperation),
                _ => new FixtureExpectation(
                    scenario != SmokeScenario.PolicyDenied,
                    ExpectedOperation)
            };

        private static FixtureExpectation ReadFixtureExpectation(
            string fixturePath,
            string scenarioName)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(fixturePath));
            var root = document.RootElement;
            if (root.GetProperty("schema_version").GetInt32() != 1 ||
                root.GetProperty("error_trailer").GetString() != ErrorTrailerName ||
                root.GetProperty("fixture_set_id").GetString() is not (
                    "briosa.client.live.v1" or
                    "briosa.client.wave1-read-only.v1" or
                    "briosa.client.wave2-point-lifecycle.v1"))
            {
                throw new SmokeFailureException("conformance-fixture-identity-mismatch");
            }

            foreach (var scenario in root.GetProperty("scenarios").EnumerateArray())
            {
                if (scenario.GetProperty("id").GetString() == scenarioName)
                {
                    var operationId = scenario.TryGetProperty(
                        "operation_id",
                        out var scenarioOperationId)
                        ? scenarioOperationId.GetString()
                        : root.GetProperty("operation_id").GetString();
                    return new FixtureExpectation(
                        scenario.GetProperty("expected")
                            .GetProperty("operation_advertised")
                            .GetBoolean(),
                        ToFullyQualifiedMethod(operationId));
                }
            }

            throw new SmokeFailureException("conformance-scenario-missing");
        }

        private static string ToFullyQualifiedMethod(string? operationId) =>
            operationId switch
            {
                "file_operations.get_working_directory" => ExpectedOperation,
                "collection_operations.get_collection_count" =>
                    GetCollectionCountOperation,
                "collection_operations.get_collection_name_by_index" =>
                    GetCollectionNameByIndexOperation,
                "collection_operations.construct_point_in_working_coordinates" =>
                    ConstructPointInWorkingCoordinatesOperation,
                "collection_operations.delete_points" => DeletePointsOperation,
                "collection_operations.rename_point" => RenamePointOperation,
                _ => throw new SmokeFailureException(
                    "conformance-operation-identity-mismatch")
            };

        private static string? GetArgument(string[] arguments, string name)
        {
            var index = Array.IndexOf(arguments, name);
            return index >= 0 && index + 1 < arguments.Length
                ? arguments[index + 1]
                : null;
        }
    }

    private sealed record FixtureExpectation(
        bool OperationAdvertised,
        string FullyQualifiedMethod);

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
