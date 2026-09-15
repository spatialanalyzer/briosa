using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Briosa.Desktop;

namespace Briosa.ControlCenter;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "The WPF application calls Exit to cancel and dispose window lifetime resources.")]
public partial class MainWindow : Window
{
    private readonly DesktopSession _session;
    private readonly ActivityReader _activity = new();
    private readonly CancellationTokenSource _closing = new();
    private bool _busy;
    private bool _initialized;
    private bool _exiting;
    public event EventHandler? ServerStateChanged;
    public DesktopState Current => _session.State;
    public bool NotificationsEnabled => NotificationsBox.IsChecked == true;
    public bool IsBusy => _busy;

    public MainWindow(DesktopSession session)
    {
        _session = session;
        InitializeComponent();
        try
        {
            var preferences = DesktopStore.ReadPreferences();
            NotificationsBox.IsChecked = preferences.Notifications;
            ThemeSelector.SelectedIndex = preferences.Theme switch { "light" => 1, "dark" => 2, _ => 0 };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        { NotificationsBox.IsChecked = true; }
        _initialized = true;
        ApplyTheme();
        Render(_session.State);
    }

    public void Render(DesktopState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        HeadingText.Text = state.Heading;
        GuidanceText.Text = state.Guidance;
        IncidentText.Text = _session.HistoricalUnknownOutcome && !state.OutcomeUnknown
            ? "An earlier command outcome remains unknown. Starting a new generation does not resolve it." : "";
        OwnershipText.Text = state.CanManage ? "Managed by this Control Center · lifecycle actions affect this server and its clients."
            : _session.HasServer ? "Monitoring only · this server belongs to another launcher, or ownership could not be verified." : "Start a managed server to enable lifecycle controls.";
        HostText.Text = state.Host;
        EndpointText.Text = state.Endpoint;
        ApplicationText.Text = $"State: {SafeText.Label(state.Application?.ApplicationState)}\nOwnership: {SafeText.Label(state.Application?.Ownership)}";
        SdkText.Text = $"State: {SafeText.Label(state.Sdk?.SdkState)}\nAttachment: {SafeText.Label(state.Sdk?.ConnectionState)}\nRecovery: {SafeText.Label(state.Sdk?.RecoveryState)}";
        IdentityText.Text = $"Activated SDK: {DesktopState.IdentityText(state.Info?.ActivatedSdkIdentity)}\n\nConnected SA: {DesktopState.IdentityText(state.Info?.ConnectedSpatialAnalyzerIdentity)}\n\nExecution: {SafeText.Label(state.Sdk?.ExecutionReadinessState)}";
        var version = state.Info?.Version;
        VersionText.Text = $"Briosa: {SafeText.Version(version?.BriosaVersion)}\nSA target: {DesktopProtocol.Target}\nProtocol: {SafeText.Version(version?.ProtocolPackage)}\nSource: {SafeText.Version(version?.SourceRevision)}\nDiagnostic: {SafeText.Code(state.Sdk?.DiagnosticCode)}\nIsolation: {SafeText.Label(state.Info?.TargetIsolationMode)}";
        ObservedText.Text = state.ObservedAt is { } time ? "Last observation: " + time.ToString("G", CultureInfo.CurrentCulture) + (state.Available ? "" : " · stale") : "No server observation yet.";
        StartButton.IsEnabled = !_busy && !_session.HasServer && !state.Available;
        StopButton.IsEnabled = RestartButton.IsEnabled = !_busy && state.Available && state.CanManage;
        StartSdkButton.IsEnabled = !_busy && state.CanStartSdk;
        ConnectButton.IsEnabled = !_busy && state.CanConnect;
        ReconnectButton.IsEnabled = !_busy && state.CanReconnect;
        StopSdkButton.IsEnabled = !_busy && state.CanStopSdk;
        RecoverButton.IsEnabled = !_busy && state.CanRecover;
        ServerStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_initialized) return;
        var compact = ActualWidth < 1040;
        OverviewCards.ColumnDefinitions[1].Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        for (int i = 0; i < OverviewCards.Children.Count; i++)
        {
            Grid.SetRow(OverviewCards.Children[i], compact ? i : i / 2);
            Grid.SetColumn(OverviewCards.Children[i], compact ? 0 : i % 2);
        }
    }

