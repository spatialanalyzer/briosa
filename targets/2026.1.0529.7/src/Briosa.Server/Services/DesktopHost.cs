using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Text.Json;
using Briosa.Desktop;
using Briosa.Server.Operations;
using Briosa.Server.Security;
using Google.Protobuf;

namespace Briosa.Server.Services;

internal sealed record DesktopHostOptions(bool Enabled, bool AutoLaunch, string Instance, string? Credential)
{
    public bool Announced { get; init; }
    public static DesktopHostOptions Create(IConfiguration configuration)
    {
        var instance = Environment.GetEnvironmentVariable(DesktopProtocol.InstanceVariable);
        var credential = Environment.GetEnvironmentVariable(DesktopProtocol.CredentialVariable);
        // Child workers and the monitoring companion must never inherit shutdown authority.
        Environment.SetEnvironmentVariable(DesktopProtocol.InstanceVariable, null);
        Environment.SetEnvironmentVariable(DesktopProtocol.CredentialVariable, null);
        var mode = configuration["Briosa:Desktop:Mode"] ?? "Auto";
        if (mode is not ("Auto" or "Disabled" or "Owned"))
            throw new InvalidOperationException("Invalid Briosa desktop mode.");
        if (mode == "Owned" && (!DesktopProtocol.IsInstance(instance) || credential is not { Length: 64 } ||
            !credential.All(char.IsAsciiHexDigit)))
            throw new InvalidOperationException("Invalid desktop ownership configuration.");
        using var process = Process.GetCurrentProcess();
        bool interactive = Environment.UserInteractive && process.SessionId != 0;
        bool enabled = mode == "Owned" || mode == "Auto" && interactive &&
            File.Exists(Path.Combine(AppContext.BaseDirectory, "Briosa.ControlCenter.exe"));
        return new(enabled, enabled && mode == "Auto", mode == "Owned" ? instance! : DesktopProtocol.NewInstance(), mode == "Owned" ? credential : null);
    }
}

