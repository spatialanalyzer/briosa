using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Grpc.Core;
using Grpc.Net.Client;

return await LifecycleClientProgram.RunAsync(args).ConfigureAwait(false);

internal static class LifecycleClientProgram
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The external probe emits only a stable, value-free failure code.")]
    public static async Task<int> RunAsync(string[] arguments)
    {
        LifecycleOptions? options = null;
        try
        {
            options = LifecycleOptions.Parse(arguments);
            using var timeout = new CancellationTokenSource(options.Timeout);
            using var channel = GrpcChannel.ForAddress(options.Address);
            var sdk = new global::Briosa.SpatialAnalyzerSdkLifecycle
                .SpatialAnalyzerSdkLifecycleClient(channel);
            var application = new global::Briosa.SpatialAnalyzerLifecycle
                .SpatialAnalyzerLifecycleClient(channel);
            var outcome = options.Scenario switch
            {
                LifecycleScenario.Inert => await Inert(
                    sdk,
                    application,
                    options,
                    timeout.Token).ConfigureAwait(false),
                LifecycleScenario.Owned => await Owned(
                    sdk,
                    application,
                    options,
                    timeout.Token).ConfigureAwait(false),
                LifecycleScenario.External => await External(
                    sdk,
                    application,
                    options,
                    stopSdk: true,
                    timeout.Token).ConfigureAwait(false),
                LifecycleScenario.ExternalConnect => await External(
                    sdk,
                    application,
                    options,
                    stopSdk: false,
                    timeout.Token).ConfigureAwait(false),
                LifecycleScenario.StopSdk => await StopCurrentSdk(
                    sdk,
                    application,
                    options,
                    timeout.Token).ConfigureAwait(false),
                LifecycleScenario.StartSdk => await StartOnly(
                    sdk,
                    application,
                    options,
                    timeout.Token).ConfigureAwait(false),
                LifecycleScenario.SdkLossPrepare => await SdkLossPrepare(
                    sdk,
                    application,
                    options,
                    timeout.Token).ConfigureAwait(false),
                LifecycleScenario.SdkLossRecover => await SdkLossRecover(
                    sdk,
                    application,
                    options,
                    timeout.Token).ConfigureAwait(false),
                _ => throw new LifecycleFailureException(
                    "unsupported-lifecycle-scenario")
            };
            Console.WriteLine(JsonSerializer.Serialize(outcome, JsonOptions));
            return 0;
        }
        catch (LifecycleFailureException exception)
        {
            WriteFailure(options, exception.DiagnosticCode);
            return 1;
        }
        catch (RpcException exception)
        {
            WriteFailure(
                options,
                $"unexpected-rpc-{Canonical(exception.StatusCode.ToString())}");
            return 1;
        }
        catch (Exception)
        {
            WriteFailure(options, "lifecycle-client-unexpected-failure");
            return 1;
        }
    }

    private static async Task<object> Inert(
        global::Briosa.SpatialAnalyzerSdkLifecycle
            .SpatialAnalyzerSdkLifecycleClient sdk,
        global::Briosa.SpatialAnalyzerLifecycle
            .SpatialAnalyzerLifecycleClient application,
        LifecycleOptions options,
        CancellationToken cancellationToken)
    {
        var sdkState = await GetSdk(sdk, options, cancellationToken)
            .ConfigureAwait(false);
        var applicationState = await GetApplication(
            application,
            options,
            cancellationToken).ConfigureAwait(false);
        Require(
            sdkState.SdkState == global::Briosa.SpatialAnalyzerSdkState.Stopped &&
            !sdkState.HasSdkGeneration &&
            !sdkState.ReadyForMp,
            "manual-server-startup-not-inert");
        return Report(options, sdkState, applicationState, "inert");
    }

    private static async Task<object> Owned(
        global::Briosa.SpatialAnalyzerSdkLifecycle
            .SpatialAnalyzerSdkLifecycleClient sdk,
        global::Briosa.SpatialAnalyzerLifecycle
            .SpatialAnalyzerLifecycleClient application,
        LifecycleOptions options,
        CancellationToken cancellationToken)
    {
        var initialApplication = await GetApplication(
            application,
            options,
            cancellationToken).ConfigureAwait(false);
        Require(
            initialApplication.ApplicationState ==
                global::Briosa.SpatialAnalyzerApplicationState.NotRunning,
            "owned-scenario-requires-no-spatial-analyzer");
        var started = await StartSdk(sdk, options, cancellationToken)
            .ConfigureAwait(false);
        var launched = await Launch(application, options, cancellationToken)
            .ConfigureAwait(false);
        var connected = await Connect(
            sdk,
            started.SdkGeneration,
            options,
            cancellationToken).ConfigureAwait(false);
        RequireReady(connected, launched.ApplicationGeneration);
        _ = await StopSdk(
            sdk,
            connected.SdkGeneration,
            options,
            cancellationToken).ConfigureAwait(false);

        var restarted = await StartSdk(sdk, options, cancellationToken)
            .ConfigureAwait(false);
        Require(
            restarted.SdkGeneration != connected.SdkGeneration,
            "sdk-generation-did-not-advance");
        var reconnected = await Connect(
            sdk,
            restarted.SdkGeneration,
            options,
            cancellationToken).ConfigureAwait(false);
        RequireReady(reconnected, launched.ApplicationGeneration);
        _ = await StopSdk(
            sdk,
            reconnected.SdkGeneration,
            options,
            cancellationToken).ConfigureAwait(false);
        var closed = await application.CloseOwnedSpatialAnalyzerAsync(
            new global::Briosa.CloseOwnedSpatialAnalyzerRequest
            {
                ExpectedApplicationGeneration = launched.ApplicationGeneration
            },
            deadline: Deadline(options),
            cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);
        Require(
            closed.State.ApplicationState ==
                global::Briosa.SpatialAnalyzerApplicationState.NotRunning &&
            closed.State.Ownership == global::Briosa.SpatialAnalyzerOwnership.None,
            "owned-spatial-analyzer-did-not-close");
        return Report(options, reconnected, closed.State, "owned-complete");
    }

    private static async Task<object> External(
        global::Briosa.SpatialAnalyzerSdkLifecycle
            .SpatialAnalyzerSdkLifecycleClient sdk,
        global::Briosa.SpatialAnalyzerLifecycle
            .SpatialAnalyzerLifecycleClient application,
        LifecycleOptions options,
        bool stopSdk,
        CancellationToken cancellationToken)
    {
        var applicationState = await GetApplication(
            application,
            options,
            cancellationToken).ConfigureAwait(false);
        Require(
            applicationState.ApplicationState ==
                global::Briosa.SpatialAnalyzerApplicationState.Running &&
            applicationState.Ownership ==
                global::Briosa.SpatialAnalyzerOwnership.External &&
            applicationState.HasApplicationGeneration,
            "external-scenario-requires-one-external-spatial-analyzer");
        var started = await StartSdk(sdk, options, cancellationToken)
            .ConfigureAwait(false);
        var connected = await Connect(
            sdk,
            started.SdkGeneration,
            options,
            cancellationToken).ConfigureAwait(false);
        RequireReady(connected, applicationState.ApplicationGeneration);
        if (!stopSdk)
        {
            return Report(options, connected, applicationState, "external-connected");
        }

        var stopped = await StopSdk(
            sdk,
            connected.SdkGeneration,
            options,
            cancellationToken).ConfigureAwait(false);
        var stillExternal = await GetApplication(
            application,
            options,
            cancellationToken).ConfigureAwait(false);
        Require(
            stillExternal.ApplicationState ==
                global::Briosa.SpatialAnalyzerApplicationState.Running &&
            stillExternal.Ownership ==
                global::Briosa.SpatialAnalyzerOwnership.External,
            "external-spatial-analyzer-was-not-preserved");
        return Report(options, stopped, stillExternal, "external-preserved");
    }

    private static async Task<object> StopCurrentSdk(
        global::Briosa.SpatialAnalyzerSdkLifecycle
            .SpatialAnalyzerSdkLifecycleClient sdk,
        global::Briosa.SpatialAnalyzerLifecycle
            .SpatialAnalyzerLifecycleClient application,
        LifecycleOptions options,
        CancellationToken cancellationToken)
    {
        var current = await GetSdk(sdk, options, cancellationToken)
            .ConfigureAwait(false);
        Require(current.HasSdkGeneration, "sdk-generation-not-active");
        var stopped = await StopSdk(
            sdk,
            current.SdkGeneration,
            options,
            cancellationToken).ConfigureAwait(false);
        var applicationState = await GetApplication(
            application,
            options,
            cancellationToken).ConfigureAwait(false);
        return Report(options, stopped, applicationState, "sdk-stopped");
    }

    private static async Task<object> StartOnly(
        global::Briosa.SpatialAnalyzerSdkLifecycle
            .SpatialAnalyzerSdkLifecycleClient sdk,
        global::Briosa.SpatialAnalyzerLifecycle
            .SpatialAnalyzerLifecycleClient application,
        LifecycleOptions options,
        CancellationToken cancellationToken)
    {
        var started = await StartSdk(sdk, options, cancellationToken)
            .ConfigureAwait(false);
        var applicationState = await GetApplication(
            application,
            options,
            cancellationToken).ConfigureAwait(false);
        return Report(options, started, applicationState, "sdk-started");
    }

    private static async Task<object> SdkLossPrepare(
        global::Briosa.SpatialAnalyzerSdkLifecycle
            .SpatialAnalyzerSdkLifecycleClient sdk,
        global::Briosa.SpatialAnalyzerLifecycle
            .SpatialAnalyzerLifecycleClient application,
        LifecycleOptions options,
        CancellationToken cancellationToken)
    {
        var started = await StartSdk(sdk, options, cancellationToken)
            .ConfigureAwait(false);
        var launched = await Launch(application, options, cancellationToken)
            .ConfigureAwait(false);
        var connected = await Connect(
            sdk,
            started.SdkGeneration,
            options,
            cancellationToken).ConfigureAwait(false);
        RequireReady(connected, launched.ApplicationGeneration);
        return Report(options, connected, launched, "awaiting-sdk-loss");
    }

    private static async Task<object> SdkLossRecover(
        global::Briosa.SpatialAnalyzerSdkLifecycle
            .SpatialAnalyzerSdkLifecycleClient sdk,
        global::Briosa.SpatialAnalyzerLifecycle
            .SpatialAnalyzerLifecycleClient application,
        LifecycleOptions options,
        CancellationToken cancellationToken)
    {
        var faulted = await WaitForSdk(
            sdk,
            state => state.SdkState == global::Briosa.SpatialAnalyzerSdkState.Faulted,
            options,
            cancellationToken).ConfigureAwait(false);
        Require(
            faulted.LastIncident is not null &&
            faulted.LastIncident.TerminationKind ==
                global::Briosa.SpatialAnalyzerSdkTerminationKind.SdkProcessExited &&
            !faulted.ReadyForMp,
            "sdk-loss-incident-not-observed");
        var recovered = await sdk.RecoverSpatialAnalyzerSdkAsync(
            new global::Briosa.RecoverSpatialAnalyzerSdkRequest
            {
                ExpectedSdkGeneration = faulted.SdkGeneration,
                Mode = global::Briosa.SpatialAnalyzerSdkRecoveryMode.ReplaceWithoutReplay
            },
            deadline: Deadline(options),
            cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);
        Require(
            recovered.State.SdkState == global::Briosa.SpatialAnalyzerSdkState.Running &&
            recovered.State.ConnectionState ==
                global::Briosa.SpatialAnalyzerConnectionState.Disconnected &&
            recovered.State.SdkGeneration != faulted.SdkGeneration &&
            recovered.State.LastIncident is not null &&
            !recovered.State.ReadyForMp,
            "sdk-recovery-did-not-create-disconnected-generation");
        var applicationState = await GetApplication(
            application,
            options,
            cancellationToken).ConfigureAwait(false);
        var connected = await Connect(
            sdk,
            recovered.State.SdkGeneration,
            options,
            cancellationToken).ConfigureAwait(false);
        RequireReady(connected, applicationState.ApplicationGeneration);
        _ = await StopSdk(
            sdk,
            connected.SdkGeneration,
            options,
            cancellationToken).ConfigureAwait(false);
        var closed = await application.CloseOwnedSpatialAnalyzerAsync(
            new global::Briosa.CloseOwnedSpatialAnalyzerRequest
            {
                ExpectedApplicationGeneration = applicationState.ApplicationGeneration
            },
            deadline: Deadline(options),
            cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);
        return Report(options, recovered.State, closed.State, "sdk-loss-recovered");
    }

    private static async Task<global::Briosa.SpatialAnalyzerSdkLifecycleState> StartSdk(
        global::Briosa.SpatialAnalyzerSdkLifecycle
            .SpatialAnalyzerSdkLifecycleClient client,
        LifecycleOptions options,
        CancellationToken cancellationToken)
    {
        var response = await client.StartSpatialAnalyzerSdkAsync(
            new global::Briosa.StartSpatialAnalyzerSdkRequest(),
            deadline: Deadline(options),
            cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);
        Require(
            response.State.SdkState == global::Briosa.SpatialAnalyzerSdkState.Running &&
            response.State.ConnectionState ==
                global::Briosa.SpatialAnalyzerConnectionState.Disconnected &&
            response.State.HasSdkGeneration &&
            !response.State.ReadyForMp,
            "sdk-did-not-start-disconnected");
        return response.State;
    }

    private static async Task<global::Briosa.SpatialAnalyzerLifecycleState> Launch(
        global::Briosa.SpatialAnalyzerLifecycle
            .SpatialAnalyzerLifecycleClient client,
        LifecycleOptions options,
        CancellationToken cancellationToken)
    {
        var response = await client.LaunchSpatialAnalyzerAsync(
            new global::Briosa.LaunchSpatialAnalyzerRequest
            {
                StartMinimized = options.StartMinimized
            },
            deadline: Deadline(options),
            cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);
        Require(
            response.State.ApplicationState ==
                global::Briosa.SpatialAnalyzerApplicationState.Running &&
            response.State.Ownership ==
                global::Briosa.SpatialAnalyzerOwnership.ServerLaunched &&
            response.State.HasApplicationGeneration,
            "spatial-analyzer-was-not-launched-owned");
        return response.State;
    }

    private static async Task<global::Briosa.SpatialAnalyzerSdkLifecycleState> Connect(
        global::Briosa.SpatialAnalyzerSdkLifecycle
            .SpatialAnalyzerSdkLifecycleClient client,
        int generation,
        LifecycleOptions options,
        CancellationToken cancellationToken) =>
        (await client.ConnectToSpatialAnalyzerAsync(
            new global::Briosa.ConnectToSpatialAnalyzerRequest
            {
                ExpectedSdkGeneration = generation
            },
            deadline: Deadline(options),
            cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false)).State;

    private static async Task<global::Briosa.SpatialAnalyzerSdkLifecycleState> StopSdk(
        global::Briosa.SpatialAnalyzerSdkLifecycle
            .SpatialAnalyzerSdkLifecycleClient client,
        int generation,
        LifecycleOptions options,
        CancellationToken cancellationToken) =>
        (await client.StopSpatialAnalyzerSdkAsync(
            new global::Briosa.StopSpatialAnalyzerSdkRequest
            {
                ExpectedSdkGeneration = generation
            },
            deadline: Deadline(options),
            cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false)).State;

    private static async Task<global::Briosa.SpatialAnalyzerSdkLifecycleState> GetSdk(
        global::Briosa.SpatialAnalyzerSdkLifecycle
            .SpatialAnalyzerSdkLifecycleClient client,
        LifecycleOptions options,
        CancellationToken cancellationToken) =>
        (await client.GetSpatialAnalyzerSdkStateAsync(
            new global::Briosa.GetSpatialAnalyzerSdkStateRequest(),
            deadline: Deadline(options),
            cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false)).State;

    private static async Task<global::Briosa.SpatialAnalyzerLifecycleState> GetApplication(
        global::Briosa.SpatialAnalyzerLifecycle
            .SpatialAnalyzerLifecycleClient client,
        LifecycleOptions options,
        CancellationToken cancellationToken) =>
        (await client.GetSpatialAnalyzerStateAsync(
            new global::Briosa.GetSpatialAnalyzerStateRequest(),
            deadline: Deadline(options),
            cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false)).State;

    private static async Task<global::Briosa.SpatialAnalyzerSdkLifecycleState> WaitForSdk(
        global::Briosa.SpatialAnalyzerSdkLifecycle
            .SpatialAnalyzerSdkLifecycleClient client,
        Func<global::Briosa.SpatialAnalyzerSdkLifecycleState, bool> predicate,
        LifecycleOptions options,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var state = await GetSdk(client, options, cancellationToken)
                .ConfigureAwait(false);
            if (predicate(state))
            {
                return state;
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(200),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static void RequireReady(
        global::Briosa.SpatialAnalyzerSdkLifecycleState state,
        int applicationGeneration) =>
        Require(
            state.SdkState == global::Briosa.SpatialAnalyzerSdkState.Ready &&
            state.ConnectionState ==
                global::Briosa.SpatialAnalyzerConnectionState.Connected &&
            state.ExecutionReadinessState ==
                global::Briosa.SpatialAnalyzerExecutionReadinessState.ExecutionReady &&
            state.ReadyForMp &&
            state.HasApplicationGeneration &&
            state.ApplicationGeneration == applicationGeneration,
            "sdk-did-not-become-execution-ready");

    private static object Report(
        LifecycleOptions options,
        global::Briosa.SpatialAnalyzerSdkLifecycleState sdk,
        global::Briosa.SpatialAnalyzerLifecycleState application,
        string outcome) => new
        {
            schema_version = 1,
            success = true,
            scenario = options.ScenarioName,
            outcome,
            sdk_state = Canonical(sdk.SdkState.ToString()),
            sdk_generation = sdk.HasSdkGeneration ? sdk.SdkGeneration : (int?)null,
            application_state = Canonical(application.ApplicationState.ToString()),
            application_ownership = Canonical(application.Ownership.ToString()),
            application_generation = application.HasApplicationGeneration
                ? application.ApplicationGeneration
                : (int?)null,
            ready_for_mp = sdk.ReadyForMp,
            incident_kind = sdk.LastIncident is null
                ? null
                : Canonical(sdk.LastIncident.TerminationKind.ToString())
        };

    private static void Require(bool condition, string diagnosticCode)
    {
        if (!condition)
        {
            throw new LifecycleFailureException(diagnosticCode);
        }
    }

    private static DateTime Deadline(LifecycleOptions options) =>
        DateTime.UtcNow.Add(options.Timeout);

    private static string Canonical(string value) =>
        string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character)
                ? $"_{char.ToUpperInvariant(character)}"
                : char.ToUpperInvariant(character).ToString()));

    private static void WriteFailure(
        LifecycleOptions? options,
        string diagnosticCode) =>
        Console.Error.WriteLine(JsonSerializer.Serialize(new
        {
            schema_version = 1,
            success = false,
            scenario = options?.ScenarioName,
            diagnostic_code = diagnosticCode
        }, JsonOptions));

    private enum LifecycleScenario
    {
        Inert,
        Owned,
        External,
        ExternalConnect,
        StopSdk,
        StartSdk,
        SdkLossPrepare,
        SdkLossRecover
    }

    private sealed record LifecycleOptions(
        Uri Address,
        LifecycleScenario Scenario,
        string ScenarioName,
        TimeSpan Timeout,
        bool StartMinimized)
    {
        public static LifecycleOptions Parse(string[] arguments)
        {
            var address = new Uri(
                GetArgument(arguments, "--address") ?? "http://127.0.0.1:50051",
                UriKind.Absolute);
            if (!address.IsLoopback || address.Scheme != Uri.UriSchemeHttp)
            {
                throw new LifecycleFailureException(
                    "lifecycle-address-must-be-loopback-http");
            }

            var scenarioName = GetArgument(arguments, "--scenario") ?? "inert";
            var scenario = scenarioName switch
            {
                "inert" => LifecycleScenario.Inert,
                "owned" => LifecycleScenario.Owned,
                "external" => LifecycleScenario.External,
                "external-connect" => LifecycleScenario.ExternalConnect,
                "stop-sdk" => LifecycleScenario.StopSdk,
                "start-sdk" => LifecycleScenario.StartSdk,
                "sdk-loss-prepare" => LifecycleScenario.SdkLossPrepare,
                "sdk-loss-recover" => LifecycleScenario.SdkLossRecover,
                _ => throw new LifecycleFailureException(
                    "unsupported-lifecycle-scenario")
            };
            var timeoutSeconds = int.Parse(
                GetArgument(arguments, "--timeout-seconds") ?? "90",
                System.Globalization.CultureInfo.InvariantCulture);
            if (timeoutSeconds is < 1 or > 600)
            {
                throw new LifecycleFailureException(
                    "lifecycle-timeout-out-of-range");
            }

            return new LifecycleOptions(
                address,
                scenario,
                scenarioName,
                TimeSpan.FromSeconds(timeoutSeconds),
                arguments.Contains("--start-minimized", StringComparer.Ordinal));
        }

        private static string? GetArgument(string[] arguments, string name)
        {
            var index = Array.IndexOf(arguments, name);
            return index >= 0 && index + 1 < arguments.Length
                ? arguments[index + 1]
                : null;
        }
    }

    [SuppressMessage(
        "Design",
        "CA1032:Implement standard exception constructors",
        Justification = "The private exception carries one stable diagnostic code.")]
    private sealed class LifecycleFailureException(string diagnosticCode) : Exception
    {
        public string DiagnosticCode { get; } = diagnosticCode;
    }
}