    public async Task ObserveAsync()
    {
        if (_busy || _exiting || !_session.HasServer) return;
        _busy = true;
        Render(_session.State);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_closing.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));
            await _session.RefreshAsync(timeout.Token);
            if (IsVisible && PauseActivity.IsChecked != true) await ReadActivityAsync();
        }
        catch (Exception exception) when (ExpectedFailure(exception)) { _session.ReconcileExit(); }
        finally { _busy = false; if (!_exiting) Render(_session.State); }
    }

    public async Task RunAsync(Func<CancellationToken, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_busy || _exiting) return;
        _busy = true;
        ActionText.Text = "Working…";
        Render(_session.State);
        try { await action(_closing.Token); ActionText.Text = "Action completed. Review the current state before continuing."; }
        catch (Exception exception) when (ExpectedFailure(exception))
        {
            ActionText.Text = "The action could not be confirmed. Refresh and review server state before trying again. No action was retried.";
            if (_session.LastReply is { Accepted: false } reply) ActionText.Text += " Diagnostic: " + SafeText.Code(reply.Diagnostic);
        }
        finally { _busy = false; if (!_exiting) Render(_session.State); }
    }

    private void NavigationChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;
        OverviewPage.Visibility = Navigation.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        ActivityPage.Visibility = Navigation.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        DetailsPage.Visibility = Navigation.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
    }

    public void ShowOverview() { Navigation.SelectedIndex = 0; ShowWindow(); }
    public void ShowActivity() { Navigation.SelectedIndex = 1; ShowWindow(); }
    public void ShowWindow() { Show(); WindowState = System.Windows.WindowState.Normal; Activate(); }
    public Task StartServerAsync() => RunAsync(_session.StartAsync);
    public Task StopServerAsync() => Confirm("Stop this Briosa server? Connected clients will lose access. In-flight commands may have an unknown outcome. SpatialAnalyzer will remain open.")
        ? RunAsync(_session.StopAsync) : Task.CompletedTask;
    public Task RestartServerAsync() => Confirm("Restart this Briosa server? Clients must establish a new runtime generation. Commands will not be replayed, and the replacement SDK stays stopped until explicitly started.")
        ? RunAsync(_session.RestartAsync) : Task.CompletedTask;
    public Task ReconnectAsync() => RunAsync(token => _session.ActAsync(DesktopAction.Reconnect, token));
    public Task RecoverAsync() => Confirm("Recover this SDK generation without replaying commands? Ensure competing SDK clients have been closed and the SpatialAnalyzer environment is ready. Any earlier unknown outcome remains unknown.")
        ? RunAsync(token => _session.ActAsync(DesktopAction.RecoverSdk, token)) : Task.CompletedTask;

    private async Task ReadActivityAsync()
    {
        if (_session.LastReply is not { } reply) return;
        await Task.Run(() => _activity.ReadRecent(reply.LogDirectory, reply.LogInstance));
        if (!_exiting) { ActivityStatus.Text = _activity.Status; ApplyFilter(); }
    }

    private void ApplyFilter()
    {
        if (!_initialized) return;
        var search = SearchBox.Text.Trim();
        ActivityGrid.ItemsSource = _activity.Entries.Where(entry =>
            (SeverityFilter.SelectedIndex == 0 || SeverityFilter.SelectedIndex == 1 && entry.Level is "Warning" or "Error" or "Fatal" ||
             SeverityFilter.SelectedIndex == 2 && entry.Level is "Error" or "Fatal") &&
            (search.Length == 0 || $"{entry.Category} {entry.Message} {entry.Operation} {entry.Details} {entry.Correlation}".Contains(search, StringComparison.OrdinalIgnoreCase))).Reverse().ToArray();
    }

    public void CopyEndpoint()
    {
        try { if (!string.IsNullOrWhiteSpace(_session.State.Endpoint)) Clipboard.SetText(_session.State.Endpoint); }
        catch (System.Runtime.InteropServices.ExternalException) { ActionText.Text = "The clipboard is busy. Try copying again."; }
    }

    public async Task ExportAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Briosa support bundle (*.zip)|*.zip", FileName = "briosa-support.zip", OverwritePrompt = true };
        if (dialog.ShowDialog(this) != true) return;
        await RunAsync(async _ =>
        {
            await ReadActivityAsync();
            SupportBundle.Export(dialog.FileName, _session.State, _activity.Entries, _session.HistoricalUnknownOutcome, DesktopStore.PackageDirectory(AppContext.BaseDirectory));
        });
    }

    private async Task DiagnosticsAsync()
    {
        await RunAsync(async token =>
        {
            var packageDirectory = DesktopStore.PackageDirectory(AppContext.BaseDirectory);
            using var process = Process.Start(new ProcessStartInfo(Path.Combine(packageDirectory, "Briosa.Server.exe"))
            { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, Arguments = "diagnostics", WorkingDirectory = packageDirectory })
                ?? throw new IOException("Package diagnostics could not start.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { if (!process.HasExited) process.Kill(); throw; }
            if (process.ExitCode != 0) throw new IOException("Package diagnostics reported a problem.");
        });
    }

    private bool Confirm(string message) => MessageBox.Show(this, message, "Briosa Control Center", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK;
    private static bool ExpectedFailure(Exception exception) => exception is IOException or UnauthorizedAccessException or InvalidOperationException or
        OperationCanceledException or TimeoutException or System.Text.Json.JsonException or ArgumentException or System.ComponentModel.Win32Exception;
    private void WindowClosing(object? sender, CancelEventArgs e) { if (!_exiting) { e.Cancel = true; Hide(); } }
    public void Exit() { if (_exiting) return; _exiting = true; _closing.Cancel(); _closing.Dispose(); Close(); }
    private async void RefreshClicked(object sender, RoutedEventArgs e) => await ObserveAsync();
    private async void StartClicked(object sender, RoutedEventArgs e) => await StartServerAsync();
    private async void StopClicked(object sender, RoutedEventArgs e) => await StopServerAsync();
    private async void RestartClicked(object sender, RoutedEventArgs e) => await RestartServerAsync();
    private async void StartSdkClicked(object sender, RoutedEventArgs e) => await RunAsync(token => _session.ActAsync(DesktopAction.StartSdk, token));
    private async void ConnectClicked(object sender, RoutedEventArgs e) => await RunAsync(token => _session.ActAsync(DesktopAction.Connect, token));
    private async void ReconnectClicked(object sender, RoutedEventArgs e) => await ReconnectAsync();
    private async void RecoverClicked(object sender, RoutedEventArgs e) => await RecoverAsync();
    private async void StopSdkClicked(object sender, RoutedEventArgs e)
    {
        if (Confirm("Stop the SDK? Commands will become unavailable and an in-flight command may have an unknown outcome. SpatialAnalyzer stays open."))
            await RunAsync(token => _session.ActAsync(DesktopAction.StopSdk, token));
    }
    private void CopyEndpointClicked(object sender, RoutedEventArgs e) => CopyEndpoint();
    private async void ExportClicked(object sender, RoutedEventArgs e) => await ExportAsync();
    private async void DiagnosticsClicked(object sender, RoutedEventArgs e) => await DiagnosticsAsync();
    private void FilterChanged(object sender, RoutedEventArgs e) => ApplyFilter();
    private void LatestClicked(object sender, RoutedEventArgs e)
    {
        if (ActivityGrid.Items.Count > 0) ActivityGrid.ScrollIntoView(ActivityGrid.Items[0]);
    }
    private void NotificationsChanged(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        try { DesktopStore.SavePreferences(new(NotificationsEnabled, SelectedTheme)); }
        catch (Exception exception) when (ExpectedFailure(exception)) { ActionText.Text = "Notification preference could not be saved. Check access to your Briosa settings."; }
    }
    private string SelectedTheme => ThemeSelector.SelectedIndex switch { 1 => "light", 2 => "dark", _ => "system" };
    private void ApplyTheme() => BrandTheme.ApplyPreference(System.Windows.Application.Current, SelectedTheme);
    private void ThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;
        ApplyTheme();
        NotificationsChanged(sender, e);
    }
    private void ExitClicked(object sender, RoutedEventArgs e) => System.Windows.Application.Current.Shutdown();
}