/// <summary>Private per-instance controller channel. Public clients retain their existing lifecycle API.</summary>
internal sealed class DesktopHost(
    DesktopHostOptions options, IHostApplicationLifetime lifetime, IConfiguration configuration,
    ServerDiscoveryService discovery, SpatialAnalyzerSdkLifecycleCoordinator sdk,
    ISpatialAnalyzerLifecycleStateProvider application, IEnumerable<ILoggerProvider> providers,
    BriosaLoggingOptions logging, LifecycleAuditLogger? auditLogger = null) : BackgroundService
{
    private readonly SemaphoreSlim _clients = new(4, 4);
    private readonly HashSet<Task> _requests = [];
    private string _state = "Starting";

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Connected pipe ownership transfers to HandleAsync; failed acceptance disposes it locally.")]
    [SuppressMessage("Reliability", "CA2025:Do not pass disposable objects into unawaited tasks", Justification = "All request tasks complete in finally before the lifetime registrations and service are disposed.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled) return;
        using var started = lifetime.ApplicationStarted.Register(() => Volatile.Write(ref _state, "Running"));
        using var stopping = lifetime.ApplicationStopping.Register(() => Volatile.Write(ref _state, "Stopping"));
        try
        {
            if (!options.Announced)
            {
                using var process = Process.GetCurrentProcess();
                DesktopStore.Register(new(options.Instance, AppContext.BaseDirectory, process.Id, process.StartTime.ToUniversalTime().Ticks));
            }
            while (!stoppingToken.IsCancellationRequested)
            {
                await _clients.WaitAsync(stoppingToken).ConfigureAwait(false);
                var pipe = new NamedPipeServerStream(DesktopProtocol.PipeName(options.Instance), PipeDirection.InOut,
                    4, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                try { await pipe.WaitForConnectionAsync(stoppingToken).ConfigureAwait(false); }
                catch { await pipe.DisposeAsync().ConfigureAwait(false); _clients.Release(); throw; }
                var request = HandleAsync(pipe, stoppingToken);
                _requests.RemoveWhere(task => task.IsCompleted);
                _requests.Add(request);
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            // Optional desktop integration must not take down the API host.
        }
        finally
        {
            await Task.WhenAll(_requests).ConfigureAwait(false);
            try { DesktopStore.RemoveRegistration(options.Instance); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
        }
    }

    private async Task HandleAsync(NamedPipeServerStream pipe, CancellationToken stoppingToken)
    {
        await using (pipe.ConfigureAwait(false))
        {
            using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            readTimeout.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                var request = await DesktopProtocol.ReadAsync<DesktopRequest>(pipe, readTimeout.Token).ConfigureAwait(false);
                using var actionTimeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                actionTimeout.CancelAfter(TimeSpan.FromMinutes(2));
                var reply = await DispatchAsync(request, actionTimeout.Token).ConfigureAwait(false);
                using var writeTimeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                writeTimeout.CancelAfter(TimeSpan.FromSeconds(5));
                await DesktopProtocol.WriteAsync(pipe, reply, writeTimeout.Token).ConfigureAwait(false);
                // Stop only after acknowledgement has been flushed. Losing it does not authorize a replay.
                if (request.Action == DesktopAction.StopServer && reply.Accepted) lifetime.StopApplication();
            }
            catch (Exception exception) when (exception is IOException or OperationCanceledException or JsonException or InvalidOperationException) { }
            finally { _clients.Release(); }
        }
    }

    internal async Task<DesktopReply> DispatchAsync(DesktopRequest request, CancellationToken cancellationToken)
    {
        bool owns = request.Instance == options.Instance && DesktopProtocol.CredentialMatches(options.Credential, request.Credential);
        bool accepted = request.Instance == options.Instance && Enum.IsDefined(request.Action) &&
            (request.Action == DesktopAction.Status || owns);
        var diagnostic = accepted ? "" : "desktop-ownership-required";
        if (accepted && request.Action != DesktopAction.Status && Volatile.Read(ref _state) != "Running")
        { accepted = false; diagnostic = "desktop-host-not-running"; }
        if (accepted)
        {
            try
            {
                switch (request.Action)
                {
                    case DesktopAction.StartSdk: await sdk.StartAsync(cancellationToken).ConfigureAwait(false); break;
                    case DesktopAction.Connect: await sdk.ConnectAsync(request.ExpectedSdkGeneration, false, cancellationToken).ConfigureAwait(false); break;
                    case DesktopAction.Reconnect: await sdk.ConnectAsync(request.ExpectedSdkGeneration, true, cancellationToken).ConfigureAwait(false); break;
                    case DesktopAction.StopSdk: await sdk.StopAsync(request.ExpectedSdkGeneration, cancellationToken).ConfigureAwait(false); break;
                    case DesktopAction.RecoverSdk: await sdk.RecoverAsync(request.ExpectedSdkGeneration,
                        SpatialAnalyzerSdkRecoveryMode.ReplaceWithoutReplay, cancellationToken).ConfigureAwait(false); break;
                }
            }
            catch (SdkLifecycleException exception)
            {
                accepted = false; diagnostic = exception.Detail.DiagnosticCode;
                auditLogger?.Rejected("Desktop" + request.Action, exception.StatusCode, diagnostic);
            }
        }
        var endpoint = PublicEndpointConfiguration.Resolve(configuration);
        var address = new UriBuilder("http", endpoint.Address.ToString(), endpoint.Port).Uri;
        var log = providers.OfType<BriosaLogProvider>().SingleOrDefault();
        var applicationState = await application.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        return new DesktopReply
        {
            Instance = options.Instance, Target = SpatialAnalyzerApi.TargetVersion, Endpoint = address.GetLeftPart(UriPartial.Authority),
            HostState = Volatile.Read(ref _state), CanManage = owns, Accepted = accepted, Diagnostic = diagnostic,
            LogInstance = log?.InstanceId ?? "", LogDirectory = logging.FileEnabled ? logging.Directory : null,
            ServerInfoJson = JsonFormatter.Default.Format(discovery.CreateServerInfo()),
            SdkStateJson = JsonFormatter.Default.Format(sdk.Current), ApplicationStateJson = JsonFormatter.Default.Format(applicationState)
        };
    }

    public override void Dispose()
    {
        base.Dispose();
        _clients.Dispose();
    }
}
