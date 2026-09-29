using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Briosa.Desktop;
using Forms = System.Windows.Forms;

namespace Briosa.ControlCenter;

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "WPF owns Application lifetime; OnExit disposes its resources.")]
public partial class App : System.Windows.Application
{
    private readonly List<SingleWindow> _instances = [];
    private DesktopSession? _session;
    private MainWindow? _window;
    private Forms.NotifyIcon? _tray;
    private Forms.ContextMenuStrip? _menu;
    private DispatcherTimer? _timer;
    private string _notification = "";
    private DateTimeOffset _lastNotification;
    private System.Drawing.Icon? _icon;
    private bool _exiting;
    private BrandTheme? _theme;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Desktop startup errors are displayed without exposing local configuration or raw exceptions.")]
    protected override async void OnStartup(StartupEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnStartup(e);
        try
        {
            BrandTheme.ApplyPreference(this, "system");
            _theme = new BrandTheme(this);
            string? monitor = null;
            bool show = e.Args is ["--show"];
            if (e.Args is ["--monitor", var instance] && DesktopProtocol.IsInstance(instance)) monitor = instance;
            else if (e.Args.Length != 0 && !show) throw new ArgumentException("Invalid desktop arguments.");
            _session = new DesktopSession(DesktopStore.PackageDirectory(AppContext.BaseDirectory), monitor);
            var key = _session.Instance ?? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(AppContext.BaseDirectory).ToUpperInvariant())));
            if (!Acquire(key, show)) { Shutdown(); return; }
            _session.InstanceSelected += (_, _) => Dispatcher.Invoke(AcquireCurrentInstance);
            _window = new MainWindow(_session);
            MainWindow = _window;
            _window.ServerStateChanged += UpdateTray;
            using var resource = GetResourceStream(new Uri("pack://application:,,,/Assets/AppIcon/briosa.ico")).Stream;
            _icon = new System.Drawing.Icon(resource);
            _menu = new Forms.ContextMenuStrip();
            _menu.Items.Add("Open Briosa Control Center", null, (_, _) => _window.ShowOverview());
            _menu.Items.Add("Copy server endpoint", null, (_, _) => _window.CopyEndpoint());
            _menu.Items.Add("View activity", null, (_, _) => _window.ShowActivity());
            _menu.Items.Add(new Forms.ToolStripSeparator());
            var start = _menu.Items.Add("Start server", null, async (_, _) => { await _window.StartServerAsync(); AcquireCurrentInstance(); });
            var reconnect = _menu.Items.Add("Reconnect to SpatialAnalyzer", null, async (_, _) => await _window.ReconnectAsync());
            var recover = _menu.Items.Add("Recover SDK", null, async (_, _) => await _window.RecoverAsync());
            var stop = _menu.Items.Add("Stop server", null, async (_, _) => await _window.StopServerAsync());
            var restart = _menu.Items.Add("Restart server", null, async (_, _) => { await _window.RestartServerAsync(); AcquireCurrentInstance(); });
            _menu.Items.Add(new Forms.ToolStripSeparator());
            _menu.Items.Add("Export support information…", null, async (_, _) => { _window.ShowWindow(); await _window.ExportAsync(); });
            _menu.Items.Add("Exit tray application", null, (_, _) => Shutdown());
            _menu.Opening += (_, _) =>
            {
                start.Enabled = !_window.IsBusy && !_session.HasServer;
                stop.Enabled = restart.Enabled = !_window.IsBusy && _window.Current.CanManage && _window.Current.Available;
                reconnect.Enabled = !_window.IsBusy && _window.Current.CanReconnect;
                recover.Enabled = !_window.IsBusy && _window.Current.CanRecover;
            };
            _tray = new Forms.NotifyIcon { Icon = _icon, ContextMenuStrip = _menu, Text = "Briosa Control Center", Visible = true };
            _tray.DoubleClick += (_, _) => _window.ShowOverview();
            _tray.BalloonTipClicked += (_, _) => _window.ShowOverview();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _timer.Tick += async (_, _) =>
            {
                await _window.ObserveAsync(); AcquireCurrentInstance();
                if (monitor is not null)
                {
                    try { if (DesktopStore.ReadExit(monitor) is { Clean: true }) Shutdown(); }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException) { }
                }
            };
            _timer.Start();
            if (show) _window.ShowWindow();
            if (_session.HasServer) await _window.ObserveAsync();
            else if (!show) { await _window.StartServerAsync(); AcquireCurrentInstance(); }
            UpdateTray(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            MessageBox.Show("Briosa Control Center could not start. Check that the complete package is installed and your per-user settings are accessible. The server has not been stopped.",
                "Briosa Control Center", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(2);
        }
    }

    private bool Acquire(string key, bool activate)
    {
        var instance = new SingleWindow(key, () => Dispatcher.BeginInvoke(() => { if (!_exiting) _window?.ShowWindow(); }));
        if (!instance.IsFirst) { if (activate) instance.ShowExisting(); instance.Dispose(); return false; }
        _instances.Add(instance);
        return true;
    }

    private void AcquireCurrentInstance()
    {
        if (_session?.Instance is { } instance && !_instances.Any(item => item.Key == instance)) Acquire(instance, false);
    }

    private void UpdateTray(object? sender, EventArgs e)
    {
        if (_tray is null || _window is null) return;
        var state = _window.Current;
        var text = "Briosa · " + DesktopProtocol.Target + " · " + state.Heading;
        _tray.Text = text[..Math.Min(text.Length, 127)];
        if (state.NeedsAttention && _window.NotificationsEnabled && state.Heading != _notification &&
            DateTimeOffset.UtcNow - _lastNotification > TimeSpan.FromSeconds(45))
        {
            _notification = state.Heading;
            _lastNotification = DateTimeOffset.UtcNow;
            _tray.ShowBalloonTip(6000, "Briosa · " + state.Heading, state.Guidance, Forms.ToolTipIcon.Warning);
        }
        if (!state.NeedsAttention) _notification = "";
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _exiting = true;
        _timer?.Stop();
        _theme?.Dispose();
        _window?.Exit();
        _tray?.Dispose();
        _menu?.Dispose();
        _icon?.Dispose();
        _session?.Dispose();
        foreach (var instance in _instances) instance.Dispose();
        base.OnExit(e);
    }
}
