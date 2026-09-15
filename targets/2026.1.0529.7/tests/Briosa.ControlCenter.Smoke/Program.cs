using System.Windows;
using System.IO;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Briosa.ControlCenter;
using Briosa.Desktop;
using Google.Protobuf;

namespace Briosa.ControlCenter.Smoke;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args is ["--package", var package, var worker]) return PackageSmoke.RunAsync(package, worker).GetAwaiter().GetResult();
        if (args is not [var output]) return 2;
        Directory.CreateDirectory(output);
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/Briosa.ControlCenter.App;component/Theme.xaml") });
        // An isolated, nonexistent package path prevents attachment to user servers.
        using var session = new DesktopSession(Path.Combine(output, "inert-package"));
        var window = new MainWindow(session);
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -20000; window.Top = -20000;
        window.ShowActivated = false; window.ShowInTaskbar = false;
        window.Show();
        var info = new GetServerInfoResponse { ReadyForMp = true,
            Version = new() { BriosaVersion = "0.2.0-review", SpatialAnalyzerTarget = DesktopProtocol.Target, ProtocolPackage = "briosa" },
            ActivatedSdkIdentity = new() { Version = DesktopProtocol.Target, Source = RuntimeIdentityEvidenceSource.OperatorAttestation, MatchState = RuntimeIdentityMatchState.ExactMatch },
            ConnectedSpatialAnalyzerIdentity = new() { Version = DesktopProtocol.Target, Source = RuntimeIdentityEvidenceSource.OperatorAttestation, MatchState = RuntimeIdentityMatchState.ExactMatch } };
        var sdk = new SpatialAnalyzerSdkLifecycleState { ReadyForMp = true, SdkState = SpatialAnalyzerSdkState.Ready,
            SdkGeneration = 1, ConnectionState = SpatialAnalyzerConnectionState.Connected,
            ExecutionReadinessState = SpatialAnalyzerExecutionReadinessState.ExecutionReady,
            RecoveryState = SpatialAnalyzerSdkRecoveryState.NotRequired };
        var state = DesktopState.FromReply(new() { Instance = DesktopProtocol.NewInstance(), Target = DesktopProtocol.Target,
            Endpoint = "http://127.0.0.1:50051", HostState = "Running", CanManage = true,
            ServerInfoJson = JsonFormatter.Default.Format(info), SdkStateJson = JsonFormatter.Default.Format(sdk),
            ApplicationStateJson = JsonFormatter.Default.Format(new SpatialAnalyzerLifecycleState { ApplicationState = SpatialAnalyzerApplicationState.Running,
                Ownership = SpatialAnalyzerOwnership.External }) });
        window.Render(state);
        var stop = (Button)window.FindName("StopButton");
        if (!stop.IsEnabled || new ButtonAutomationPeer(stop).GetName().Length == 0) throw new InvalidOperationException("Owned stop control is inaccessible.");
        BrandTheme.ApplyPreference(app, "light");
        CheckContrast(app, "TextFillColorPrimaryBrush", "CardBackgroundFillColorDefaultBrush");
        CheckContrast(app, "AccentButtonForeground", "AccentButtonBackground");
        Save(window, Path.Combine(output, "overview.png"), 1140, 800, 1);
        BrandTheme.ApplyPreference(app, "dark");
        CheckContrast(app, "TextFillColorPrimaryBrush", "CardBackgroundFillColorDefaultBrush");
        CheckContrast(app, "AccentButtonForeground", "AccentButtonBackground");
        Save(window, Path.Combine(output, "overview-dark.png"), 1140, 800, 1);
        window.Render(state.Unavailable());
        if (stop.IsEnabled) throw new InvalidOperationException("Stale status enabled management.");
        Save(window, Path.Combine(output, "stale-150-percent.png"), 800, 680, 1.5);
        ((ListBox)window.FindName("Navigation")).SelectedIndex = 1;
        ((DataGrid)window.FindName("ActivityGrid")).ItemsSource = new[]
        {
            new ActivityEntry(DateTimeOffset.Now, "Error", "Execution", "Command execution observation recorded.", "", "GetWorkingFrameProperties", "ExecutionDisposition: StartedOutcomeUnknown"),
            new ActivityEntry(DateTimeOffset.Now.AddMinutes(-2), "Information", "Server", "Server started.", "", "", "")
        };
        Save(window, Path.Combine(output, "activity.png"), 1000, 760, 1);
        ((ListBox)window.FindName("Navigation")).SelectedIndex = 2;
        Save(window, Path.Combine(output, "support.png"), 1000, 760, 1);
        BrandTheme.Apply(app, dark: false, highContrast: true);
        if (app.Resources["BriosaWorkspaceBrush"] is not SolidColorBrush contrast || contrast.Color != SystemColors.WindowColor)
            throw new InvalidOperationException("High contrast retained decorative planes.");
        window.Exit();
        app.Shutdown();
        Console.WriteLine("WPF layout, state gating, and automation labels passed. No server or SDK was started.");
        return 0;
    }

    private static void CheckContrast(Application app, string text, string background)
    {
        static double Luminance(System.Windows.Media.Color color)
        {
            static double Channel(byte value) { var c = value / 255d; return c <= .04045 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4); }
            return .2126 * Channel(color.R) + .7152 * Channel(color.G) + .0722 * Channel(color.B);
        }
        var a = Luminance(((SolidColorBrush)app.Resources[text]).Color);
        var b = Luminance(((SolidColorBrush)app.Resources[background]).Color);
        if ((Math.Max(a, b) + .05) / (Math.Min(a, b) + .05) < 4.5) throw new InvalidOperationException("Insufficient theme contrast.");
    }

    private static void Save(MainWindow window, string path, int width, int height, double scale)
    {
        window.Width = width; window.Height = height;
        window.Measure(new Size(width, height));
        window.Arrange(new Rect(0, 0, width, height));
        window.UpdateLayout();
        window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        var peers = new Queue<AutomationPeer>();
        peers.Enqueue(UIElementAutomationPeer.CreatePeerForElement(window));
        var controls = new List<string>();
        while (peers.TryDequeue(out var peer))
        {
            controls.Add(peer.GetName());
            foreach (var child in peer.GetChildren() ?? []) peers.Enqueue(child);
        }
        var selected = ((ListBox)window.FindName("Navigation")).SelectedIndex;
        var expected = selected switch { 0 => "Start server", 1 => "Search activity", _ => "App theme" };
        if (!controls.Contains(expected)) throw new InvalidOperationException("Page controls are missing from the window automation tree: " + expected);
        var visual = (Visual)window.Content;
        var bitmap = new RenderTargetBitmap((int)(width * scale), (int)(height * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        var canvas = new DrawingVisual();
        using (var drawing = canvas.RenderOpen())
        {
            drawing.DrawRectangle(SystemColors.WindowBrush, null, new Rect(0, 0, width, height));
            drawing.DrawRectangle(new VisualBrush(visual), null, new Rect(0, 0, width, height));
        }
        bitmap.Render(canvas);
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        if (pixels.Distinct().Count() < 10) throw new InvalidOperationException("The rendered window is blank.");
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path);
        encoder.Save(file);
    }
}
