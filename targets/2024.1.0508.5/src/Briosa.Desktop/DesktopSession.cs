using System.Diagnostics;

namespace Briosa.Desktop;

public sealed class DesktopSession : IDisposable
{
    private readonly string _packageDirectory;
    private Process? _ownedProcess;
    private DesktopOwnership? _ownership;
    public string? Instance { get; private set; }
    public DesktopReply? LastReply { get; private set; }
    public DesktopState State { get; private set; } = new();
    public bool HistoricalUnknownOutcome { get; private set; }
    public bool HasServer => Instance is not null;
    public event EventHandler? InstanceSelected;

    public DesktopIdentitySettings ReadIdentitySettings() => DesktopStore.ReadIdentitySettings(_packageDirectory);
    public void SaveIdentitySettings(DesktopIdentitySettings settings)
    {
        if (HasServer) throw new InvalidOperationException("Stop the selected server before changing connection setup.");
        DesktopStore.SaveIdentitySettings(_packageDirectory, settings);
    }

    public DesktopSession(string packageDirectory, string? monitorInstance = null)
    {
        _packageDirectory = Path.GetFullPath(packageDirectory);
        Instance = monitorInstance ?? DesktopStore.Find(_packageDirectory)?.Instance;
        if (Instance is not null) _ownership = DesktopStore.ReadOwnership(Instance);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (Instance is not null) throw new InvalidOperationException("A server is already selected.");
        var existing = DesktopStore.Find(_packageDirectory);
        if (existing is not null)
        {
            Instance = existing.Instance;
            _ownership = DesktopStore.ReadOwnership(Instance);
            InstanceSelected?.Invoke(this, EventArgs.Empty);
            await RefreshAsync(cancellationToken).ConfigureAwait(false);
            return;
        }
        var identitySettings = ReadIdentitySettings();
        Instance = DesktopProtocol.NewInstance();
        _ownership = new(Instance, DesktopProtocol.NewCredential());
        DesktopStore.SaveOwnership(_ownership);
        InstanceSelected?.Invoke(this, EventArgs.Empty);
        var start = new ProcessStartInfo(Path.Combine(_packageDirectory, "Briosa.Server.exe"))
        {
            WorkingDirectory = _packageDirectory, UseShellExecute = false,
            CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden
        };
        start.Environment[DesktopProtocol.ModeVariable] = "Owned";
        start.Environment[DesktopProtocol.InstanceVariable] = Instance;
        start.Environment[DesktopProtocol.CredentialVariable] = _ownership.Credential;
        identitySettings.ApplyTo(start);
        try
        {
            _ownedProcess = Process.Start(start) ?? throw new IOException("Server launch failed.");
            using var startup = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            startup.CancelAfter(TimeSpan.FromSeconds(20));
            while (!startup.IsCancellationRequested)
            {
                if (_ownedProcess.HasExited) throw new IOException("Server exited during startup.");
                try
                {
                    await ObserveStartupAsync(startup.Token).ConfigureAwait(false);
                    if (State.Available) return;
                }
                catch (Exception exception) when (exception is IOException or OperationCanceledException) { }
                await Task.Delay(150, startup.Token).ConfigureAwait(false);
            }
            throw new TimeoutException("Server startup did not complete within the observation budget.");
        }
        catch
        {
            State = State.Unavailable("Server startup failed");
            // Never terminate a possibly running server because the UI stopped waiting.
            if (_ownedProcess is null || _ownedProcess.HasExited) ForgetStoppedServer();
            throw;
        }
    }

    private async Task ObserveStartupAsync(CancellationToken cancellationToken)
    {
        using var attempt = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        attempt.CancelAfter(TimeSpan.FromMilliseconds(750));
        await RefreshAsync(attempt.Token).ConfigureAwait(false);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (Instance is null) return;
        try
        {
            var reply = await DesktopProtocol.SendAsync(new(Instance, Credential: _ownership?.Credential), cancellationToken).ConfigureAwait(false);
            LastReply = reply;
            State = DesktopState.FromReply(reply);
            HistoricalUnknownOutcome |= State.OutcomeUnknown;
        }
        catch
        {
            State = State.Unavailable();
            var exit = DesktopStore.ReadExit(Instance);
            if (exit is { Clean: false }) State = State.Unavailable("Server startup or shutdown failed");
            throw;
        }
    }

    public async Task ActAsync(DesktopAction action, CancellationToken cancellationToken)
    {
        if (Instance is null || _ownership is null) throw new InvalidOperationException("This server is monitored only.");
        // Refresh the owner proof and generation; the server validates both again when it acts.
        await RefreshAsync(cancellationToken).ConfigureAwait(false);
        if (!State.Available || !State.CanManage) throw new InvalidOperationException("Server management is unavailable.");
        var reply = await DesktopProtocol.SendAsync(new(Instance, action, _ownership.Credential,
            State.Sdk?.SdkGeneration ?? 0), cancellationToken).ConfigureAwait(false);
        LastReply = reply;
        State = DesktopState.FromReply(reply);
        HistoricalUnknownOutcome |= State.OutcomeUnknown;
        if (!reply.Accepted) throw new DesktopActionRejectedException(reply.Diagnostic);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Instance is null) return;
        var registration = DesktopStore.Find(_packageDirectory);
        if (_ownedProcess is null && registration?.Instance == Instance)
            _ownedProcess = Process.GetProcessById(registration.ProcessId);
        if (_ownedProcess is null) throw new InvalidOperationException("The exact owned process could not be established.");
        await ActAsync(DesktopAction.StopServer, cancellationToken).ConfigureAwait(false);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        await _ownedProcess.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        ForgetStoppedServer();
        State = new();
    }

    public async Task RestartAsync(CancellationToken cancellationToken)
    {
        await StopAsync(cancellationToken).ConfigureAwait(false);
        await StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public bool ReconcileExit()
    {
        bool exited = _ownedProcess?.HasExited == true;
        if (!exited && Instance is not null && !State.Available)
        {
            var registration = DesktopStore.ReadRegistration(Instance);
            exited = DesktopStore.ReadExit(Instance) is not null || registration is not null && !DesktopStore.IsAlive(registration);
        }
        if (exited)
        {
            ForgetStoppedServer();
            State = State.Unavailable("Server exited");
            return true;
        }
        return false;
    }

    private void ForgetStoppedServer()
    {
        if (Instance is not null) DesktopStore.RemoveOwnership(Instance);
        _ownedProcess?.Dispose();
        _ownedProcess = null;
        Instance = null;
        _ownership = null;
    }

    public void Dispose() => _ownedProcess?.Dispose();
}
